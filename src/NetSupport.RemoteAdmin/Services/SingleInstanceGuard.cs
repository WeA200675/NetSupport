namespace NetSupport.RemoteAdmin.Services;

internal sealed class SingleInstanceGuard : IDisposable
{
    internal const string DefaultMutexName = @"Local\NetSupport.RemoteAdmin.SingleInstance";

    private readonly Mutex _mutex;
    private bool _disposed;

    private SingleInstanceGuard(Mutex mutex)
    {
        _mutex = mutex;
    }

    internal static SingleInstanceGuard? TryAcquire(string? mutexName = null)
    {
        var name = string.IsNullOrWhiteSpace(mutexName) ? DefaultMutexName : mutexName.Trim();
        var mutex = new Mutex(initiallyOwned: true, name, out var createdNew);
        if (createdNew)
            return new SingleInstanceGuard(mutex);

        mutex.Dispose();
        return null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        try
        {
            _mutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // The mutex can already be released while the process is shutting down.
        }
        finally
        {
            _mutex.Dispose();
        }
    }
}
