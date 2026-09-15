using System.Text.Json.Serialization;

namespace NetSupport.RemoteAdmin.Models;

/// <summary>
/// Lightweight local audit trail of remote-session launch attempts.
/// It intentionally stores no credentials or runtime machine inventory.
/// </summary>
public sealed class SessionHistoryEntry
{
    public DateTimeOffset StartedAt { get; set; }
    public string Host { get; set; } = string.Empty;
    public string? TargetName { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public string? Error { get; set; }

    [JsonIgnore]
    public string StatusMarker => Succeeded ? "✓" : "!";

    [JsonIgnore]
    public string TimeText => StartedAt.ToLocalTime().ToString("dd.MM. HH:mm");

    [JsonIgnore]
    public string TargetText => string.IsNullOrWhiteSpace(TargetName)
        ? Host
        : $"{TargetName} · {Host}";

    [JsonIgnore]
    public string ProviderActionText => $"{ProviderName} · {Action}";
}
