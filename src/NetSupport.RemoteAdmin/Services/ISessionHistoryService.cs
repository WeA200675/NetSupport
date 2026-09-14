using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Services;

public interface ISessionHistoryService
{
    Task<IReadOnlyList<SessionHistoryEntry>> GetRecentAsync(
        int count = 20,
        CancellationToken cancellationToken = default);

    Task RecordAsync(
        SessionHistoryEntry entry,
        CancellationToken cancellationToken = default);

    Task ExportCsvAsync(
        string destinationPath,
        CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
