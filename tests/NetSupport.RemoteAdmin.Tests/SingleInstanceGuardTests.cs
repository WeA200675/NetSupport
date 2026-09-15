using NetSupport.RemoteAdmin.Services;
using Xunit;

namespace NetSupport.RemoteAdmin.Tests;

public sealed class SingleInstanceGuardTests
{
    [Fact]
    public void TryAcquire_AllowsOnlyOneOwnerForSameSessionName()
    {
        var mutexName = @"Local\NetSupport.RemoteAdmin.Tests." + Guid.NewGuid().ToString("N");

        using var first = SingleInstanceGuard.TryAcquire(mutexName);
        var second = SingleInstanceGuard.TryAcquire(mutexName);

        Assert.NotNull(first);
        Assert.Null(second);
    }

    [Fact]
    public void TryAcquire_AllowsNewOwnerAfterPreviousGuardIsDisposed()
    {
        var mutexName = @"Local\NetSupport.RemoteAdmin.Tests." + Guid.NewGuid().ToString("N");

        var first = SingleInstanceGuard.TryAcquire(mutexName);
        Assert.NotNull(first);
        first.Dispose();

        using var second = SingleInstanceGuard.TryAcquire(mutexName);
        Assert.NotNull(second);
    }
}
