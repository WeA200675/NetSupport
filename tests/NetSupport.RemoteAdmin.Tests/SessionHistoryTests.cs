using System.Text;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Services;
using Xunit;

namespace NetSupport.RemoteAdmin.Tests;

public sealed class SessionHistoryTests
{
    [Fact]
    public async Task ExportCsvAsync_NeutralizesSpreadsheetFormulaCells()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = new JsonSessionHistoryService(root);
            await service.RecordAsync(new SessionHistoryEntry
            {
                StartedAt = DateTimeOffset.UtcNow,
                Host = "+1+1",
                TargetName = "=HYPERLINK(\"https://example.invalid\")",
                ProviderName = "NetSupport Manager",
                ProviderId = "netsupport",
                Action = "Control",
                Succeeded = false,
                Error = "@SUM(1,1)\r\nsecond line"
            });

            var csvPath = Path.Combine(root, "history.csv");
            await service.ExportCsvAsync(csvPath);
            var csv = await File.ReadAllTextAsync(csvPath, Encoding.UTF8);

            Assert.Contains("'+1+1", csv, StringComparison.Ordinal);
            Assert.Contains("'=HYPERLINK", csv, StringComparison.Ordinal);
            Assert.Contains("'@SUM(1,1) second line", csv, StringComparison.Ordinal);
            Assert.DoesNotContain("\r\nsecond line", csv, StringComparison.Ordinal);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task RecordAsync_CapsHistoryAtOneHundredEntries()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = new JsonSessionHistoryService(root);
            for (var i = 0; i < 105; i++)
            {
                await service.RecordAsync(new SessionHistoryEntry
                {
                    StartedAt = DateTimeOffset.UtcNow.AddSeconds(i),
                    Host = $"PC-{i:000}",
                    ProviderId = "netsupport",
                    ProviderName = "NetSupport Manager",
                    Action = "Control",
                    Succeeded = true
                });
            }

            var entries = await service.GetRecentAsync(200);
            Assert.Equal(100, entries.Count);
            Assert.Equal("PC-104", entries[0].Host);
            Assert.DoesNotContain(entries, entry => entry.Host == "PC-000");
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task GetRecentAsync_DamagedHistoryDoesNotBlockApplication()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = new JsonSessionHistoryService(root);
            await File.WriteAllTextAsync(service.HistoryPath, "{ damaged-json");

            var entries = await service.GetRecentAsync(20);
            Assert.Empty(entries);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "NetSupportRemoteAdmin-history-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTempDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best effort test cleanup.
        }
    }
}
