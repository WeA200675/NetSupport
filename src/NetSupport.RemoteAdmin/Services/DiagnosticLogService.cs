using System.IO;
using System.Text;

namespace NetSupport.RemoteAdmin.Services;

/// <summary>
/// Small local diagnostic log for operational troubleshooting. The caller decides
/// whether logging is enabled through the supplied predicate. Credentials and
/// screen contents must never be passed to this service.
/// </summary>
public sealed class DiagnosticLogService : IDiagnosticLogService
{
    private const long MaxLogBytes = 2 * 1024 * 1024;
    private readonly object _gate = new();
    private readonly Func<bool> _isEnabled;

    public DiagnosticLogService(string configDirectory, Func<bool> isEnabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configDirectory);
        _isEnabled = isEnabled ?? throw new ArgumentNullException(nameof(isEnabled));
        LogDirectory = Path.Combine(configDirectory, "logs");
        LogPath = Path.Combine(LogDirectory, "application.log");
    }

    public string LogDirectory { get; }
    public string LogPath { get; }

    public void Info(string message) => Write("INFO", message, null);

    public void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        if (!_isEnabled())
            return;

        try
        {
            lock (_gate)
            {
                Directory.CreateDirectory(LogDirectory);
                RotateIfNeeded();

                var safeMessage = Normalize(message);
                var builder = new StringBuilder()
                    .Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"))
                    .Append(" [").Append(level).Append("] ")
                    .Append(safeMessage);

                if (exception is not null)
                {
                    builder.Append(" | ")
                        .Append(exception.GetType().Name)
                        .Append(": ")
                        .Append(Normalize(exception.Message));
                }

                File.AppendAllText(LogPath, builder.AppendLine().ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // Diagnostics must never break the administration application.
        }
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(LogPath))
            return;

        var info = new FileInfo(LogPath);
        if (info.Length < MaxLogBytes)
            return;

        var rotatedPath = LogPath + ".1";
        File.Move(LogPath, rotatedPath, overwrite: true);
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? "(leer)"
            : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
}
