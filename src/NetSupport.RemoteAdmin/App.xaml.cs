using System.Windows;
using NetSupport.RemoteAdmin.Providers;
using NetSupport.RemoteAdmin.Services;

namespace NetSupport.RemoteAdmin;

public partial class App : System.Windows.Application
{
    private SingleInstanceGuard? _singleInstanceGuard;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceGuard = SingleInstanceGuard.TryAcquire();
        if (_singleInstanceGuard is null)
        {
            System.Windows.MessageBox.Show(
                "NetSupport Remote Admin läuft in dieser Windows-Sitzung bereits.",
                "NetSupport Remote Admin",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown(0);
            return;
        }

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
            var netSupportReachability = new NetSupportReachabilityService();
            var detailsService = new PowerShellTargetDetailsService();
            var historyService = new JsonSessionHistoryService(configService.ConfigDirectory);
            var window = new MainWindow(
                config,
                configService,
                registry,
                discovery,
                availability,
                netSupportReachability,
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

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceGuard?.Dispose();
        _singleInstanceGuard = null;
        base.OnExit(e);
    }
}
