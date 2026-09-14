using System.Windows;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Views;

namespace NetSupport.RemoteAdmin.Services;

public sealed class EmbeddedRdpSessionLauncher : IRdpSessionLauncher
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
            window.Show();
        });
    }
}
