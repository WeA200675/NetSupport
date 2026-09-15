using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Services;

/// <summary>
/// Creates a local troubleshooting ZIP without copying settings.json verbatim.
/// Credentials are never written. With anonymization enabled, known target and
/// local identity strings are replaced before logs/history are included.
/// </summary>
public sealed class SupportBundleService : ISupportBundleService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly AppConfig _config;
    private readonly ConfigService _configService;
    private readonly ISessionHistoryService _historyService;
    private readonly IDiagnosticLogService _diagnosticLog;
    private readonly INetSupportInstallationService _netSupportInstallationService = new NetSupportInstallationService();
    private readonly INetSupportProfileService _netSupportProfileService = new NetSupportProfileService();

    public SupportBundleService(
        AppConfig config,
        ConfigService configService,
        ISessionHistoryService historyService,
        IDiagnosticLogService diagnosticLog)
    {
        _config = config;
        _configService = configService;
        _historyService = historyService;
        _diagnosticLog = diagnosticLog;
    }

    public async Task CreateAsync(
        string destinationPath,
        bool anonymizeIdentifiers = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        var fullDestinationPath = Path.GetFullPath(destinationPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullDestinationPath)
                                  ?? throw new InvalidOperationException("Zielordner konnte nicht ermittelt werden."));

        var tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "NetSupportRemoteAdmin-support-" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempDirectory);
        try
        {
            var aliases = BuildAliases(anonymizeIdentifiers);

            await WriteManifestAsync(tempDirectory, anonymizeIdentifiers, cancellationToken);
            await WriteSystemInfoAsync(tempDirectory, anonymizeIdentifiers, cancellationToken);
            await WriteConfigSummaryAsync(tempDirectory, aliases, anonymizeIdentifiers, cancellationToken);
            await WriteHistoryAsync(tempDirectory, aliases, anonymizeIdentifiers, cancellationToken);
            await CopySanitizedLogsAsync(tempDirectory, aliases, anonymizeIdentifiers, cancellationToken);

            if (File.Exists(fullDestinationPath))
                File.Delete(fullDestinationPath);

            ZipFile.CreateFromDirectory(
                tempDirectory,
                fullDestinationPath,
                CompressionLevel.Optimal,
                includeBaseDirectory: false);

            _diagnosticLog.Info($"Supportpaket erstellt: {Path.GetFileName(fullDestinationPath)}");
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, recursive: true);
            }
            catch
            {
                // Temporary support data should be cleaned up when possible,
                // but cleanup failure must not invalidate an already created ZIP.
            }
        }
    }

    private static async Task WriteManifestAsync(
        string directory,
        bool anonymized,
        CancellationToken cancellationToken)
    {
        var text = $$"""
NetSupport Remote Admin - Supportpaket
Erstellt: {{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}}
Anonymisierung: {{(anonymized ? "aktiv" : "deaktiviert")}}
Remotezugriffsrichtlinie: NetSupport-only

Enthalten:
- system-info.json: lokale Laufzeit-/Windows-Informationen
- configuration-summary.json: bereinigte Konfigurationsübersicht inklusive NetSupport-Version/Erkennungs-/Profilstatus
- recent-history.json: bereinigter lokaler Startverlauf
- recent-errors.txt: zuletzt bekannte Fehler aus Verlauf/Diagnoselog
- logs/: vorhandene, beim Verpacken bereinigte Diagnoseprotokolle

Nie enthalten:
- Kennwörter oder gespeicherte Windows-Credentials
- Bildschirminhalte
- Zwischenablageinhalte
- Inhalte übertragener Dateien
- settings.json im Original

Hinweis:
Das Paket ist für technische Fehlersuche gedacht. Vor einer Weitergabe sollte es trotzdem kurz geprüft werden,
insbesondere wenn die Anonymisierung deaktiviert wurde.
""";

        await File.WriteAllTextAsync(
            Path.Combine(directory, "README.txt"),
            text,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            cancellationToken);
    }

    private static async Task WriteSystemInfoAsync(
        string directory,
        bool anonymized,
        CancellationToken cancellationToken)
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var data = new
        {
            createdAt = DateTimeOffset.Now,
            applicationVersion = entryAssembly?.GetName().Version?.ToString() ?? "unknown",
            windows = RuntimeInformation.OSDescription,
            osArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            framework = RuntimeInformation.FrameworkDescription,
            is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
            is64BitProcess = Environment.Is64BitProcess,
            culture = CultureInfo.CurrentCulture.Name,
            uiCulture = CultureInfo.CurrentUICulture.Name,
            machine = anonymized ? "local-machine" : Environment.MachineName,
            user = anonymized ? "local-user" : Environment.UserName,
            userDomain = anonymized ? "local-domain" : Environment.UserDomainName
        };

        await WriteJsonAsync(Path.Combine(directory, "system-info.json"), data, cancellationToken);
    }

    private async Task WriteConfigSummaryAsync(
        string directory,
        IReadOnlyDictionary<string, string> aliases,
        bool anonymized,
        CancellationToken cancellationToken)
    {
        var groupAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var nextGroup = 1;

        string? MapGroup(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            if (!anonymized)
                return value.Trim();

            var key = value.Trim();
            if (!groupAliases.TryGetValue(key, out var alias))
            {
                alias = $"group-{nextGroup++:000}";
                groupAliases[key] = alias;
            }

            return alias;
        }

        var targets = _config.Targets.Select((target, index) => new
        {
            id = anonymized ? AliasFor(target, aliases, index) : target.Host,
            name = anonymized ? AliasFor(target, aliases, index) : target.Name,
            host = anonymized ? AliasFor(target, aliases, index) : target.Host,
            description = anonymized ? null : target.Description,
            isFavorite = target.IsFavorite,
            group = MapGroup(target.Group),
            preferredProviderId = target.PreferredProviderId,
            preferredAction = target.PreferredAction
        }).ToList();

        var savedViews = anonymized
            ? _config.SavedViews.Select((_, index) => new
            {
                name = $"view-{index + 1:000}",
                searchTextConfigured = !string.IsNullOrWhiteSpace(_config.SavedViews[index].SearchText),
                groupConfigured = !string.IsNullOrWhiteSpace(_config.SavedViews[index].Group),
                favoritesOnly = _config.SavedViews[index].FavoritesOnly
            }).ToList<object>()
            : _config.SavedViews.Select(view => (object)new
            {
                name = view.Name,
                searchText = view.SearchText,
                group = view.Group,
                favoritesOnly = view.FavoritesOnly
            }).ToList();

        var netSupportCandidates = _netSupportInstallationService.Discover(_config.NetSupportExecutable);
        var configuredNetSupport = netSupportCandidates.FirstOrDefault(candidate =>
            string.Equals(candidate.Source, "Konfiguriert", StringComparison.OrdinalIgnoreCase));
        var detectedNetSupport = netSupportCandidates.FirstOrDefault(candidate => candidate.IsUsable);

        var configuredProfile = string.IsNullOrWhiteSpace(_config.NetSupportProfileName)
            ? null
            : _config.NetSupportProfileName.Trim();
        bool? profileAvailable = null;
        if (configuredProfile is not null)
        {
            try
            {
                profileAvailable = _netSupportProfileService.ProfileExists(configuredProfile);
            }
            catch
            {
                profileAvailable = false;
            }
        }

        var data = new
        {
            remoteAccessPolicy = "NetSupport-only",
            startMinimized = _config.StartMinimized,
            diagnosticLoggingEnabled = _config.DiagnosticLoggingEnabled,
            netSupportClientPort = _config.NetSupportClientPort,
            netSupportExecutableConfigured = !string.IsNullOrWhiteSpace(_config.NetSupportExecutable),
            netSupportExecutableExists = configuredNetSupport?.IsUsable == true,
            netSupportExecutableFileName = string.IsNullOrWhiteSpace(_config.NetSupportExecutable)
                ? null
                : Path.GetFileName(_config.NetSupportExecutable),
            netSupportAlternativeInstallationFound = configuredNetSupport?.IsUsable != true && detectedNetSupport is not null,
            netSupportProductName = detectedNetSupport?.ProductName,
            netSupportProductVersion = detectedNetSupport?.ProductVersion,
            netSupportFileVersion = detectedNetSupport?.FileVersion,
            netSupportCompanyName = detectedNetSupport?.CompanyName,
            netSupportProfileConfigured = configuredProfile is not null,
            netSupportProfileName = configuredProfile is null
                ? null
                : anonymized ? "netsupport-profile" : configuredProfile,
            netSupportProfileAvailable = profileAvailable,
            netSupportProfileLocked = _config.NetSupportLockProfile,
            netSupportDiscoveredProfileCount = _netSupportProfileService.DiscoverProfiles().Count,
            targetCount = _config.Targets.Count,
            savedViewCount = _config.SavedViews.Count,
            targets,
            savedViews
        };

        await WriteJsonAsync(Path.Combine(directory, "configuration-summary.json"), data, cancellationToken);
    }

    private async Task WriteHistoryAsync(
        string directory,
        IReadOnlyDictionary<string, string> aliases,
        bool anonymized,
        CancellationToken cancellationToken)
    {
        var history = await _historyService.GetRecentAsync(100, cancellationToken);
        var sanitized = history.Select((entry, index) => new
        {
            startedAt = entry.StartedAt,
            host = anonymized ? AliasFor(entry.Host, aliases, index) : entry.Host,
            targetName = anonymized
                ? AliasFor(entry.Host, aliases, index)
                : entry.TargetName,
            providerId = entry.ProviderId,
            providerName = entry.ProviderName,
            action = entry.Action,
            succeeded = entry.Succeeded,
            error = SanitizeText(entry.Error, aliases, anonymized)
        }).ToList();

        await WriteJsonAsync(Path.Combine(directory, "recent-history.json"), sanitized, cancellationToken);

        var errors = sanitized
            .Where(entry => !entry.succeeded && !string.IsNullOrWhiteSpace(entry.error))
            .Select(entry => $"{entry.startedAt:yyyy-MM-dd HH:mm:ss zzz} | {entry.host} | {entry.providerName} | {entry.action} | {entry.error}")
            .ToList();

        var logErrors = await ReadDiagnosticErrorsAsync(aliases, anonymized, cancellationToken);
        errors.AddRange(logErrors);

        if (errors.Count == 0)
            errors.Add("Keine Fehler im lokalen Verlauf bzw. Diagnoseprotokoll gefunden.");

        await File.WriteAllLinesAsync(
            Path.Combine(directory, "recent-errors.txt"),
            errors.TakeLast(100),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            cancellationToken);
    }

    private async Task CopySanitizedLogsAsync(
        string directory,
        IReadOnlyDictionary<string, string> aliases,
        bool anonymized,
        CancellationToken cancellationToken)
    {
        var logDirectory = Path.Combine(directory, "logs");
        Directory.CreateDirectory(logDirectory);

        foreach (var sourcePath in new[] { _diagnosticLog.LogPath + ".1", _diagnosticLog.LogPath })
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!File.Exists(sourcePath))
                continue;

            string content;
            try
            {
                content = await File.ReadAllTextAsync(sourcePath, cancellationToken);
            }
            catch (IOException)
            {
                continue;
            }

            var sanitized = SanitizeText(content, aliases, anonymized) ?? string.Empty;
            await File.WriteAllTextAsync(
                Path.Combine(logDirectory, Path.GetFileName(sourcePath)),
                sanitized,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
                cancellationToken);
        }
    }

    private async Task<IReadOnlyList<string>> ReadDiagnosticErrorsAsync(
        IReadOnlyDictionary<string, string> aliases,
        bool anonymized,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        foreach (var path in new[] { _diagnosticLog.LogPath + ".1", _diagnosticLog.LogPath })
        {
            if (!File.Exists(path))
                continue;

            try
            {
                var lines = await File.ReadAllLinesAsync(path, cancellationToken);
                errors.AddRange(lines
                    .Where(line => line.Contains("[ERROR]", StringComparison.OrdinalIgnoreCase))
                    .Select(line => SanitizeText(line, aliases, anonymized) ?? string.Empty));
            }
            catch (IOException)
            {
                // A log file can be unavailable briefly while another process/thread writes it.
            }
        }

        return errors.TakeLast(50).ToList();
    }

    private IReadOnlyDictionary<string, string> BuildAliases(bool anonymized)
    {
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!anonymized)
            return aliases;

        for (var i = 0; i < _config.Targets.Count; i++)
        {
            var target = _config.Targets[i];
            var alias = $"target-{i + 1:000}";
            AddAlias(aliases, target.Host, alias);
            AddAlias(aliases, target.Name, alias);
        }

        AddAlias(aliases, Environment.MachineName, "local-machine");
        AddAlias(aliases, Environment.UserName, "local-user");
        AddAlias(aliases, Environment.UserDomainName, "local-domain");
        AddAlias(aliases, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "user-profile");
        AddAlias(aliases, _configService.ConfigDirectory, "config-directory");
        AddAlias(aliases, _config.NetSupportProfileName, "netsupport-profile");

        return aliases;
    }

    private static void AddAlias(IDictionary<string, string> aliases, string? value, string alias)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var trimmed = value.Trim();
        if (trimmed.Length < 3)
            return;

        aliases[trimmed] = alias;
    }

    private static string AliasFor(
        RemoteTarget target,
        IReadOnlyDictionary<string, string> aliases,
        int index) => AliasFor(target.Host, aliases, index);

    private static string AliasFor(
        string host,
        IReadOnlyDictionary<string, string> aliases,
        int index)
    {
        if (!string.IsNullOrWhiteSpace(host) && aliases.TryGetValue(host.Trim(), out var alias))
            return alias;

        return $"target-{index + 1:000}";
    }

    private static string? SanitizeText(
        string? value,
        IReadOnlyDictionary<string, string> aliases,
        bool anonymized)
    {
        if (value is null || !anonymized || aliases.Count == 0)
            return value;

        var result = value;
        foreach (var pair in aliases.OrderByDescending(pair => pair.Key.Length))
            result = result.Replace(pair.Key, pair.Value, StringComparison.OrdinalIgnoreCase);

        return result;
    }

    private static async Task WriteJsonAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken)
    {
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
    }
}
