using System.Windows;
using NetSupport.RemoteAdmin.Providers;
using NetSupport.RemoteAdmin.Services;

namespace NetSupport.RemoteAdmin;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var configService = new ConfigService();
            var config = await configService.LoadAsync();

            var registry = new RemoteProviderRegistry(new IRemoteProvider[]
            {
                new NetSupportProvider(config),
                new RdpProvider(config)
            });

            var window = new MainWindow(config, configService, registry);
            MainWindow = window;

            if (!config.StartMinimized)
                window.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "NetSupport Remote Admin", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
