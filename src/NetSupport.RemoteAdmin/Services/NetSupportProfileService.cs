using Microsoft.Win32;

namespace NetSupport.RemoteAdmin.Services;

public sealed class NetSupportProfileService : INetSupportProfileService
{
    public const string ConfigListRegistryPath = @"Software\NetSupport Ltd\PCICTL\ConfigList";

    public IReadOnlyList<string> DiscoverProfiles()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(ConfigListRegistryPath, writable: false);
            if (key is null)
                return Array.Empty<string>();

            return key.GetSubKeyNames()
                .Where(IsSafeProfileName)
                .OrderBy(name => string.Equals(name, "Standard", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            // Profile discovery is best effort. A configured profile is validated again before launch.
            return Array.Empty<string>();
        }
    }

    public bool ProfileExists(string profileName)
    {
        var normalized = NormalizeProfileName(profileName);
        return DiscoverProfiles().Any(name =>
            string.Equals(name, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public static string NormalizeProfileName(string profileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileName);
        var normalized = profileName.Trim();
        if (!IsSafeProfileName(normalized))
        {
            throw new ArgumentException(
                "Der NetSupport-Profilname ist ungültig. Steuerzeichen, Anführungszeichen und Backslashes sind nicht erlaubt; maximal 128 Zeichen.",
                nameof(profileName));
        }

        return normalized;
    }

    public static bool IsSafeProfileName(string? profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            return false;

        var trimmed = profileName.Trim();
        if (trimmed.Length is < 1 or > 128 || trimmed.Contains('"') || trimmed.Contains('\\'))
            return false;

        return trimmed.All(character => !char.IsControl(character));
    }
}
