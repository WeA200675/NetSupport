# NetSupport Remote Admin – Projektübersicht

> Diese Datei fasst den aktuellen funktionalen und technischen Stand des Projekts zusammen und wird bei weiteren Ausbauschritten mitgepflegt.

---

## Ziel

**NetSupport Remote Admin** ist eine kompakte Windows-Anwendung für die tägliche Administration einer größeren Anzahl von Domänenrechnern.

NetSupport Manager wird nicht ersetzt, sondern als Remote-Backend hinter einer einfacheren Oberfläche verwendet. Windows Remote Desktop ist ein zusätzlicher Provider. Discovery, Rechnerdetails, Verlauf, Betriebseinstellungen, Diagnose/Support und RDP-Spezialfunktionen sind über eigene Schnittstellen gekapselt.

Wichtige Prinzipien:

- wenige Klicks für häufige Aufgaben
- Tray-/Infobereich-Betrieb
- keine Abhängigkeit von unzuverlässig verteilten NetSupport-UI-Einstellungen
- keine Speicherung von Passwörtern
- explizites Speichern dauerhafter Zielpräferenzen
- flüchtige Inventardaten bleiben flüchtig
- dokumentierte Windows-/Microsoft-Schnittstellen vor undokumentierten Workarounds
- Diagnose darf die eigentliche Remoteverwaltung niemals blockieren
- Supportdaten werden gezielt erzeugt statt komplette Konfigurationsdateien blind zu archivieren

---

## Aktueller Bedienablauf

1. Anwendung starten oder aus dem Tray öffnen.
2. Rechner direkt per Name/IP eingeben oder aus Active Directory laden.
3. Liste über Text, Gruppe, Favoriten oder eine gespeicherte Ansicht filtern.
4. Ziel auswählen.
5. Optional Online-Status und Rechnerdetails laden.
6. Favorit, Gruppe, Standard-Provider und RDP-Zielpräferenzen mit **Speichern / Aktualisieren** sichern.
7. Direkte Schnellaktion oder **Standardverbindung starten** verwenden.
8. Unter **Zuletzt verwendet** jüngste Startversuche einsehen oder als CSV exportieren.
9. Unter **Erweitert → Einstellungen** Start-, RDP-, Autostart-, Diagnose- und NetSupport-Optionen ändern.
10. Bei Bedarf ein anonymisierbares Supportpaket als ZIP erzeugen.

---

## Zielorganisation

Persistierbare Zielattribute umfassen unter anderem:

```json
{
  "isFavorite": true,
  "group": "Büro",
  "preferredProviderId": "netsupport"
}
```

Funktionen:

- Favoriten zuerst sortieren
- **Nur Favoriten**
- freie Gruppen wie `Büro`, `Werkstatt`, `Server`
- Gruppenfilter
- Gruppen werden von der Textsuche berücksichtigt
- bevorzugter Control-Provider pro Ziel
- Doppelklick nutzt den bevorzugten Provider
- Fallback auf einen verfügbaren Control-Provider

Details: [`TARGET_ORGANIZATION.md`](TARGET_ORGANIZATION.md).

---

## Gespeicherte Ansichten

Benannte Ansichten speichern:

- Textsuche
- Gruppenfilter
- Favoritenfilter

Beispiel:

```json
{
  "name": "Server",
  "searchText": null,
  "group": "Server",
  "favoritesOnly": false
}
```

Die Ansichten liegen in `settings.json` und werden beim Auswählen sofort angewendet.

---

## Lokaler Startverlauf und CSV-Export

`ISessionHistoryService` protokolliert die letzten Remote-Aktionsstarts separat in:

```text
%AppData%\NetSupportRemoteAdmin\session-history.json
```

Gespeichert werden nur:

- Zeit
- Ziel
- Provider
- Aktion
- Start erfolgreich/fehlgeschlagen
- Fehlertext bei fehlgeschlagenem Start

Der Verlauf ist auf 100 Einträge begrenzt, kann gelöscht werden und enthält keine Passwörter, Bildschirminhalte oder CIM-Inventardaten.

Über **CSV exportieren** kann der vollständige lokale Verlauf semikolongetrennt und als UTF-8 mit BOM exportiert werden. Das Format ist bewusst auf zuverlässiges Öffnen in deutschsprachigen Excel-Installationen ausgelegt.

Details: [`SAVED_VIEWS_AND_HISTORY.md`](SAVED_VIEWS_AND_HISTORY.md) und [`OPERATIONS.md`](OPERATIONS.md).

---

## Einstellungen, Autostart und Diagnose

Unter **Erweitert → Einstellungen** können ohne manuelle JSON-Bearbeitung geändert werden:

