using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Services;

namespace NetSupport.RemoteAdmin.Providers;

public sealed partial class NetSupportProvider(
    AppConfig config,
    IDiagnosticLogService diagnosticLog) : IRemoteProvider
{
    private readonly INetSupportProfileService _profileService = new NetSupportProfileService();

    public string Id => "netsupport";
    public string DisplayName => "NetSupport Manager";

    public IReadOnlyCollection<RemoteAction> SupportedActions { get; } = new[]
    {
        RemoteAction.Control,
        RemoteAction.View,
        RemoteAction.Chat,
        RemoteAction.Inventory,
        RemoteAction.CommandPrompt,
        RemoteAction.FileTransfer
    };

    public bool IsAvailable
    {
        get
        {
            try
            {
                _ = GetValidatedExecutablePath();
                ValidateConfiguredProfile();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public Task ConnectAsync(RemoteTarget target, RemoteAction action, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var executablePath = GetValidatedExecutablePath();
        var profileArguments = BuildProfileArguments();

        if (!SupportedActions.Contains(action))
            throw new NotSupportedException($"Die Aktion '{action}' wird von NetSupport nicht unterstützt.");

        var connectArgument = BuildConnectArgument(target.Host);
        var actionArguments = string.Join(' ', GetActionArguments(action));
        var arguments = string.Join(
            ' ',
            new[] { profileArguments, connectArgument, actionArguments }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        // NetSupport documents an unusual compact /c\">address\" syntax for IP connections.
        // ProcessStartInfo.ArgumentList can re-escape embedded quotes, so pass the validated
        // command line directly to PCICTLUI.EXE instead of asking .NET to reconstruct it.
        var psi = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = arguments,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(executablePath) ?? Environment.CurrentDirectory
        };

        diagnosticLog.Info($"NetSupport CLI: {Path.GetFileName(psi.FileName)} {arguments}");
        var process = Process.Start(psi)
                      ?? throw new InvalidOperationException("NetSupport konnte nicht gestartet werden.");
        diagnosticLog.Info($"NetSupport-Prozess gestartet: PID {process.Id}; Aktion {action}; Ziel {target.Host}");
        return Task.CompletedTask;
    }

    internal static string BuildConnectArgument(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Für die Verbindung ist ein Rechnername oder eine IP-Adresse erforderlich.", nameof(host));

        var trimmed = host.Trim();
        if (trimmed.Length > 253 || trimmed.Contains('"') || trimmed.Contains('\r') || trimmed.Contains('\n'))
            throw new ArgumentException("Der Rechnername enthält ungültige Zeichen.", nameof(host));

        if (IPAddress.TryParse(trimmed, out var address))
            return $"/c\">{address}\"";

        // The UI is intended for domain computer/DNS names, not arbitrary command-line text.
        // Restrict the value before placing it into NetSupport's raw command line.
        if (!DomainHostNameRegex().IsMatch(trimmed))
        {
            throw new ArgumentException(
                "Der Rechnername ist ungültig. Erlaubt sind Buchstaben, Ziffern, Punkt, Bindestrich und Unterstrich.",
                nameof(host));
        }

        return $"/c {trimmed}";
    }

    private string BuildProfileArguments()
    {
        var profile = ValidateConfiguredProfile();
        if (profile is null)
            return string.Empty;

        // /F is only meaningful together with /N. Quoting allows profile names containing spaces
        // while the profile-name validator prevents additional command-line syntax from being injected.
        return config.NetSupportLockProfile
            ? $"/f /n \"{profile}\""
            : $"/n \"{profile}\"";
    }

    private string? ValidateConfiguredProfile()
    {
        if (string.IsNullOrWhiteSpace(config.NetSupportProfileName))
        {
            if (config.NetSupportLockProfile)
            {
                throw new InvalidOperationException(
                    "NetSupport-Profilbindung (/F) ist aktiviert, aber es wurde kein Control-Profil ausgewählt.");
            }

            return null;
        }

        var profile = NetSupportProfileService.NormalizeProfileName(config.NetSupportProfileName);
        if (!_profileService.ProfileExists(profile))
        {
            throw new InvalidOperationException(
                $"Das konfigurierte NetSupport-Control-Profil '{profile}' wurde unter HKCU\\{NetSupportProfileService.ConfigListRegistryPath} nicht gefunden. " +
                "Bitte das Profil in NetSupport Manager anlegen oder unter Erweitert → Einstellungen ein vorhandenes Profil auswählen.");
        }

        return profile;
    }

    private string GetValidatedExecutablePath()
    {
        if (string.IsNullOrWhiteSpace(config.NetSupportExecutable))
        {
            throw new InvalidOperationException(
                "NetSupport Manager (PCICTLUI.EXE) ist nicht konfiguriert. Bitte den Pfad unter Erweitert → Einstellungen prüfen.");
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(config.NetSupportExecutable.Trim().Trim('"')));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Der konfigurierte NetSupport-Pfad ist ungültig.", ex);
        }

        if (!string.Equals(Path.GetFileName(fullPath), "PCICTLUI.EXE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Der konfigurierte NetSupport-Pfad muss auf PCICTLUI.EXE zeigen. Andere Programme werden nicht über den Remote-Provider gestartet.");
        }

        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException(
                $"NetSupport Manager wurde am konfigurierten Pfad nicht gefunden: {fullPath}");
        }

        return fullPath;
    }

    private static IEnumerable<string> GetActionArguments(RemoteAction action) => action switch
    {
        RemoteAction.Control => ["/vc", "/e"],
        RemoteAction.View => ["/v", "/e"],
        RemoteAction.Chat => ["/a", "/ea"],
        RemoteAction.Inventory => ["/i", "/ei"],
        RemoteAction.CommandPrompt => ["/m", "/em"],
        RemoteAction.FileTransfer => ["/x", "/ex"],
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
    };

    [GeneratedRegex(@"^[A-Za-z0-9](?:[A-Za-z0-9._-]{0,251}[A-Za-z0-9_])?$", RegexOptions.CultureInvariant)]
    private static partial Regex DomainHostNameRegex();
}
