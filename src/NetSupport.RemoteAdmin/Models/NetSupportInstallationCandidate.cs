namespace NetSupport.RemoteAdmin.Models;

public sealed class NetSupportInstallationCandidate
{
    public string Path { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public bool Exists { get; init; }
    public bool IsControlExecutable { get; init; }
    public string? ProductName { get; init; }
    public string? ProductVersion { get; init; }
    public string? FileVersion { get; init; }
    public string? CompanyName { get; init; }
    public DateTimeOffset? LastWriteTime { get; init; }

    public bool IsUsable => Exists && IsControlExecutable;

    public string StatusText => IsUsable
        ? "Gültig"
        : Exists
            ? "Falsche EXE"
            : "Nicht gefunden";

    public string VersionText => !string.IsNullOrWhiteSpace(ProductVersion)
        ? ProductVersion!
        : !string.IsNullOrWhiteSpace(FileVersion)
            ? FileVersion!
            : "–";
}