- Mit Windows starten
- beim Start minimiert öffnen
- Diagnoseprotokoll aktivieren/deaktivieren
- eingebettetes RDP bevorzugen
- externes RDP im Vollbild starten
- Pfad zu `PCICTLUI.EXE`

### Windows-Autostart

Autostart wird ausschließlich im Profil des aktuellen Benutzers verwaltet:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

Wert:

```text
NetSupportRemoteAdmin
```

Dadurch werden keine computerweiten Einstellungen oder Gruppenrichtlinien verändert.

### Diagnoseprotokoll

Optionales Log:

```text
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

Das Log wird bei ungefähr 2 MB nach

```text
application.log.1
```

rotiert.

Erfasst werden unter anderem Programmstart/-ende, Remote-Aktionsstarts, Provider/Aktion/Zielhost, AD-Ladevorgänge, Statusprüfungen und Fehler. Nicht protokolliert werden Passwörter, RDP-Credentials, Bildschirm-/Zwischenablageinhalte oder Remote-Dateiinhalte.

Details: [`OPERATIONS.md`](OPERATIONS.md).

---

## Supportpaket

`ISupportBundleService` erzeugt auf Wunsch ein lokales ZIP für Fehlersuche.

Aktuelle Implementierung:

```text
ISupportBundleService
   +--> SupportBundleService
           +--> AppConfig (bereinigte Zusammenfassung)
           +--> ISessionHistoryService
           +--> IDiagnosticLogService
           +--> System-/Runtime-Informationen
           +--> ZIP-Erzeugung
```

Standardmäßig ist die Anonymisierung aktiviert.

Das ZIP enthält:

```text
README.txt
system-info.json
configuration-summary.json
recent-history.json
recent-errors.txt
logs/
```

Die originale `settings.json` wird nicht kopiert. RDP-Passwörter/Credentials, Sitzungsinhalte, Zwischenablageinhalte und Remote-Dateiinhalte werden nicht aufgenommen.

Bei aktiver Anonymisierung werden bekannte Host-/Rechner-/Benutzer-/Domainwerte in Verlauf und Logs ersetzt. Ziele erscheinen z. B. als `target-001`. Gruppen und Ansichten werden in der Konfigurationsübersicht abstrahiert. RDP-Benutzername/Domain werden dort nur als `konfiguriert: ja/nein` dargestellt.

Details: [`SUPPORT_BUNDLE.md`](SUPPORT_BUNDLE.md).

---

## Active Directory und Status

Discovery:

```text
ITargetDiscoveryService
   +--> DomainComputerDiscoveryService
           +--> Get-ADComputer
```

Voraussetzung: RSAT / ActiveDirectory-PowerShell-Modul.

`HostAvailabilityService` prüft Rechner parallel per Ping. Status und letzter Prüfzeitpunkt werden nicht dauerhaft gespeichert.

---

## Rechnerdetails

Details werden über `ITargetDetailsService` geladen.

Aktuelle Implementierung:

```text
PowerShellTargetDetailsService
   +--> DNS
   +--> Get-CimInstance Win32_ComputerSystem
   +--> Get-CimInstance Win32_OperatingSystem
