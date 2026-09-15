namespace NetSupport.RemoteAdmin.Services;

public interface INetSupportProfileService
{
    IReadOnlyList<string> DiscoverProfiles();
    bool ProfileExists(string profileName);
}
