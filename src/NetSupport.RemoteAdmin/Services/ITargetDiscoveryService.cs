using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Services;

public interface ITargetDiscoveryService
{
    string DisplayName { get; }
    Task<IReadOnlyList<RemoteTarget>> DiscoverAsync(CancellationToken cancellationToken = default);
}
