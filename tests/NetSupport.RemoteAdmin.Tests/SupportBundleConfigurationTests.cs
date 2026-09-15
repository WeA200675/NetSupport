using System.IO.Compression;
using System.Text.Json;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Services;
using Xunit;

namespace NetSupport.RemoteAdmin.Tests;

public sealed class SupportBundleConfigurationTests
{
    [Fact]
    public async Task ConfigurationSummary_IncludesConfiguredNetSupportClientPort()
    {
        var root = Path.Combine(Path.GetTempPath(), "NetSupportRemoteAdmin-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var logPath = Path.Combine(root, "application.log");
            await File.WriteAllTextAsync(logPath, "[INFO] test\n");

            var config = new AppConfig
            {
                NetSupportClientPort = 15405,
                Targets = [new RemoteTarget { Name = "PC-001", Host = "PC-001" }]
            };

            var service = new SupportBundleService(
                config,
                new ConfigService(),
                new EmptyHistoryService(),
                new FakeDiagnosticLogService(logPath));

            var zipPath = Path.Combine(root, "support.zip");
            await service.CreateAsync(zipPath, anonymizeIdentifiers: true);

            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.GetEntry("configuration-summary.json");
            Assert.NotNull(entry);

            using var stream = entry!.Open();
            using var document = await JsonDocument.ParseAsync(stream);
            Assert.Equal(15405, document.RootElement.GetProperty("netSupportClientPort").GetInt32());
        }
        finally
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch
            {
                // Best effort test cleanup.
            }
        }
    }

    private sealed class EmptyHistoryService : ISessionHistoryService
    {
        public Task<IReadOnlyList<SessionHistoryEntry>> GetRecentAsync(
            int count = 20,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SessionHistoryEntry>>([]);

        public Task RecordAsync(SessionHistoryEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ExportCsvAsync(string destinationPath, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ClearAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeDiagnosticLogService(string logPath) : IDiagnosticLogService
    {
        public string LogDirectory => Path.GetDirectoryName(logPath)!;
        public string LogPath => logPath;
        public void Info(string message) { }
        public void Error(string message, Exception? exception = null) { }
    }
}
