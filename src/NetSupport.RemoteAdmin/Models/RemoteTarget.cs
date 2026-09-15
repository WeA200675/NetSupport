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
    /// In the current domain build this is normalized to "netsupport".
    /// </summary>
    public string? PreferredProviderId { get; set; }

    /// <summary>
    /// Preferred NetSupport action for the target's standard/double-click connection.
    /// Stored as a readable action name (for example Control, View or CommandPrompt).
    /// A missing or invalid value falls back to Control.
    /// </summary>
    public string? PreferredAction { get; set; }

    [JsonIgnore]
    public HostStatus Status { get; set; } = HostStatus.Unknown;

    [JsonIgnore]
    public NetSupportReachabilityStatus NetSupportStatus { get; set; } = NetSupportReachabilityStatus.Unknown;

    [JsonIgnore]
    public DateTimeOffset? LastStatusCheck { get; set; }

    [JsonIgnore]
    public RemoteTargetDetails? Details { get; set; }

    [JsonIgnore]
    public string StatusText => Status switch
    {
        HostStatus.Online => "Online",
        // A failed ping does not prove that the computer is powered off; ICMP can be blocked.
        HostStatus.Offline => "Nicht erreichbar",
        _ => "Unbekannt"
    };

    [JsonIgnore]
    public string NetSupportStatusText => NetSupportStatus switch
    {
        NetSupportReachabilityStatus.Reachable => "NetSupport erreichbar",
        NetSupportReachabilityStatus.Unreachable => "NetSupport nicht erreichbar",
        _ => "NetSupport nicht geprüft"
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

public enum NetSupportReachabilityStatus
{
    Unknown,
    Reachable,
    Unreachable
}
