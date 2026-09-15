using NetSupport.RemoteAdmin.Services;
using Xunit;

namespace NetSupport.RemoteAdmin.Tests;

public sealed class DomainComputerDiscoveryTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseTargets_EmptyOutput_ReturnsEmptyList(string? output)
    {
        Assert.Empty(DomainComputerDiscoveryService.ParseTargets(output));
    }

    [Fact]
    public void ParseTargets_SingleObject_UsesComputerNameWhenDnsHostNameIsMissing()
    {
        const string json = """
        {
          "Name": " PC-001 ",
          "DNSHostName": null,
          "Description": "  Werkstatt-PC  "
        }
        """;

        var target = Assert.Single(DomainComputerDiscoveryService.ParseTargets(json));

        Assert.Equal("PC-001", target.Name);
        Assert.Equal("PC-001", target.Host);
        Assert.Equal("Werkstatt-PC", target.Description);
    }

    [Fact]
    public void ParseTargets_Array_PrefersDnsHostNameAndRemovesDuplicateHosts()
    {
        const string json = """
        [
          {
            "Name": "PC-002",
            "DNSHostName": " PC-002.example.local ",
            "Description": "Office"
          },
          {
            "Name": "PC-002-duplicate",
            "DNSHostName": "pc-002.example.local",
            "Description": "Duplicate"
          },
          {
            "Name": "PC-001",
            "DNSHostName": "PC-001.example.local",
            "Description": null
          }
        ]
        """;

        var targets = DomainComputerDiscoveryService.ParseTargets(json);

        Assert.Equal(2, targets.Count);
        Assert.Equal("PC-001", targets[0].Name);
        Assert.Equal("PC-001.example.local", targets[0].Host);
        Assert.Null(targets[0].Description);
        Assert.Equal("PC-002", targets[1].Name);
        Assert.Equal("PC-002.example.local", targets[1].Host);
        Assert.Equal("Office", targets[1].Description);
    }

    [Fact]
    public void ParseTargets_SkipsEntriesWithoutAnyUsableHost()
    {
        const string json = """
        [
          { "Name": "", "DNSHostName": "", "Description": "invalid" },
          { "Name": "PC-003", "DNSHostName": "", "Description": "valid" }
        ]
        """;

        var target = Assert.Single(DomainComputerDiscoveryService.ParseTargets(json));

        Assert.Equal("PC-003", target.Host);
    }
}
