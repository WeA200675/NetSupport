using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Services;

public sealed record RdpDisconnectMessage(string Summary, string Details);

/// <summary>
/// Converts documented ExtendedDisconnectReasonCode values into concise German operator guidance.
/// </summary>
public static class RdpDisconnectMessageFormatter
{
    public static RdpDisconnectMessage Format(RdpDisconnectedEventArgs e)
    {
        var friendly = e.ExtendedReason switch
        {
            0 => "Keine zusätzlichen Trennungsinformationen verfügbar.",
            1 => "Die Verbindung wurde durch eine Anwendung getrennt.",
            2 => "Die Sitzung wurde durch eine Anwendung abgemeldet.",
            3 => "Der Server hat die Sitzung wegen Inaktivität getrennt.",
            4 => "Das Zeitlimit für die Anmeldung wurde überschritten.",
            5 => "Die Sitzung wurde durch eine andere Verbindung ersetzt.",
            6 => "Auf dem Server war nicht genügend Arbeitsspeicher verfügbar.",
            7 => "Der Server hat die RDP-Verbindung abgelehnt.",
            8 => "Der Server hat die Verbindung aus Sicherheits-/FIPS-Gründen abgelehnt.",
            9 => "Der Server hat die Verbindung wegen unzureichender Berechtigungen abgelehnt.",
            10 => "Der Server verlangt neue/frische Anmeldeinformationen.",
            11 => "Die Trennung wurde durch Benutzeraktivität ausgelöst.",
            12 => "Der Benutzer wurde abgemeldet.",
            256 => "Interner RDP-Lizenzierungsfehler.",
            257 => "Kein RDP-Lizenzserver verfügbar.",
            258 => "Keine gültige RDP-Lizenz verfügbar.",
            259 => "Ungültige Lizenzierungsnachricht empfangen.",
            260 => "Hardware-ID passt nicht zur RDP-Lizenz.",
            261 => "RDP-Client-Lizenzfehler.",
            262 => "Lizenzierungsprotokoll konnte wegen Netzwerk-/Protokollproblemen nicht beendet werden.",
            263 => "Der Client hat das Lizenzierungsprotokoll vorzeitig beendet.",
            264 => "Fehler bei der Verschlüsselung einer Lizenzierungsnachricht.",
            265 => "RDP-Lizenz konnte nicht aktualisiert oder erneuert werden.",
            266 => "Der Remote-PC ist nicht für weitere Remoteverbindungen lizenziert.",
            267 => "Zugriff beim Erstellen/Aktualisieren des lokalen RDP-Lizenzspeichers verweigert.",
            768 => "Die angegebenen Anmeldeinformationen wurden abgelehnt.",
            >= 4096 and <= 32767 => "Interner RDP-Protokollfehler. Server-Ereignisprotokoll prüfen.",
            _ => $"Unbekannter erweiterter RDP-Trennungsgrund ({e.ExtendedReason})."
        };

        var nativeDescription = string.IsNullOrWhiteSpace(e.Description)
            ? null
            : e.Description.Trim();

        var details = $"Client-Grund: {e.Reason}; Extended-Grund: {e.ExtendedReason}";
        if (nativeDescription is not null)
            details += $"; Windows: {nativeDescription}";

        return new RdpDisconnectMessage(friendly, details);
    }
}
