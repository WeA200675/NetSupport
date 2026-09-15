using System.Windows;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Services;

namespace NetSupport.RemoteAdmin;

public partial class MainWindow
{
    private NetSupportReachabilityService? _netSupportReachability;

    internal void InitializeNetSupportReachability(NetSupportReachabilityService service)
    {
        _netSupportReachability = service ?? throw new ArgumentNullException(nameof(service));

        // Replace the original ping-only handler without changing the XAML contract.
        RefreshStatusButton.Click -= RefreshStatusButton_OnClick;
        RefreshStatusButton.Click += RefreshCombinedStatusButton_OnClick;
    }

    private async void RefreshCombinedStatusButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_netSupportReachability is null)
            return;

        _statusCancellation?.Cancel();
        _statusCancellation?.Dispose();
        _statusCancellation = new CancellationTokenSource();
        var cancellationToken = _statusCancellation.Token;
        var clientPort = _config.NetSupportClientPort;

        try
        {
            RefreshStatusButton.IsEnabled = false;
            StatusTextBlock.Text = $"Prüfe {_targets.Count} Rechner: Ping + NetSupport TCP {clientPort} …";

            using var gate = new SemaphoreSlim(12);
            var tasks = _targets.Select(async target =>
            {
                await gate.WaitAsync(cancellationToken);
                try
                {
                    var pingTask = _availability.IsOnlineAsync(
                        target.Host,
                        cancellationToken: cancellationToken);
                    var netSupportTask = _netSupportReachability.IsReachableAsync(
                        target.Host,
                        clientPort,
                        timeoutMilliseconds: 1200,
                        cancellationToken);

                    await Task.WhenAll(pingTask, netSupportTask);
                    target.Status = await pingTask ? HostStatus.Online : HostStatus.Offline;
                    target.NetSupportStatus = await netSupportTask
                        ? NetSupportReachabilityStatus.Reachable
                        : NetSupportReachabilityStatus.Unreachable;
                    target.LastStatusCheck = DateTimeOffset.Now;
                }
                finally
                {
                    gate.Release();
                }
            });

            await Task.WhenAll(tasks);
            RefreshTargets();
            UpdateSelectedTargetCard(SelectedTarget);

            var pingReachable = _targets.Count(target => target.Status == HostStatus.Online);
            var netSupportReachable = _targets.Count(target =>
                target.NetSupportStatus == NetSupportReachabilityStatus.Reachable);

            _diagnosticLog.Info(
                $"Statusprüfung beendet: Ping {pingReachable}/{_targets.Count}; NetSupport TCP {clientPort} {netSupportReachable}/{_targets.Count} erreichbar.");
            StatusTextBlock.Text =
                $"Status aktualisiert: Ping {pingReachable}/{_targets.Count}, NetSupport TCP {clientPort} {netSupportReachable}/{_targets.Count} erreichbar.";
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = "Statusprüfung abgebrochen.";
        }
        finally
        {
            RefreshStatusButton.IsEnabled = true;
        }
    }
}
