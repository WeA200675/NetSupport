using System.Text.Json.Serialization;

namespace NetSupport.RemoteAdmin.Models;

public sealed class RemoteTarget
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Marks a persisted target as a favorite for quick filtering and sorting.
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// Optional user-defined group such as "Büro", "Werkstatt" or "Server".
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Provider id used for the target's standard/double-click connection.
    /// Unknown or unavailable providers fall back to an available Control provider.
    /// </summary>
    public string? PreferredProviderId { get; set; }

    /// <summary>
    /// Optional user name used to prefill embedded RDP sessions.
    /// Passwords are intentionally never stored in the target configuration.
    /// </summary>
    public string? RdpUserName { get; set; }

    /// <summary>
    /// Optional Windows/AD domain used to prefill embedded RDP sessions.
    /// </summary>
    public string? RdpDomain { get; set; }

    /// <summary>
    /// Enables clipboard redirection for RDP sessions.
    /// </summary>
    public bool RdpRedirectClipboard { get; set; } = true;

    /// <summary>
    /// Enables local drive redirection for RDP sessions. Disabled by default.
    /// </summary>
    public bool RdpRedirectDrives { get; set; }

    /// <summary>
    /// Enables redirection of the default local microphone into the RDP session.
    /// Disabled by default.
    /// </summary>
    public bool RdpRedirectMicrophone { get; set; }

    /// <summary>
    /// RDP audio output mode: 0 = play locally, 1 = play remotely, 2 = do not play.
    /// </summary>
    public int RdpAudioRedirectionMode { get; set; }

    /// <summary>
    /// Requests the administrative RDP session for the target when supported.
    /// </summary>
    public bool RdpAdminSession { get; set; }

    /// <summary>
    /// Requests a multi-monitor RDP session. The setting is applied on the next connection.
    /// </summary>
    public bool RdpUseMultiMonitor { get; set; }

    /// <summary>
    /// Optional comma-separated local monitor IDs for a targeted RDP multi-monitor session.
    /// IDs are obtained from mstsc.exe /l. When set, the provider uses a generated .rdp file
    /// through the external Windows client so the documented selectedmonitors setting is used.
    /// </summary>
    public string? RdpSelectedMonitors { get; set; }

    [JsonIgnore]
    public HostStatus Status { get; set; } = HostStatus.Unknown;

    [JsonIgnore]
    public DateTimeOffset? LastStatusCheck { get; set; }

    [JsonIgnore]
    public RemoteTargetDetails? Details { get; set; }

    [JsonIgnore]
    public string StatusText => Status switch
    {
        HostStatus.Online => "Online",
        HostStatus.Offline => "Offline",
        _ => "Unbekannt"
    };

    [JsonIgnore]
    public string FavoriteMarker => IsFavorite ? "★" : string.Empty;

    public override string ToString() => string.IsNullOrWhiteSpace(Name) ? Host : $"{Name} ({Host})";
}

public enum HostStatus
{
    Unknown,
    Online,
    Offline
}
