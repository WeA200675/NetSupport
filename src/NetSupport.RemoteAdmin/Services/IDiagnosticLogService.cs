namespace NetSupport.RemoteAdmin.Services;

public interface IDiagnosticLogService
{
    string LogDirectory { get; }
    string LogPath { get; }

    void Info(string message);
    void Error(string message, Exception? exception = null);
}