```

Angezeigt werden:

- IP-Adresse(n)
- angemeldeter Windows-Benutzer
- Windows-Edition/-Version
- Hersteller/Modell
- letzter Prüfzeitpunkt
- jüngster bekannter Remote-Start des Hosts

CIM/WSMan-Fehler blockieren die übrigen Remote-Funktionen nicht.

---

## NetSupport Manager

Der NetSupport-Provider startet `PCICTLUI.EXE`.

Aktuelle Schnellaktionen:

| Aktion | Backend |
|---|---|
| Steuern | NetSupport Control |
| Nur ansehen | NetSupport View |
| Chat | NetSupport Chat |
| Inventar | NetSupport Inventory |
| Remote CMD | NetSupport Remote Command Prompt |
| Dateien | NetSupport File Transfer |

Der NetSupport-Pfad kann in der Einstellungsseite geändert werden; die Liste der verfügbaren Provider wird anschließend ohne Neustart aktualisiert.

---

## Windows Remote Desktop

### Eingebetteter Viewer

Der normale RDP-Weg hostet Microsofts `MsRdpClient12NotSafeForScripting` in einem eigenen Fenster.

Aktuelle Funktionen:

- Connect / Reconnect / Disconnect
- Vollbild
- Connecting-/Connected-/Login-/Disconnect-Status
- Extended Disconnect Reason
- Fatal-Error-Anzeige
- Remote-Auflösung
- Auto-Reconnect-Status
- SmartSizing
- Zwischenablage
- Admin-Sitzung
- Benutzername/Domäne
- keine Passwortspeicherung
- Remote Alt+Tab, Start und Task-Manager
- Multi-Monitor über `UseMultimon`

### Gezielte Monitorwahl

Für einzelne lokale RDP-Monitore kann pro Ziel gespeichert werden:

```json
"rdpSelectedMonitors": "0,1"
```

Die lokalen IDs werden mit

```text
mstsc.exe /l
```

angezeigt. Im UI steht dafür **IDs anzeigen** bereit.

Ist `rdpSelectedMonitors` gesetzt, erzeugt `IRdpConnectionFileService` eine minimale `.rdp`-Datei mit:

```text
use multimon:i:1
selectedmonitors:s:0,1
```

und startet den Windows-RDP-Client damit. Ohne ID-Liste bleibt der normale eingebettete Viewer aktiv.

Die Dateien liegen unter:

```text
%AppData%\NetSupportRemoteAdmin\rdp\
```

Sie enthalten keine Passwörter oder Credentials.

Microsoft dokumentiert `SelectedMonitors` inzwischen zusätzlich als benannte Eigenschaft von `IMsRdpExtendedSettings`. Für die aktuelle Phase wird trotzdem der transparente `.rdp`-Pfad verwendet, weil die bestehende AxHost-Kapselung bewusst ohne generierte MSTSCLib-Interop-Assemblies arbeitet. Eine spätere typisierte Extended-Settings-Anbindung kann die Auswahl auch im eingebetteten Viewer ermöglichen.

Details: [`RDP_SELECTED_MONITORS.md`](RDP_SELECTED_MONITORS.md) und [`RDP_SESSION.md`](RDP_SESSION.md).

---

## Persistente Dateien

### Bedienkonfiguration

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

Enthält unter anderem:

- gespeicherte Ziele
- Favoriten/Gruppen
- bevorzugte Provider
- nicht geheime RDP-Präferenzen
- `rdpSelectedMonitors`
- gespeicherte Ansichten
- Start-/RDP-/Diagnoseoptionen

### Startverlauf

```text
%AppData%\NetSupportRemoteAdmin\session-history.json
```

### Diagnose

```text
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

### Generierte RDP-Verbindungsdateien

```text
%AppData%\NetSupportRemoteAdmin\rdp\
```

Support-ZIPs werden nicht automatisch im AppData-Verzeichnis abgelegt, sondern nur am vom Benutzer gewählten Zielpfad.

Nicht gespeichert werden RDP-Passwörter oder andere Credentials.

---

## Architektur

```text
MainWindow
   |
   +--> RemoteProviderRegistry
   |       +--> NetSupportProvider --> PCICTLUI.EXE
   |       +--> RdpProvider
   |               +--> IRdpSessionLauncher
   |               |       +--> EmbeddedRdpSessionLauncher
   |               |               +--> RdpSessionWindow
   |               |                       +--> RdpActiveXControl
   |               |
   |               +--> IRdpConnectionFileService
   |                       +--> RdpConnectionFileService
   |                               +--> .rdp + mstsc.exe
   |
   +--> ITargetDiscoveryService
   |       +--> DomainComputerDiscoveryService
   |
   +--> ITargetDetailsService
   |       +--> PowerShellTargetDetailsService
   |
   +--> ISessionHistoryService
   |       +--> JsonSessionHistoryService
   |
   +--> IAutoStartService
   |       +--> WindowsAutoStartService --> HKCU Run
   |
   +--> IDiagnosticLogService
   |       +--> DiagnosticLogService --> logs/application.log
   |
   +--> ISupportBundleService
   |       +--> SupportBundleService --> anonymisiertes ZIP
   |
   +--> HostAvailabilityService
   +--> ConfigService
```

Erweiterungspunkte:

```text
IRemoteProvider
ITargetDiscoveryService
ITargetDetailsService
ISessionHistoryService
IRdpSessionLauncher
IRdpConnectionFileService
IAutoStartService
IDiagnosticLogService
ISupportBundleService
```

---

## Build und Test

```powershell
dotnet restore NetSupport.sln
dotnet build NetSupport.sln --configuration Release
```

GitHub Actions führt zusätzlich einen self-contained Windows-x64-Publish aus und lädt das Artefakt

```text
NetSupport.RemoteAdmin-win-x64
```

hoch.

Praktische Prüfschritte: [`TESTING.md`](TESTING.md).

---

## Nächste sinnvolle Ausbaustufen

- optional `IMsRdpExtendedSettings.SelectedMonitors` typisiert für eingebettetes RDP anbinden
- weitere RDP-Redirects wie Laufwerke/Audio
- weitere Tastatur-/Sondertastenfunktionen
- detailliertere RDP-Fehlertexte
- Filter/Zeitraum für den History-Export
- weitere Discovery-, Details-, History-, Support- und Remote-Provider

---

## Entwicklungsbranch

```text
feature/extensible-remote-admin
PR #1 – Add extensible remote admin frontend
```
