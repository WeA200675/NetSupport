# RDP-Diagnose und Remote-Aktionen

> Diese Datei beschreibt die verbesserte Fehlerdarstellung und die dokumentierten Sonderaktionen des eingebetteten RDP-Viewers.

---

## Ziel

Bei einer unerwartet getrennten RDP-Sitzung soll nicht nur ein numerischer Fehlercode sichtbar sein. Die Anwendung zeigt deshalb:

- eine kurze verständliche Zusammenfassung
- den normalen RDP-Client-Trennungsgrund
- den `ExtendedDisconnectReasonCode`
- sofern verfügbar den von Windows gelieferten Fehlertext
- eine Schaltfläche **Details kopieren** für Support-/Ticketfälle

---

## Extended Disconnect Reasons

`RdpDisconnectMessageFormatter` übersetzt die dokumentierten Microsoft-Werte in verständliche Hinweise.

Beispiele:

| Extended Code | Bedeutung im UI |
|---:|---|
| `3` | Sitzung wegen Inaktivität getrennt |
| `4` | Anmeldezeitlimit überschritten |
| `5` | Sitzung durch andere Verbindung ersetzt |
| `7` | Server hat Verbindung abgelehnt |
| `9` | unzureichende Berechtigungen |
| `10` | neue/frische Anmeldeinformationen erforderlich |
| `256–267` | RDP-Lizenzierungsfehler |
| `768` | Anmeldeinformationen abgelehnt |
| `4096–32767` | interner RDP-Protokollfehler |

Bei internen Protokollfehlern ist zusätzlich das Ereignisprotokoll des Zielservers bzw. Ziel-PCs relevant.

Die Anwendung behält die originalen numerischen Codes in den kopierbaren Details, damit ein Supportfall weiterhin exakt mit Microsoft-/Windows-Dokumentation abgeglichen werden kann.

---

## Windows-Fehlertext

Das ActiveX-Control versucht weiterhin über

```text
GetErrorDescription(reason, extendedReason)
```

einen nativen Windows-Text zu erhalten.

Ist ein solcher Text vorhanden, wird er zusätzlich zur eigenen verständlichen Zusammenfassung angezeigt. Fällt diese Abfrage aus, bleiben die numerischen Codes und die eigene Klassifizierung erhalten.

---

## Details kopieren

Nach einer unerwarteten Trennung oder einem fatalen RDP-Control-Fehler wird im Session-Fenster **Details kopieren** eingeblendet.

Kopiert werden:

```text
RDP-Ziel: <host>
Status: <sichtbarer Sessionstatus>
Details: <Fehlercodes / Windows-Text>
```

Es werden dabei keine RDP-Passwörter oder gespeicherten Credentials ergänzt.

Vor Weitergabe eines kopierten Textes sollte ein interner Hostname bei Bedarf trotzdem anonymisiert werden.

---

## Dokumentierte Remote-Aktionen

Die Anwendung verwendet `IMsRdpClient8.SendRemoteAction` nur mit dokumentierten `RemoteSessionActionType`-Werten.

Aktuell:

| UI | Wert | Bedeutung |
|---|---:|---|
| Alt+Tab remote | `4` | Anwendung wechseln |
| Start remote | `3` | Startoberfläche öffnen |
| Action Center | `5` | entspricht Win+A |
| Task-Manager | `6` | Task-Manager, sofern vom Ziel/Client unterstützt |

Die Task-Manager-Aktion ist versionsabhängiger als die älteren Aktionen. Nicht unterstützte Aktionen werden nur als Statusmeldung behandelt und beenden die Sitzung nicht.

---

## Warum kein eigener Ctrl+Alt+Entf-Hack?

Microsoft stellt mit `IMsRdpClientNonScriptable::SendKeys` zwar eine Möglichkeit bereit, Scancode-Sequenzen atomar an die Remotesitzung zu schicken. Diese Methode liegt aber auf einer nicht-scriptbaren vtable-Schnittstelle.

Die aktuelle RDP-Kapselung verzichtet bewusst auf generierte MSTSCLib-/AxInterop-Assemblies und zusätzliche COM-vtable-Interopdefinitionen. Deshalb wird für diesen Stand kein undokumentierter/dynamischer Ersatz für `Ctrl+Alt+Entf` oder `Ctrl+Alt+End` eingebaut.

Wenn diese Funktion später benötigt wird, sollte sie über eine sauber typisierte `IMsRdpClientNonScriptable`-Anbindung erfolgen.

---

## Architektur

```text
RdpActiveXControl
   |
   +--> OnDisconnected
   |       +--> reason
   |       +--> ExtendedDisconnectReason
   |       +--> GetErrorDescription(...)
   |
RdpSessionWindow
   |
   +--> RdpDisconnectMessageFormatter
   |       +--> verständliche Zusammenfassung
   |       +--> technische Details
   |
   +--> Details kopieren
   +--> SendRemoteAction(...)
```

---

## Betriebsregel

Die verständliche Zusammenfassung ist eine Bedienhilfe und ersetzt nicht die originalen Windows-Codes. Für schwierige RDP-Fälle sollten daher im Supportpaket bzw. Ticket möglichst sowohl der sichtbare Text als auch die numerischen Reason-Codes enthalten bleiben.
