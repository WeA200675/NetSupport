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
}
