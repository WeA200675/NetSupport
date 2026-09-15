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
