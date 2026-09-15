namespace NetSupport.RemoteAdmin.Models;

public sealed class TargetConnectionDiagnosticResult
{
    public required string Host { get; init; }
    public int NetSupportPort { get; init; }
    public DateTimeOffset CheckedAt { get; init; }
    public IReadOnlyList<string> IpAddresses { get; init; } = Array.Empty<string>();
    public string? DnsError { get; init; }
    public bool PingReachable { get; init; }
    public bool NetSupportReachable { get; init; }
    public long DnsDurationMilliseconds { get; init; }
    public long PingDurationMilliseconds { get; init; }
    public long NetSupportDurationMilliseconds { get; init; }

    public bool DnsResolved => IpAddresses.Count > 0 && string.IsNullOrWhiteSpace(DnsError);

    public string DnsStatusText => DnsResolved
        ? string.Join(", ", IpAddresses)
        : string.IsNullOrWhiteSpace(DnsError)
            ? "Keine Adresse gefunden"
            : DnsError;

    public string PingStatusText => PingReachable
        ? "Ping erreichbar"
        : "Ping keine Antwort";

    public string NetSupportStatusText => NetSupportReachable
        ? $"TCP {NetSupportPort} erreichbar"
        : $"TCP {NetSupportPort} nicht erreichbar";

    public string SummaryText => NetSupportReachable
        ? "Der konfigurierte NetSupport-Client-Port ist erreichbar."
        : DnsResolved
            ? "DNS funktioniert; der konfigurierte NetSupport-Client-Port ist derzeit nicht erreichbar."
            : "DNS-Auflösung und NetSupport-Port konnten nicht bestätigt werden.";

    public string ToReportText() => string.Join(Environment.NewLine,
        $"Ziel: {Host}",
        $"Geprüft: {CheckedAt.ToLocalTime():dd.MM.yyyy HH:mm:ss}",
        $"DNS/IP: {DnsStatusText} ({DnsDurationMilliseconds} ms)",
        $"Ping: {PingStatusText} ({PingDurationMilliseconds} ms)",
        $"NetSupport: {NetSupportStatusText} ({NetSupportDurationMilliseconds} ms)",
        $"Zusammenfassung: {SummaryText}");
}
