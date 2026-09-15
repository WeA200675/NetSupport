using System.Windows;
using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Views;

public partial class TargetConnectionDiagnosticsWindow : Window
{
    private readonly TargetConnectionDiagnosticResult _result;

    public TargetConnectionDiagnosticsWindow(TargetConnectionDiagnosticResult result)
    {
        InitializeComponent();
        _result = result ?? throw new ArgumentNullException(nameof(result));

        Title = $"Verbindungsdiagnose – {_result.Host}";
        TargetTextBlock.Text = $"Ziel: {_result.Host}";
        SummaryTextBlock.Text = _result.SummaryText;
        DnsTextBlock.Text = $"{_result.DnsStatusText} · {_result.DnsDurationMilliseconds} ms";
        PingTextBlock.Text = $"{_result.PingStatusText} · {_result.PingDurationMilliseconds} ms";
        NetSupportTextBlock.Text = $"{_result.NetSupportStatusText} · {_result.NetSupportDurationMilliseconds} ms";
        CheckedAtTextBlock.Text = _result.CheckedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");
    }

    private void CopyButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Windows.Clipboard.SetText(_result.ToReportText());
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Diagnose konnte nicht in die Zwischenablage kopiert werden: {ex.Message}",
                "Verbindungsdiagnose",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e) => Close();
}
