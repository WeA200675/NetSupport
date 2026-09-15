using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Services;

public interface ISystemHealthService
{
    Task<IReadOnlyList<SystemHealthCheckResult>> CheckAsync(CancellationToken cancellationToken = default);
}
