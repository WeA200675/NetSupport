using System.Text.Json;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Services;
using Xunit;

namespace NetSupport.RemoteAdmin.Tests;

public sealed class ConfigPersistenceTests
{
    [Fact]
    public async Task SaveAsync_WritesNormalizedPrimaryAndBackup()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = new ConfigService(root);
            var config = new AppConfig
            {
                Targets =
                [
                    new RemoteTarget
                    {
                        Name = " PC-001 ",
                        Host = " PC-001 ",
                        PreferredProviderId = "rdp",
                        PreferredAction = "invalid"
                    }
                ]
            };

            await service.SaveAsync(config);

            Assert.True(File.Exists(service.ConfigPath));
            Assert.True(File.Exists(service.BackupPath));

            var primary = await File.ReadAllTextAsync(service.ConfigPath);
            var backup = await File.ReadAllTextAsync(service.BackupPath);
            Assert.Equal(primary, backup);
            Assert.Contains("netsupport", primary, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Control", primary, StringComparison.Ordinal);
            Assert.DoesNotContain("\"rdp\"", primary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"invalid\"", primary, StringComparison.OrdinalIgnoreCase);

            using var document = JsonDocument.Parse(primary);
            var target = document.RootElement.GetProperty("Targets")[0];
            Assert.Equal("PC-001", target.GetProperty("Name").GetString());
            Assert.Equal("PC-001", target.GetProperty("Host").GetString());
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task LoadAsync_RepairsCorruptPrimaryFromNormalizedBackup()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = new ConfigService(root);
            await service.SaveAsync(new AppConfig
            {
                Targets =
                [
                    new RemoteTarget
                    {
                        Name = "PC-RECOVER",
                        Host = "PC-RECOVER",
                        PreferredProviderId = "netsupport",
                        PreferredAction = "View"
                    }
                ]
            });

            await File.WriteAllTextAsync(service.ConfigPath, "{ this is not valid json");

            var recovered = await service.LoadAsync();
            var target = Assert.Single(recovered.Targets);
            Assert.Equal("PC-RECOVER", target.Host);
            Assert.Equal("netsupport", target.PreferredProviderId);
            Assert.Equal("View", target.PreferredAction);

            // LoadAsync repairs the primary and keeps a valid normalized backup.
            _ = JsonDocument.Parse(await File.ReadAllTextAsync(service.ConfigPath));
            _ = JsonDocument.Parse(await File.ReadAllTextAsync(service.BackupPath));
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task LoadAsync_RecoversWhenPrimaryIsMissingButBackupExists()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = new ConfigService(root);
            await service.SaveAsync(new AppConfig
            {
                Targets = [new RemoteTarget { Name = "PC-ONLY-BACKUP", Host = "PC-ONLY-BACKUP" }]
            });

            File.Delete(service.ConfigPath);
            var recovered = await service.LoadAsync();

            Assert.Equal("PC-ONLY-BACKUP", Assert.Single(recovered.Targets).Host);
            Assert.True(File.Exists(service.ConfigPath));
            Assert.True(File.Exists(service.BackupPath));
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task LoadAsync_ThrowsClearErrorWhenPrimaryAndBackupAreBothCorrupt()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = new ConfigService(root);
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(service.ConfigPath, "{ broken-primary");
            await File.WriteAllTextAsync(service.BackupPath, "{ broken-backup");

            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => service.LoadAsync());
            Assert.Contains("beschädigt", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Backup", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task SaveAsync_LeavesNoTemporaryFilesAfterSuccessfulWrite()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = new ConfigService(root);
            await service.SaveAsync(new AppConfig());

            Assert.Empty(Directory.GetFiles(root, "*.tmp", SearchOption.TopDirectoryOnly));
            Assert.Empty(Directory.GetFiles(root, ".*.tmp", SearchOption.TopDirectoryOnly));
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "NetSupportRemoteAdmin-config-test-" + Guid.NewGuid().ToString("N"));
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
