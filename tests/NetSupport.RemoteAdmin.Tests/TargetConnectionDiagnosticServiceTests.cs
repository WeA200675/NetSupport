using NetSupport.RemoteAdmin.Services;
using Xunit;

namespace NetSupport.RemoteAdmin.Tests;

public sealed class TargetConnectionDiagnosticServiceTests
{
    [Fact]
    public async Task DiagnoseAsync_RejectsEmptyTargetBeforeAnyNetworkProbe()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.DiagnoseAsync("   ", 5405));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public async Task DiagnoseAsync_RejectsInvalidPortBeforeAnyNetworkProbe(int port)
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.DiagnoseAsync("PC-001", port));
    }

    private static TargetConnectionDiagnosticService CreateService() => new(
        new HostAvailabilityService(),
        new NetSupportReachabilityService());
}
