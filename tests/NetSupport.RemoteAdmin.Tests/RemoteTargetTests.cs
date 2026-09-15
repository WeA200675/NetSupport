using NetSupport.RemoteAdmin.Models;
using Xunit;

namespace NetSupport.RemoteAdmin.Tests;

public sealed class RemoteTargetTests
{
    [Theory]
    [InlineData(HostStatus.Unknown, "Unbekannt")]
    [InlineData(HostStatus.Online, "Online")]
    [InlineData(HostStatus.Offline, "Nicht erreichbar")]
    public void StatusText_DoesNotOverstatePingFailure(HostStatus status, string expected)
    {
        var target = new RemoteTarget { Status = status };
        Assert.Equal(expected, target.StatusText);
    }

    [Theory]
    [InlineData(HostStatus.Online, NetSupportReachabilityStatus.Reachable, "Ping erreichbar · NetSupport erreichbar")]
    [InlineData(HostStatus.Online, NetSupportReachabilityStatus.Unreachable, "Ping erreichbar · NetSupport nicht erreichbar")]
    [InlineData(HostStatus.Offline, NetSupportReachabilityStatus.Reachable, "Ping keine Antwort · NetSupport erreichbar")]
    [InlineData(HostStatus.Offline, NetSupportReachabilityStatus.Unreachable, "Ping keine Antwort · NetSupport nicht erreichbar")]
    [InlineData(HostStatus.Unknown, NetSupportReachabilityStatus.Reachable, "Ping nicht geprüft · NetSupport erreichbar")]
    [InlineData(HostStatus.Unknown, NetSupportReachabilityStatus.Unreachable, "Ping nicht geprüft · NetSupport nicht erreichbar")]
    public void StatusText_CombinesPingAndNetSupportWithoutInferringPowerState(
        HostStatus pingStatus,
        NetSupportReachabilityStatus netSupportStatus,
        string expected)
    {
        var target = new RemoteTarget
        {
            Status = pingStatus,
            NetSupportStatus = netSupportStatus
        };

        Assert.Equal(expected, target.StatusText);
    }

    [Theory]
    [InlineData(NetSupportReachabilityStatus.Unknown, "NetSupport nicht geprüft")]
    [InlineData(NetSupportReachabilityStatus.Reachable, "NetSupport erreichbar")]
    [InlineData(NetSupportReachabilityStatus.Unreachable, "NetSupport nicht erreichbar")]
    public void NetSupportStatusText_UsesDiagnosticWording(
        NetSupportReachabilityStatus status,
        string expected)
    {
        var target = new RemoteTarget { NetSupportStatus = status };
        Assert.Equal(expected, target.NetSupportStatusText);
    }
}
