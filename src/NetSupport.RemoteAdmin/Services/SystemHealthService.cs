using System.Diagnostics;
using System.IO;
using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Services;

/// <summary>
/// Checks local prerequisites of the administration workstation without scanning remote hosts.
/// </summary>
public sealed class SystemHealthService(
    AppConfig config,
    ConfigService configService,
    IAutoStartService autoStartService) : ISystemHealthService
{
    private readonly INetSupportInstallationService _netSupportInstallationService = new NetSupportInstallationService();
    private readonly INetSupportProfileService _netSupportProfileService = new NetSupportProfileService();

    public async Task<IReadOnlyList<SystemHealthCheckResult>> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        var results = new List<SystemHealthCheckResult>
        {
            CheckRemoteAccessPolicy(),
            CheckConfigDirectory(),
            CheckNetSupport(),
            CheckNetSupportProfile(),
            CheckNetSupportClientPort(),
            CheckAutoStart(),
            CheckDiagnostics()
        };

        results.Add(await CheckActiveDirectoryDiscoveryAsync(cancellationToken));
        results.Add(await CheckLocalCimAsync(cancellationToken));
        return results;
    }

    private static SystemHealthCheckResult CheckRemoteAccessPolicy() => Healthy(
        "Remotezugriffsrichtlinie",
        "NetSupport-only ist aktiv; nur NetSupport wird als Remote-Provider registriert.",
        "Domänenkonformer Remotezugriff erfolgt ausschließlich über NetSupport Manager.");

    private SystemHealthCheckResult CheckConfigDirectory()
    {
        try
        {
            Directory.CreateDirectory(configService.ConfigDirectory);
            var testPath = Path.Combine(configService.ConfigDirectory, $".write-test-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(testPath, "ok");
            File.Delete(testPath);

            return Healthy(
                "Anwendungsdaten",
                "AppData-Verzeichnis ist verfügbar und beschreibbar.",
                configService.ConfigDirectory);
        }
        catch (Exception ex)
        {
            return Error(
                "Anwendungsdaten",
                "AppData-Verzeichnis ist nicht beschreibbar.",
                ex.Message);
        }
    }

    private SystemHealthCheckResult CheckNetSupport()
    {
        if (!string.IsNullOrWhiteSpace(config.NetSupportExecutable))
        {
            var configured = _netSupportInstallationService.Inspect(config.NetSupportExecutable, "Konfiguriert");
            if (configured.IsUsable)
            {
                return Healthy(
                    "NetSupport Manager",
                    "Die konfigurierte PCICTLUI.EXE wurde gefunden und als Control-Executable erkannt.",
                    FormatNetSupportDetails(configured));
            }

            var alternativePath = _netSupportInstallationService.FindBestExecutable(config.NetSupportExecutable);
            if (!string.IsNullOrWhiteSpace(alternativePath) &&
                !string.Equals(alternativePath, configured.Path, StringComparison.OrdinalIgnoreCase))
            {
                var alternative = _netSupportInstallationService.Inspect(alternativePath, "Automatisch erkannt");
                var configuredReason = configured.Exists
                    ? "Datei vorhanden, aber nicht PCICTLUI.EXE"
                    : "Datei nicht vorhanden";
                return Warning(
                    "NetSupport Manager",
                    "Der konfigurierte Pfad ist ungültig, aber eine andere gültige NetSupport-Installation wurde gefunden.",
                    $"Konfiguriert ({configuredReason}): {configured.Path}\nGefunden: {FormatNetSupportDetails(alternative)}");
            }

            var reason = configured.Exists
                ? "Die konfigurierte Datei ist vorhanden, aber nicht PCICTLUI.EXE."
                : "Die konfigurierte PCICTLUI.EXE wurde nicht gefunden.";
            return Error(
                "NetSupport Manager",
                reason,
                configured.Path);
        }

        var detectedPath = _netSupportInstallationService.FindBestExecutable();
        if (!string.IsNullOrWhiteSpace(detectedPath))
        {
            var detected = _netSupportInstallationService.Inspect(detectedPath, "Automatisch erkannt");
            return Warning(
                "NetSupport Manager",
                "NetSupport wurde lokal gefunden, ist aber noch nicht als Control-Pfad gespeichert.",
                FormatNetSupportDetails(detected));
        }

        return Error(
            "NetSupport Manager",
            "PCICTLUI.EXE ist nicht konfiguriert und konnte lokal nicht automatisch gefunden werden.",
            "Pfad unter Erweitert → Einstellungen festlegen oder NetSupport Manager Control installieren.");
    }

    private SystemHealthCheckResult CheckNetSupportProfile()
    {
        if (string.IsNullOrWhiteSpace(config.NetSupportProfileName))
        {
            if (config.NetSupportLockProfile)
            {
                return Error(
                    "NetSupport Control-Profil",
                    "Profilbindung (/F) ist aktiviert, aber kein Control-Profil ist konfiguriert.",
                    "Unter Erweitert → Einstellungen ein vorhandenes Profil auswählen oder die Profilbindung deaktivieren.");
            }

            var profiles = _netSupportProfileService.DiscoverProfiles();
            return new SystemHealthCheckResult
            {
                Name = "NetSupport Control-Profil",
                Level = SystemHealthLevel.Info,
                Summary = "Kein festes Control-Profil konfiguriert; NetSupport verwendet sein Standardverhalten.",
                Details = profiles.Count == 0
                    ? $"Keine Profile unter HKCU\\{NetSupportProfileService.ConfigListRegistryPath} gefunden."
                    : $"{profiles.Count} lokale(s) Profil(e) verfügbar: {string.Join(", ", profiles)}"
            };
        }

        string profile;
        try
        {
            profile = NetSupportProfileService.NormalizeProfileName(config.NetSupportProfileName);
        }
        catch (ArgumentException ex)
        {
            return Error(
                "NetSupport Control-Profil",
                "Der konfigurierte Profilname ist ungültig.",
                ex.Message);
        }

        if (!_netSupportProfileService.ProfileExists(profile))
        {
            return Error(
                "NetSupport Control-Profil",
                $"Das konfigurierte Profil '{profile}' wurde für den aktuellen Windows-Benutzer nicht gefunden.",
                $"Erwartet unter HKCU\\{NetSupportProfileService.ConfigListRegistryPath}. Remote-Aktionen werden absichtlich blockiert, damit kein anderes Profil stillschweigend verwendet wird.");
        }

        return Healthy(
            "NetSupport Control-Profil",
            $"Control-Profil '{profile}' ist verfügbar.",
            config.NetSupportLockProfile
                ? "Profilbindung ist aktiv (/F + /N); ein Profilwechsel im Control wird eingeschränkt."
                : "Profil wird mit /N geladen; /F ist nicht aktiviert.");
    }

    private SystemHealthCheckResult CheckNetSupportClientPort()
    {
        try
        {
            NetSupportReachabilityService.ValidatePort(config.NetSupportClientPort);
            return Healthy(
                "NetSupport Client-Port",
                $"TCP {config.NetSupportClientPort} ist für die optionale NetSupport-Erreichbarkeitsprüfung konfiguriert.",
                "Der Systemzustand öffnet keine Verbindung zu Zielrechnern. Der Port wird erst über 'Status prüfen' gegen ausgewählte/geladene Ziele getestet und blockiert keinen NetSupport-Start.");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Error(
                "NetSupport Client-Port",
                "Der konfigurierte NetSupport-Client-Port ist ungültig.",
                ex.Message);
        }
    }

    private SystemHealthCheckResult CheckAutoStart() => new()
    {
        Name = "Windows-Autostart",
        Level = SystemHealthLevel.Info,
        Summary = autoStartService.IsEnabled ? "Autostart ist aktiviert." : "Autostart ist deaktiviert.",
        Details = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Run\NetSupportRemoteAdmin"
    };

    private SystemHealthCheckResult CheckDiagnostics() => new()
    {
        Name = "Diagnoseprotokoll",
        Level = config.DiagnosticLoggingEnabled ? SystemHealthLevel.Healthy : SystemHealthLevel.Info,
        Summary = config.DiagnosticLoggingEnabled ? "Diagnoseprotokoll ist aktiviert." : "Diagnoseprotokoll ist deaktiviert.",
        Details = Path.Combine(configService.ConfigDirectory, "logs", "application.log")
    };

    private static async Task<SystemHealthCheckResult> CheckActiveDirectoryDiscoveryAsync(
        CancellationToken cancellationToken)
    {
        const string command = """
            $m = Get-Module -ListAvailable ActiveDirectory | Select-Object -First 1
            if ($null -ne $m) {
                Write-Output ('RSAT|' + $m.Version.ToString())
                exit 0
            }

            try {
                $rootDse = [ADSI]'LDAP://RootDSE'
                $dn = [string]$rootDse.defaultNamingContext
                if ([string]::IsNullOrWhiteSpace($dn)) { exit 4 }
                Write-Output ('LDAP|' + $dn)
                exit 0
            }
            catch {
                Write-Error $_
                exit 5
            }
            """;

        var result = await RunPowerShellAsync(command, cancellationToken);

        if (result.TimedOut)
            return Warning("Active Directory", "Prüfung der AD-Erkennung hat das Zeitlimit überschritten.");

        if (result.ExitCode == 0)
        {
            var output = result.StandardOutput.Trim();
            if (output.StartsWith("RSAT|", StringComparison.OrdinalIgnoreCase))
            {
                var version = output[5..].Trim();
                return Healthy(
                    "Active Directory",
                    "ActiveDirectory-PowerShell-Modul ist verfügbar; Domänensuche verwendet bevorzugt Get-ADComputer.",
                    string.IsNullOrWhiteSpace(version) ? "Quelle: RSAT" : $"Quelle: RSAT · Modulversion: {version}");
            }

            if (output.StartsWith("LDAP|", StringComparison.OrdinalIgnoreCase))
            {
                return Healthy(
                    "Active Directory",
                    "RSAT ist nicht installiert, aber der read-only LDAP-Fallback ist verfügbar.",
                    "'Domäne laden' kann Computerobjekte über LDAP lesen; es werden keine AD-Objekte verändert.");
            }

            return Healthy(
                "Active Directory",
                "Active-Directory-Erkennung ist verfügbar.",
                output);
        }

        var details = string.IsNullOrWhiteSpace(result.StandardError)
            ? "Weder RSAT/Get-ADComputer noch der LDAP-RootDSE-Fallback war verfügbar."
            : result.StandardError.Trim();
        return Warning(
            "Active Directory",
            "Domänensuche ist auf diesem Admin-PC derzeit nicht verfügbar.",
            details);
    }

    private static async Task<SystemHealthCheckResult> CheckLocalCimAsync(
        CancellationToken cancellationToken)
    {
        var result = await RunPowerShellAsync(
            "$os = Get-CimInstance -ClassName Win32_OperatingSystem -ErrorAction Stop; $os.Caption",
            cancellationToken);

        if (result.TimedOut)
            return Warning("CIM / WSMan", "Lokale CIM-Prüfung hat das Zeitlimit überschritten.");

        if (result.ExitCode == 0)
        {
            var caption = result.StandardOutput.Trim();
            return Healthy(
                "CIM / WSMan",
                "Lokale CIM-Abfrage funktioniert.",
                string.IsNullOrWhiteSpace(caption) ? null : caption);
        }

        var details = string.IsNullOrWhiteSpace(result.StandardError)
            ? "Remote-Rechnerdetails können trotzdem abhängig von Firewall/Berechtigungen variieren."
            : result.StandardError.Trim();
        return Warning("CIM / WSMan", "Lokale CIM-Abfrage ist fehlgeschlagen.", details);
    }

    private static string FormatNetSupportDetails(NetSupportInstallationCandidate candidate)
    {
        var details = new List<string> { candidate.Path };
        if (!string.IsNullOrWhiteSpace(candidate.ProductName))
            details.Add($"Produkt: {candidate.ProductName}");
        if (!string.IsNullOrWhiteSpace(candidate.ProductVersion))
            details.Add($"Produktversion: {candidate.ProductVersion}");
        if (!string.IsNullOrWhiteSpace(candidate.FileVersion) &&
            !string.Equals(candidate.FileVersion, candidate.ProductVersion, StringComparison.OrdinalIgnoreCase))
        {
            details.Add($"Dateiversion: {candidate.FileVersion}");
        }
        if (!string.IsNullOrWhiteSpace(candidate.CompanyName))
            details.Add($"Hersteller: {candidate.CompanyName}");
        details.Add($"Quelle: {candidate.Source}");
        return string.Join('\n', details);
    }

    private static async Task<PowerShellResult> RunPowerShellAsync(
        string command,
        CancellationToken cancellationToken)
    {
        var powershell = Path.Combine(
            Environment.SystemDirectory,
            "WindowsPowerShell",
            "v1.0",
            "powershell.exe");
        if (!File.Exists(powershell))
            return new PowerShellResult(-1, string.Empty, "powershell.exe wurde nicht gefunden.", false);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = powershell,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        process.StartInfo.ArgumentList.Add("-NoLogo");
        process.StartInfo.ArgumentList.Add("-NoProfile");
        process.StartInfo.ArgumentList.Add("-NonInteractive");
        process.StartInfo.ArgumentList.Add("-Command");
        process.StartInfo.ArgumentList.Add(command);

        try
        {
            if (!process.Start())
                return new PowerShellResult(-1, string.Empty, "PowerShell konnte nicht gestartet werden.", false);

            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(timeout.Token);

            return new PowerShellResult(
                process.ExitCode,
                await standardOutput,
                await standardError,
                false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best effort only.
            }

            return new PowerShellResult(-1, string.Empty, "Zeitlimit überschritten.", true);
        }
        catch (Exception ex)
        {
            return new PowerShellResult(-1, string.Empty, ex.Message, false);
        }
    }

    private static SystemHealthCheckResult Healthy(string name, string summary, string? details = null) => new()
    {
        Name = name,
        Level = SystemHealthLevel.Healthy,
        Summary = summary,
        Details = details
    };

    private static SystemHealthCheckResult Warning(string name, string summary, string? details = null) => new()
    {
        Name = name,
        Level = SystemHealthLevel.Warning,
        Summary = summary,
        Details = details
    };

    private static SystemHealthCheckResult Error(string name, string summary, string? details = null) => new()
    {
        Name = name,
        Level = SystemHealthLevel.Error,
        Summary = summary,
        Details = details
    };

    private sealed record PowerShellResult(
        int ExitCode,
        string StandardOutput,
        string StandardError,
        bool TimedOut);
}
