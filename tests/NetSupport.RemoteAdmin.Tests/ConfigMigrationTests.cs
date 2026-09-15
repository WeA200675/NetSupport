using System.Text.Json;
using NetSupport.RemoteAdmin.Services;
using Xunit;

namespace NetSupport.RemoteAdmin.Tests;

public sealed class ConfigMigrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    [Fact]
    public void Normalize_ForcesNetSupportAndFallsBackInvalidActionToControl()
    {
        var config = new AppConfig
        {
            Targets =
            [
                new()
                {
                    Name = " PC-001 ",
                    Host = " PC-001.example.local ",
                    PreferredProviderId = "rdp",
                    PreferredAction = "LaunchSomethingElse"
                }
            ]
        };

        ConfigNormalizer.Normalize(config);

        Assert.Equal(ConfigNormalizer.CurrentSchemaVersion, config.SchemaVersion);
        var target = Assert.Single(config.Targets);
        Assert.Equal("netsupport", target.PreferredProviderId);
        Assert.Equal("Control", target.PreferredAction);
        Assert.Equal("PC-001", target.Name);
        Assert.Equal("PC-001.example.local", target.Host);
    }

    [Theory]
    [InlineData("control", "Control")]
    [InlineData("VIEW", "View")]
    [InlineData(" chat ", "Chat")]
    [InlineData("Inventory", "Inventory")]
    [InlineData("commandprompt", "CommandPrompt")]
    [InlineData("FileTransfer", "FileTransfer")]
    [InlineData("Rdp", "Control")]
    [InlineData("", "Control")]
    public void NormalizePreferredAction_ProducesCanonicalApprovedValue(string input, string expected)
    {
        Assert.Equal(expected, ConfigNormalizer.NormalizePreferredAction(input));
    }

    [Fact]
    public void Normalize_RepairsNullCollectionsFromMalformedOrLegacyJson()
    {
        const string json = """
        {
          "targets": null,
          "savedViews": null
        }
        """;

        var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions)!;
        Assert.Equal(0, config.SchemaVersion);

        ConfigNormalizer.Normalize(config);

        Assert.Equal(ConfigNormalizer.CurrentSchemaVersion, config.SchemaVersion);
        Assert.NotNull(config.Targets);
        Assert.Empty(config.Targets);
        Assert.NotNull(config.SavedViews);
        Assert.Empty(config.SavedViews);
    }

    [Fact]
    public void Normalize_RejectsConfigurationFromNewerSchema()
    {
        var config = new AppConfig
        {
            SchemaVersion = ConfigNormalizer.CurrentSchemaVersion + 1
        };

        var exception = Assert.Throws<InvalidDataException>(() => ConfigNormalizer.Normalize(config));
        Assert.Contains("Schema-Version", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains((ConfigNormalizer.CurrentSchemaVersion + 1).ToString(), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LegacyRdpFields_AreIgnoredAndDisappearWhenConfigurationIsReserialized()
    {
        const string legacyJson = """
        {
          "netSupportExecutable": "  C:\\Program Files\\NetSupport\\NetSupport Manager\\PCICTLUI.EXE  ",
          "useEmbeddedRdp": true,
          "useFullScreenRdp": true,
          "targets": [
            {
              "name": "PC-001",
              "host": "PC-001",
              "preferredProviderId": "rdp",
              "preferredAction": "NotARealAction",
              "rdpUserName": "legacy-user",
              "rdpDomain": "legacy-domain",
              "rdpUseMultiMonitor": true
            }
          ]
        }
        """;

        var config = JsonSerializer.Deserialize<AppConfig>(legacyJson, JsonOptions)!;
        Assert.Equal(0, config.SchemaVersion);

        ConfigNormalizer.Normalize(config);
        var migratedJson = JsonSerializer.Serialize(config, JsonOptions);

        Assert.DoesNotContain("useEmbeddedRdp", migratedJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("useFullScreenRdp", migratedJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rdpUserName", migratedJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rdpDomain", migratedJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rdpUseMultiMonitor", migratedJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"\"SchemaVersion\": {ConfigNormalizer.CurrentSchemaVersion}", migratedJson, StringComparison.Ordinal);

        var target = Assert.Single(config.Targets);
        Assert.Equal("netsupport", target.PreferredProviderId);
        Assert.Equal("Control", target.PreferredAction);
        Assert.Equal(@"C:\Program Files\NetSupport\NetSupport Manager\PCICTLUI.EXE", config.NetSupportExecutable);
    }
}
