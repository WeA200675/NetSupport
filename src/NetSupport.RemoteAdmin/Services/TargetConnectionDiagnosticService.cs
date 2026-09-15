using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Services;

/// <summary>
/// Read-only diagnostics for one target. It resolves DNS, checks ICMP and tests the configured
/// NetSupport client TCP port. It never starts PCICTLUI.EXE and never changes the target.
/// </summary>
public sealed class TargetConnectionDiagnosticService(
    HostAvailabilityService availability,
    NetSupportReachabilityService netSupportReachability) : ITargetConnectionDiagnosticService
{
    public async Task<TargetConnectionDiagnosticResult> DiagnoseAsync(
        string host,
        int netSupportPort,
        CancellationToken cancellationToken = default)
    {
        var normalizedHost = host?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedHost))
            throw new ArgumentException("Ein Rechnername oder eine IP-Adresse ist erforderlich.", nameof(host));

        NetSupportReachabilityService.ValidatePort(netSupportPort);

        var dnsTask = ResolveDnsAsync(normalizedHost, cancellationToken);
        var pingTask = MeasureAsync(
            () => availability.IsOnlineAsync(normalizedHost, timeoutMilliseconds: 1200, cancellationToken),
            cancellationToken);
        var netSupportTask = MeasureAsync(
            () => netSupportReachability.IsReachableAsync(
                normalizedHost,
                netSupportPort,
                timeoutMilliseconds: 1500,
                cancellationToken),
            cancellationToken);

        await Task.WhenAll(dnsTask, pingTask, netSupportTask);

        var dns = await dnsTask;
        var ping = await pingTask;
        var netSupport = await netSupportTask;

        return new TargetConnectionDiagnosticResult
        {
            Host = normalizedHost,
            NetSupportPort = netSupportPort,
            CheckedAt = DateTimeOffset.Now,
            IpAddresses = dns.Addresses,
            DnsError = dns.Error,
            PingReachable = ping.Reachable,
            NetSupportReachable = netSupport.Reachable,
            DnsDurationMilliseconds = dns.DurationMilliseconds,
            PingDurationMilliseconds = ping.DurationMilliseconds,
            NetSupportDurationMilliseconds = netSupport.DurationMilliseconds
        };
    }

    private static async Task<DnsOutcome> ResolveDnsAsync(
        string host,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(2500));

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, timeout.Token);
            var values = addresses
                .OrderBy(address => address.AddressFamily == AddressFamily.InterNetwork ? 0 : 1)
                .ThenBy(address => address.ToString(), StringComparer.OrdinalIgnoreCase)
                .Select(address => address.ToString())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new DnsOutcome(values, null, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new DnsOutcome(Array.Empty<string>(), "DNS-Zeitlimit erreicht", stopwatch.ElapsedMilliseconds);
        }
        catch (SocketException ex)
        {
            return new DnsOutcome(Array.Empty<string>(), $"DNS-Fehler: {ex.SocketErrorCode}", stopwatch.ElapsedMilliseconds);
        }
        catch (ArgumentException ex)
        {
            return new DnsOutcome(Array.Empty<string>(), $"DNS-Eingabe ungültig: {ex.Message}", stopwatch.ElapsedMilliseconds);
        }
    }

    private static async Task<ProbeOutcome> MeasureAsync(
        Func<Task<bool>> probe,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stopwatch = Stopwatch.StartNew();
        var reachable = await probe();
        return new ProbeOutcome(reachable, stopwatch.ElapsedMilliseconds);
    }

    private sealed record DnsOutcome(
        IReadOnlyList<string> Addresses,
        string? Error,
        long DurationMilliseconds);

    private sealed record ProbeOutcome(bool Reachable, long DurationMilliseconds);
}
