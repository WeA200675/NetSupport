namespace NetSupport.RemoteAdmin.Models;

public enum SystemHealthLevel
{
    Healthy,
    Info,
    Warning,
    Error
}

public sealed class SystemHealthCheckResult
{
    public string Name { get; init; } = string.Empty;
    public SystemHealthLevel Level { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string? Details { get; init; }

    public string StatusIcon => Level switch
    {
        SystemHealthLevel.Healthy => "✓",
        SystemHealthLevel.Info => "•",
        SystemHealthLevel.Warning => "!",
        SystemHealthLevel.Error => "×",
        _ => "?"
    };

    public string StatusText => Level switch
    {
        SystemHealthLevel.Healthy => "OK",
        SystemHealthLevel.Info => "Info",
        SystemHealthLevel.Warning => "Hinweis",
        SystemHealthLevel.Error => "Fehler",
        _ => "Unbekannt"
    };
}
