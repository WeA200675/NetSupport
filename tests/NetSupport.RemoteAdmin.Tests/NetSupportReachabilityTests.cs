using System.Net;
using System.Net.Sockets;
using NetSupport.RemoteAdmin.Services;
using Xunit;

namespace NetSupport.RemoteAdmin.Tests;

public sealed class NetSupportReachabilityTests
{
    [Fact]
    public async Task IsReachableAsync_ReturnsTrueForListeningTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var service = new NetSupportReachabilityService();
        var reachable = await service.IsReachableAsync("127.0.0.1", port, timeoutMilliseconds: 1000);

        Assert.True(reachable);
    }

    [Fact]
    public async Task IsReachableAsync_ReturnsFalseForClosedTcpPort()
    {
        int port;
        using (var listener = new TcpListener(IPAddress.Loopback, 0))
        {
            listener.Start();
            port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
        }

        var service = new NetSupportReachabilityService();
        var reachable = await service.IsReachableAsync("127.0.0.1", port, timeoutMilliseconds: 500);

        Assert.False(reachable);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void ValidatePort_RejectsOutOfRangeValues(int port)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NetSupportReachabilityService.ValidatePort(port));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5405)]
    [InlineData(65535)]
    public void ValidatePort_AcceptsTcpPortRange(int port)
    {
        NetSupportReachabilityService.ValidatePort(port);
    }
}
