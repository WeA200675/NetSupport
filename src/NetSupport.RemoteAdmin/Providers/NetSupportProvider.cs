using System.Diagnostics;
using System.IO;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Services;

namespace NetSupport.RemoteAdmin.Providers;

public sealed class NetSupportProvider(
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
        var profile = ValidateConfiguredProfile();

        if (!SupportedActions.Contains(action))
            throw new NotSupportedException($"Die Aktion '{action}' wird von NetSupport nicht unterstützt.");

        var arguments = NetSupportCommandLine.BuildArguments(
            target.Host,
            action,
            profile,
            config.NetSupportLockProfile);

        // NetSupport documents an unusual compact /c\">address\" syntax for IP connections.
        // The dedicated builder validates every raw command-line value before it reaches this point.
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

    private string GetValidatedExecutablePath() =>
        NetSupportCommandLine.ValidateExecutablePath(config.NetSupportExecutable);
}
