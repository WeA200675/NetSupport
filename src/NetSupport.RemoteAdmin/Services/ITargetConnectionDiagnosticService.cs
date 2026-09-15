using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Services;

public interface ITargetConnectionDiagnosticService
{
    Task<TargetConnectionDiagnosticResult> DiagnoseAsync(
        string host,
        int netSupportPort,
        CancellationToken cancellationToken = default);
}
