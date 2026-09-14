namespace NetSupport.RemoteAdmin.Models;

/// <summary>
/// Runtime-only information collected from a target computer.
/// These values are intentionally not persisted in settings.json because they can become stale.
/// </summary>
public sealed class RemoteTargetDetails
{
    public string? IpAddresses { get; init; }
    public string? OperatingSystem { get; init; }
    public string? OperatingSystemVersion { get; init; }
    public string? LoggedOnUser { get; init; }
    public string? Manufacturer { get; init; }
    public string? Model { get; init; }
    public DateTimeOffset CheckedAt { get; init; } = DateTimeOffset.Now;
    public string? ManagementError { get; init; }

    public string ComputerModel => string.Join(" ", new[] { Manufacturer, Model }
        .Where(value => !string.IsNullOrWhiteSpace(value))) is { Length: > 0 } value
            ? value
            : "–";

    public string OperatingSystemDisplay => string.Join(" · ", new[] { OperatingSystem, OperatingSystemVersion }
        .Where(value => !string.IsNullOrWhiteSpace(value))) is { Length: > 0 } value
            ? value
            : "–";
}
