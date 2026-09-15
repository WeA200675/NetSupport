using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Providers;
using NetSupport.RemoteAdmin.Services;
using Xunit;

namespace NetSupport.RemoteAdmin.Tests;

public sealed class NetSupportCommandLineTests
{
    [Theory]
    [InlineData("PC-001", "/c PC-001")]
    [InlineData("PC-001.example.local", "/c PC-001.example.local")]
    [InlineData("10.20.30.40", "/c\">10.20.30.40\"")]
    public void BuildConnectArgument_AcceptsExpectedTargets(string host, string expected)
    {
        Assert.Equal(expected, NetSupportCommandLine.BuildConnectArgument(host));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-bad")]
    [InlineData("PC 001")]
    [InlineData("PC-001\" /a")]
    [InlineData("PC-001\r\n/a")]
    [InlineData("PC-001&calc")]
    public void BuildConnectArgument_RejectsUnsafeTargets(string host)
    {
        Assert.Throws<ArgumentException>(() => NetSupportCommandLine.BuildConnectArgument(host));
    }

    [Theory]
    [InlineData(RemoteAction.Control, "/vc /e")]
    [InlineData(RemoteAction.View, "/v /e")]
    [InlineData(RemoteAction.Chat, "/a /ea")]
    [InlineData(RemoteAction.Inventory, "/i /ei")]
    [InlineData(RemoteAction.CommandPrompt, "/m /em")]
    [InlineData(RemoteAction.FileTransfer, "/x /ex")]
    public void GetActionArguments_MapsAllSupportedActions(RemoteAction action, string expected)
    {
        Assert.Equal(expected, NetSupportCommandLine.GetActionArguments(action));
    }

    [Fact]
    public void BuildProfileArguments_NoProfileWithoutLock_IsEmpty()
    {
        Assert.Equal(string.Empty, NetSupportCommandLine.BuildProfileArguments(null, lockProfile: false));
    }

    [Fact]
    public void BuildProfileArguments_LockWithoutProfile_IsRejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            NetSupportCommandLine.BuildProfileArguments(null, lockProfile: true));
    }

    [Theory]
    [InlineData("Helpdesk", false, "/n \"Helpdesk\"")]
    [InlineData("Helpdesk Standard", false, "/n \"Helpdesk Standard\"")]
    [InlineData("Helpdesk Standard", true, "/f /n \"Helpdesk Standard\"")]
    public void BuildProfileArguments_ProducesExpectedSyntax(
        string profileName,
        bool lockProfile,
        string expected)
    {
        Assert.Equal(expected, NetSupportCommandLine.BuildProfileArguments(profileName, lockProfile));
    }

    [Fact]
    public void BuildArguments_ProducesExactCombinedCommandLine()
    {
        var actual = NetSupportCommandLine.BuildArguments(
            "10.20.30.40",
            RemoteAction.Control,
            "Helpdesk Standard",
            lockProfile: true);

        Assert.Equal(
            "/f /n \"Helpdesk Standard\" /c\">10.20.30.40\" /vc /e",
            actual);
    }

    [Theory]
    [InlineData("Help\"desk")]
    [InlineData("Help\nDesk")]
    [InlineData("Help\\Desk")]
    public void ProfileNameValidation_RejectsCommandLineBreakingCharacters(string profileName)
    {
        Assert.False(NetSupportProfileService.IsSafeProfileName(profileName));
        Assert.Throws<ArgumentException>(() => NetSupportProfileService.NormalizeProfileName(profileName));
    }

    [Fact]
    public void ProfileNameValidation_RejectsNamesLongerThan128Characters()
    {
        var profileName = new string('A', 129);

        Assert.False(NetSupportProfileService.IsSafeProfileName(profileName));
        Assert.Throws<ArgumentException>(() => NetSupportProfileService.NormalizeProfileName(profileName));
    }

    [Fact]
    public void ProfileNameValidation_TrimsSafeName()
    {
        Assert.Equal("Helpdesk Standard", NetSupportProfileService.NormalizeProfileName("  Helpdesk Standard  "));
    }

    [Fact]
    public void ValidateExecutablePath_AcceptsOnlyExistingPcictluiExe()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var validPath = Path.Combine(directory, "PCICTLUI.EXE");
            File.WriteAllBytes(validPath, Array.Empty<byte>());

            Assert.Equal(Path.GetFullPath(validPath), NetSupportCommandLine.ValidateExecutablePath(validPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ValidateExecutablePath_RejectsForeignExecutableEvenWhenItExists()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var invalidPath = Path.Combine(directory, "notepad.exe");
            File.WriteAllBytes(invalidPath, Array.Empty<byte>());

            Assert.Throws<InvalidOperationException>(() =>
                NetSupportCommandLine.ValidateExecutablePath(invalidPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ValidateExecutablePath_RejectsMissingPcictluiExe()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "PCICTLUI.EXE");

        Assert.Throws<InvalidOperationException>(() =>
            NetSupportCommandLine.ValidateExecutablePath(path));
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "NetSupport.RemoteAdmin.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
