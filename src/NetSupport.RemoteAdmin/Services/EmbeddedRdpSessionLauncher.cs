using System.Windows;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Views;

namespace NetSupport.RemoteAdmin.Services;

public sealed class EmbeddedRdpSessionLauncher(
    AppConfig config,
    ConfigService configService) : IRdpSessionLauncher
{
    public bool IsAvailable => OperatingSystem.IsWindowsVersionAtLeast(10);

    public async Task LaunchAsync(RemoteTarget target, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsAvailable)
            throw new PlatformNotSupportedException("Der eingebettete RDP-Client benötigt Windows 10 oder neuer.");

        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var window = new RdpSessionWindow(target)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            window.Closed += async (_, _) =>
            {
                var savedTarget = config.Targets.FirstOrDefault(t =>
                    string.Equals(t.Host, target.Host, StringComparison.OrdinalIgnoreCase));

                if (savedTarget is null)
                    return;

                savedTarget.RdpUserName = target.RdpUserName;
                savedTarget.RdpDomain = target.RdpDomain;
                savedTarget.RdpRedirectClipboard = target.RdpRedirectClipboard;
                savedTarget.RdpAdminSession = target.RdpAdminSession;
                savedTarget.RdpUseMultiMonitor = target.RdpUseMultiMonitor;

                try
                {
                    await configService.SaveAsync(config);
                }
                catch
                {
                    // Session shutdown must not be blocked by a configuration write failure.
                    // The next explicit config save can persist the RDP preference values.
                }
            };

            window.Show();
        });
    }
}
