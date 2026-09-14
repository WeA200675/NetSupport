using System.Diagnostics;
using System.IO;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Services;

namespace NetSupport.RemoteAdmin.Providers;

public sealed class RdpProvider(
    AppConfig config,
    IRdpSessionLauncher sessionLauncher,
    IRdpConnectionFileService connectionFileService) : IRemoteProvider
{
    public string Id => "rdp";
    public string DisplayName => config.UseEmbeddedRdp
        ? "Windows Remote Desktop (eingebettet)"
        : "Windows Remote Desktop";

    public IReadOnlyCollection<RemoteAction> SupportedActions { get; } = new[] { RemoteAction.Control };

    public bool IsAvailable => sessionLauncher.IsAvailable
                               || File.Exists(Path.Combine(Environment.SystemDirectory, "mstsc.exe"));

    public async Task ConnectAsync(RemoteTarget target, RemoteAction action, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (action != RemoteAction.Control)
            throw new NotSupportedException("RDP unterstützt in diesem Frontend nur die Aktion 'Control'.");

        if (string.IsNullOrWhiteSpace(target.Host))
            throw new ArgumentException("Für die Verbindung ist ein Rechnername oder eine IP-Adresse erforderlich.", nameof(target));

        var selectedMonitors = connectionFileService.NormalizeMonitorIds(target.RdpSelectedMonitors);

        // Targeted monitor selection is currently expressed through the documented
        // selectedmonitors RDP-file setting. Normal sessions stay embedded when enabled.
        if (selectedMonitors is null && config.UseEmbeddedRdp && sessionLauncher.IsAvailable)
        {
            await sessionLauncher.LaunchAsync(target, cancellationToken);
            return;
        }

        LaunchExternalClient(target, selectedMonitors);
    }

    private void LaunchExternalClient(RemoteTarget target, string? selectedMonitors)
    {
        var mstscPath = Path.Combine(Environment.SystemDirectory, "mstsc.exe");
        if (!File.Exists(mstscPath))
            throw new InvalidOperationException("Windows Remote Desktop (mstsc.exe) wurde nicht gefunden.");

        var psi = new ProcessStartInfo
        {
            FileName = mstscPath,
            UseShellExecute = true
        };

        if (selectedMonitors is not null)
        {
            target.RdpSelectedMonitors = selectedMonitors;
            var connectionFile = connectionFileService.CreateSelectedMonitorsFile(target, config.UseFullScreenRdp);
            psi.ArgumentList.Add(connectionFile);
        }
        else
        {
            psi.ArgumentList.Add($"/v:{target.Host}");

            if (target.RdpUseMultiMonitor)
                psi.ArgumentList.Add("/multimon");
            else if (config.UseFullScreenRdp)
                psi.ArgumentList.Add("/f");
        }

        if (target.RdpAdminSession)
            psi.ArgumentList.Add("/admin");

        _ = Process.Start(psi) ?? throw new InvalidOperationException("Remote Desktop konnte nicht gestartet werden.");
    }
}
