using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Services;

public interface ITargetDetailsService
{
    string DisplayName { get; }

    Task<RemoteTargetDetails> GetDetailsAsync(
        RemoteTarget target,
        CancellationToken cancellationToken = default);
}
