using System.IO.Compression;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Services;
using Xunit;

namespace NetSupport.RemoteAdmin.Tests;

public sealed class SupportBundlePrivacyTests
{
    [Fact]
    public async Task AnonymizedBundle_RemovesKnownIdentifiersAndNeverCopiesSettingsJson()
    {
        var root = Path.Combine(Path.GetTempPath(), "NetSupportRemoteAdmin-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var logPath = Path.Combine(root, "application.log");
            await File.WriteAllTextAsync(
                logPath,
                "[INFO] NetSupport CLI: PCICTLUI.EXE /n \"Secret Helpdesk Profile\" /c SECRET-PC.example.local /vc /e\n" +
                "[ERROR] Verbindung zu SECRET-PC.example.local für Secret Workstation fehlgeschlagen.\n");

            var config = new AppConfig
            {
                NetSupportProfileName = "Secret Helpdesk Profile",
                Targets =
                [
                    new RemoteTarget
                    {
                        Name = "Secret Workstation",
                        Host = "SECRET-PC.example.local",
                        Description = "Accounting workstation",
                        Group = "Secret Finance Group",
                        PreferredProviderId = "netsupport",
                        PreferredAction = "Control"
                    }
                ],
                SavedViews =
                [
                    new SavedTargetView
                    {
                        Name = "Secret View",
                        SearchText = "SECRET-PC",
                        Group = "Secret Finance Group",
                        FavoritesOnly = true
                    }
                ]
            };

            var history = new FakeHistoryService(
            [
                new SessionHistoryEntry
                {
                    StartedAt = DateTimeOffset.UtcNow,
                    Host = "SECRET-PC.example.local",
                    TargetName = "Secret Workstation",
                    ProviderId = "netsupport",
                    ProviderName = "NetSupport Manager",
                    Action = "Control",
                    Succeeded = false,
                    Error = "Secret Workstation / SECRET-PC.example.local konnte nicht gestartet werden."
                }
            ]);

            var diagnosticLog = new FakeDiagnosticLogService(logPath);
            var service = new SupportBundleService(config, new ConfigService(), history, diagnosticLog);
            var zipPath = Path.Combine(root, "support.zip");

            await service.CreateAsync(zipPath, anonymizeIdentifiers: true);

            using var archive = ZipFile.OpenRead(zipPath);
            Assert.DoesNotContain(archive.Entries, entry =>
                string.Equals(entry.FullName, "settings.json", StringComparison.OrdinalIgnoreCase));

            var text = string.Join("\n", archive.Entries
                .Where(entry => !string.IsNullOrEmpty(entry.Name))
                .Select(ReadEntry));

            Assert.DoesNotContain("SECRET-PC.example.local", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Secret Workstation", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Secret Finance Group", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Secret Helpdesk Profile", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Secret View", text, StringComparison.OrdinalIgnoreCase);

            Assert.Contains("target-001", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("group-001", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("netsupport-profile", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("NetSupport-only", text, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public async Task AnonymizedBundle_DoesNotIntroduceCredentialFields()
    {
        var root = Path.Combine(Path.GetTempPath(), "NetSupportRemoteAdmin-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var logPath = Path.Combine(root, "application.log");
            await File.WriteAllTextAsync(logPath, "[INFO] ordinary diagnostic line\n");

            var service = new SupportBundleService(
                new AppConfig(),
                new ConfigService(),
                new FakeHistoryService([]),
                new FakeDiagnosticLogService(logPath));
            var zipPath = Path.Combine(root, "support.zip");

            await service.CreateAsync(zipPath, anonymizeIdentifiers: true);

            using var archive = ZipFile.OpenRead(zipPath);
            var text = string.Join("\n", archive.Entries
                .Where(entry => !string.IsNullOrEmpty(entry.Name))
                .Select(ReadEntry));

            Assert.DoesNotContain("\"password\"", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"credential\"", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"token\"", text, StringComparison.OrdinalIgnoreCase);
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

    private static string ReadEntry(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private sealed class FakeHistoryService(IReadOnlyList<SessionHistoryEntry> entries) : ISessionHistoryService
    {
        public Task<IReadOnlyList<SessionHistoryEntry>> GetRecentAsync(
            int count = 20,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SessionHistoryEntry>>(entries.Take(count).ToList());

        public Task RecordAsync(SessionHistoryEntry entry, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ExportCsvAsync(string destinationPath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ClearAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeDiagnosticLogService(string logPath) : IDiagnosticLogService
    {
        public string LogDirectory => Path.GetDirectoryName(logPath)!;
        public string LogPath => logPath;

        public void Info(string message)
        {
        }

        public void Error(string message, Exception? exception = null)
        {
        }
    }
}
