using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Tests;

public sealed class TargetConnectionDiagnosticResultTests
{
    [Fact]
    public void StatusTexts_KeepPingAndNetSupportIndependent()
    {
        var result = new TargetConnectionDiagnosticResult
        {
            Host = "PC-001",
            NetSupportPort = 5405,
            CheckedAt = DateTimeOffset.UtcNow,
            IpAddresses = new[] { "10.20.30.40" },
            PingReachable = false,
            NetSupportReachable = true
        };

        Assert.Equal("Ping keine Antwort", result.PingStatusText);
        Assert.Equal("TCP 5405 erreichbar", result.NetSupportStatusText);
        Assert.Equal("Der konfigurierte NetSupport-Client-Port ist erreichbar.", result.SummaryText);
        Assert.DoesNotContain("offline", result.ToReportText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DnsStatusText_UsesResolvedAddresses()
    {
        var result = new TargetConnectionDiagnosticResult
        {
            Host = "PC-001",
            NetSupportPort = 5405,
            CheckedAt = DateTimeOffset.UtcNow,
            IpAddresses = new[] { "10.20.30.40", "fe80::1" }
        };

        Assert.True(result.DnsResolved);
        Assert.Equal("10.20.30.40, fe80::1", result.DnsStatusText);
    }

    [Fact]
    public void DnsStatusText_PreservesDiagnosticErrorWithoutClaimingPowerState()
    {
        var result = new TargetConnectionDiagnosticResult
        {
            Host = "missing.example.invalid",
            NetSupportPort = 5405,
            CheckedAt = DateTimeOffset.UtcNow,
            DnsError = "DNS-Fehler: HostNotFound",
            PingReachable = false,
            NetSupportReachable = false
        };

        Assert.False(result.DnsResolved);
        Assert.Equal("DNS-Fehler: HostNotFound", result.DnsStatusText);
        Assert.DoesNotContain("ausgeschaltet", result.SummaryText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("offline", result.SummaryText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Report_ContainsTargetPortAndProbeDurations()
    {
        var result = new TargetConnectionDiagnosticResult
        {
            Host = "PC-001",
            NetSupportPort = 5505,
            CheckedAt = new DateTimeOffset(2026, 9, 15, 7, 30, 0, TimeSpan.Zero),
            IpAddresses = new[] { "10.20.30.40" },
            PingReachable = true,
            NetSupportReachable = false,
            DnsDurationMilliseconds = 12,
            PingDurationMilliseconds = 25,
            NetSupportDurationMilliseconds = 1501
        };

        var report = result.ToReportText();

        Assert.Contains("PC-001", report);
        Assert.Contains("TCP 5505 nicht erreichbar", report);
        Assert.Contains("12 ms", report);
        Assert.Contains("25 ms", report);
        Assert.Contains("1501 ms", report);
    }
}
