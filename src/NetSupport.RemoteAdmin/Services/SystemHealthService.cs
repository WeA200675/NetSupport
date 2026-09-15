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

    public async Task<IReadOnlyList<SystemHealthCheckResult>> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        var results = new List<SystemHealthCheckResult>
        {
            CheckRemoteAccessPolicy(),
            CheckConfigDirectory(),
            CheckNetSupport(),
            CheckAutoStart(),
            CheckDiagnostics()
        };

        results.Add(await CheckActiveDirectoryModuleAsync(cancellationToken));
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

    private static async Task<SystemHealthCheckResult> CheckActiveDirectoryModuleAsync(
        CancellationToken cancellationToken)
    {
        var result = await RunPowerShellAsync(
            "$m = Get-Module -ListAvailable ActiveDirectory | Select-Object -First 1; " +
            "if ($null -eq $m) { exit 3 }; $m.Version.ToString()",
            cancellationToken);

        if (result.TimedOut)
            return Warning("Active Directory / RSAT", "Prüfung des ActiveDirectory-Moduls hat das Zeitlimit überschritten.");

        if (result.ExitCode == 0)
        {
            var version = result.StandardOutput.Trim();
            return Healthy(
                "Active Directory / RSAT",
                "ActiveDirectory-PowerShell-Modul ist verfügbar.",
                string.IsNullOrWhiteSpace(version) ? null : $"Modulversion: {version}");
        }

        return Warning(
            "Active Directory / RSAT",
            "ActiveDirectory-PowerShell-Modul wurde nicht gefunden.",
            "'Domäne laden' benötigt RSAT / ActiveDirectory PowerShell.");
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
