using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Services;

public interface IRdpSessionLauncher
{
    bool IsAvailable { get; }
    Task LaunchAsync(RemoteTarget target, CancellationToken cancellationToken = default);
}
