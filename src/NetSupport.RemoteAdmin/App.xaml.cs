using System.Windows;
using NetSupport.RemoteAdmin.Providers;
using NetSupport.RemoteAdmin.Services;

namespace NetSupport.RemoteAdmin;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var configService = new ConfigService();
            var config = await configService.LoadAsync();
            var rdpSessionLauncher = new EmbeddedRdpSessionLauncher(config, configService);

            var registry = new RemoteProviderRegistry(new IRemoteProvider[]
            {
                new NetSupportProvider(config),
                new RdpProvider(config, rdpSessionLauncher)
            });

            var discovery = new DomainComputerDiscoveryService();
            var availability = new HostAvailabilityService();
            var window = new MainWindow(config, configService, registry, discovery, availability);
            MainWindow = window;

            if (!config.StartMinimized)
                window.Show();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "NetSupport Remote Admin", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
