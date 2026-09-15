using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Services;

public interface INetSupportInstallationService
{
    IReadOnlyList<NetSupportInstallationCandidate> Discover(string? configuredPath = null);
    NetSupportInstallationCandidate Inspect(string path, string source = "Manuell");
    string? FindBestExecutable(string? configuredPath = null);
}
