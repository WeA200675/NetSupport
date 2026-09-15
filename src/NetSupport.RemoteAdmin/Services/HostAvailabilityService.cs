using System.Net.NetworkInformation;

namespace NetSupport.RemoteAdmin.Services;

public sealed class HostAvailabilityService
{
    public async Task<bool> IsOnlineAsync(string host, int timeoutMilliseconds = 700, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host))
            return false;

        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, TimeSpan.FromMilliseconds(timeoutMilliseconds), cancellationToken: cancellationToken);
            return reply.Status == IPStatus.Success;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }
}
