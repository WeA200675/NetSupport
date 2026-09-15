using System.Windows;
using NetSupport.RemoteAdmin.Providers;
using NetSupport.RemoteAdmin.Services;

namespace NetSupport.RemoteAdmin;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        IDiagnosticLogService? diagnosticLog = null;
        try
        {
            var configService = new ConfigService();
            var config = await configService.LoadAsync();
            diagnosticLog = new DiagnosticLogService(
                configService.ConfigDirectory,
                () => config.DiagnosticLoggingEnabled);
            var autoStartService = new WindowsAutoStartService();

            DispatcherUnhandledException += (_, args) =>
                diagnosticLog.Error("Unbehandelte UI-Ausnahme.", args.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                diagnosticLog.Error("Unbehandelte AppDomain-Ausnahme.", args.ExceptionObject as Exception);
            TaskScheduler.UnobservedTaskException += (_, args) =>
                diagnosticLog.Error("Nicht beobachtete Task-Ausnahme.", args.Exception);

            diagnosticLog.Info("Anwendung gestartet. Remotezugriffsrichtlinie: NetSupport-only; RDP ist nicht registriert.");

            // Domain policy: only NetSupport is registered as an executable remote provider.
            var registry = new RemoteProviderRegistry(new IRemoteProvider[]
            {
                new NetSupportProvider(config, diagnosticLog)
            });

            var discovery = new DomainComputerDiscoveryService();
            var availability = new HostAvailabilityService();
            var detailsService = new PowerShellTargetDetailsService();
            var historyService = new JsonSessionHistoryService(configService.ConfigDirectory);
            var window = new MainWindow(
                config,
                configService,
                registry,
                discovery,
                availability,
                detailsService,
                historyService,
                autoStartService,
                diagnosticLog);
            MainWindow = window;

            if (!config.StartMinimized)
                window.Show();
        }
        catch (Exception ex)
        {
            diagnosticLog?.Error("Anwendungsstart fehlgeschlagen.", ex);
            System.Windows.MessageBox.Show(ex.Message, "NetSupport Remote Admin", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
