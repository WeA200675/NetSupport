using System.Windows;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Services;

namespace NetSupport.RemoteAdmin.Views;

public partial class SystemHealthWindow : Window
{
    private readonly ISystemHealthService _healthService;
    private readonly IDiagnosticLogService _diagnosticLog;
    private CancellationTokenSource? _refreshCancellation;

    public SystemHealthWindow(
        ISystemHealthService healthService,
        IDiagnosticLogService diagnosticLog)
    {
        InitializeComponent();
        _healthService = healthService;
        _diagnosticLog = diagnosticLog;

        Loaded += async (_, _) => await RefreshAsync();
        Closing += (_, _) =>
        {
            _refreshCancellation?.Cancel();
            _refreshCancellation?.Dispose();
        };
    }

    private async void RefreshButton_OnClick(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        _refreshCancellation?.Cancel();
        _refreshCancellation?.Dispose();
        _refreshCancellation = new CancellationTokenSource();

        try
        {
            RefreshButton.IsEnabled = false;
            SummaryTextBlock.Text = "Prüfe lokale Voraussetzungen …";
            CheckedAtTextBlock.Text = string.Empty;
            ChecksListBox.ItemsSource = null;

            var results = await _healthService.CheckAsync(_refreshCancellation.Token);
            ChecksListBox.ItemsSource = results;

            var errors = results.Count(result => result.Level == SystemHealthLevel.Error);
            var warnings = results.Count(result => result.Level == SystemHealthLevel.Warning);
            var healthy = results.Count(result => result.Level == SystemHealthLevel.Healthy);

            SummaryTextBlock.Text = errors > 0
                ? $"{errors} Fehler, {warnings} Hinweise, {healthy} Prüfungen OK"
                : warnings > 0
                    ? $"Keine Fehler, {warnings} Hinweise, {healthy} Prüfungen OK"
                    : $"Alle relevanten Prüfungen OK ({healthy})";
            CheckedAtTextBlock.Text = $"Geprüft: {DateTime.Now:dd.MM.yyyy HH:mm:ss}";
            _diagnosticLog.Info($"Systemzustand geprüft: {errors} Fehler, {warnings} Hinweise.");
        }
        catch (OperationCanceledException)
        {
            SummaryTextBlock.Text = "Prüfung abgebrochen.";
        }
        catch (Exception ex)
        {
            _diagnosticLog.Error("Systemzustand konnte nicht geprüft werden.", ex);
            SummaryTextBlock.Text = "Systemzustand konnte nicht geprüft werden.";
            System.Windows.MessageBox.Show(
                ex.Message,
                "Systemzustand",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }
}
