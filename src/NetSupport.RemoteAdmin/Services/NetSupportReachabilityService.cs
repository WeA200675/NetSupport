using System.Net.Sockets;

namespace NetSupport.RemoteAdmin.Services;

/// <summary>
/// Performs a lightweight TCP reachability check against the NetSupport Client listening port.
/// This is diagnostic only and never blocks an actual NetSupport launch.
/// </summary>
public sealed class NetSupportReachabilityService
{
    public const int DefaultClientPort = 5405;

    public async Task<bool> IsReachableAsync(
        string host,
        int port,
        int timeoutMilliseconds = 1000,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host))
            return false;

        ValidatePort(port);
        if (timeoutMilliseconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(timeoutMilliseconds));

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host.Trim(), port, timeout.Token);
            return client.Connected;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (SocketException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static void ValidatePort(int port)
    {
        if (port is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), "Der NetSupport-Client-Port muss zwischen 1 und 65535 liegen.");
    }
}
