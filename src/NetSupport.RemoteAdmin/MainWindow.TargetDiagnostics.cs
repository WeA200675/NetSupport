using System.Windows;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Services;
using NetSupport.RemoteAdmin.Views;

namespace NetSupport.RemoteAdmin;

public partial class MainWindow
{
    private ITargetConnectionDiagnosticService? _targetConnectionDiagnostics;

    private async void TargetDiagnosticsButton_OnClick(object sender, RoutedEventArgs e)
    {
        var target = CurrentTarget;
        if (target is null)
        {
            StatusTextBlock.Text = "Bitte zuerst einen Rechner auswählen oder eingeben.";
            HostTextBox.Focus();
            return;
        }

        if (_targetConnectionDiagnostics is null)
        {
            StatusTextBlock.Text = "Verbindungsdiagnose ist noch nicht initialisiert.";
            return;
        }

        try
        {
            TargetDiagnosticsButton.IsEnabled = false;
            var port = _config.NetSupportClientPort;
            StatusTextBlock.Text = $"Diagnostiziere {target.Host}: DNS, Ping und NetSupport TCP {port} …";

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            var result = await _targetConnectionDiagnostics.DiagnoseAsync(target.Host, port, timeout.Token);

            target.Status = result.PingReachable ? HostStatus.Online : HostStatus.Offline;
            target.NetSupportStatus = result.NetSupportReachable
                ? NetSupportReachabilityStatus.Reachable
                : NetSupportReachabilityStatus.Unreachable;
            target.LastStatusCheck = result.CheckedAt;

            RefreshTargets();
            UpdateSelectedTargetCard(SelectedTarget ?? target);

            _diagnosticLog.Info(
                $"Einzelrechner-Diagnose: {target.Host}; DNS {(result.DnsResolved ? "OK" : "Fehler")}; " +
                $"Ping {(result.PingReachable ? "erreichbar" : "keine Antwort")}; " +
                $"NetSupport TCP {port} {(result.NetSupportReachable ? "erreichbar" : "nicht erreichbar")}.");

            var window = new TargetConnectionDiagnosticsWindow(result)
            {
                Owner = this
            };
            window.ShowDialog();

            StatusTextBlock.Text = $"Diagnose für {target.Host} abgeschlossen.";
        }
        catch (OperationCanceledException)
        {
            _diagnosticLog.Info($"Einzelrechner-Diagnose Zeitlimit/Abbruch: {target.Host}");
            StatusTextBlock.Text = $"Diagnose für {target.Host} wurde abgebrochen oder hat das Zeitlimit erreicht.";
        }
        catch (Exception ex)
        {
            _diagnosticLog.Error($"Einzelrechner-Diagnose fehlgeschlagen: {target.Host}", ex);
            StatusTextBlock.Text = $"Diagnose für {target.Host} fehlgeschlagen.";
            System.Windows.MessageBox.Show(ex.Message, "Verbindungsdiagnose", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            TargetDiagnosticsButton.IsEnabled = true;
        }
    }
}
