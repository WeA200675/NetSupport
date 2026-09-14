using System.Text.Json.Serialization;

namespace NetSupport.RemoteAdmin.Models;

public sealed class RemoteTarget
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Optional user name used to prefill embedded RDP sessions.
    /// Passwords are intentionally never stored in the target configuration.
    /// </summary>
    public string? RdpUserName { get; set; }

    /// <summary>
    /// Optional Windows/AD domain used to prefill embedded RDP sessions.
    /// </summary>
    public string? RdpDomain { get; set; }

    [JsonIgnore]
    public HostStatus Status { get; set; } = HostStatus.Unknown;

    [JsonIgnore]
    public string StatusText => Status switch
    {
        HostStatus.Online => "Online",
        HostStatus.Offline => "Offline",
        _ => "Unbekannt"
    };

    public override string ToString() => string.IsNullOrWhiteSpace(Name) ? Host : $"{Name} ({Host})";
}

public enum HostStatus
{
    Unknown,
    Online,
    Offline
}
