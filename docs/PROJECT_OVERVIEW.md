# NetSupport Remote Admin – Projektübersicht

> Diese Datei fasst den aktuellen funktionalen und technischen Stand des Projekts zusammen und wird bei weiteren Ausbauschritten mitgepflegt.

---

## Ziel

**NetSupport Remote Admin** ist eine kompakte Windows-Anwendung für die tägliche Administration einer größeren Anzahl von Domänenrechnern.

NetSupport Manager wird nicht ersetzt, sondern als Remote-Backend hinter einer einfacheren Oberfläche verwendet. Windows Remote Desktop ist ein zusätzlicher Provider. Discovery, Rechnerdetails, Verlauf und RDP-Spezialfunktionen sind über eigene Schnittstellen gekapselt.

Wichtige Prinzipien:

- wenige Klicks für häufige Aufgaben
- Tray-/Infobereich-Betrieb
- keine Abhängigkeit von unzuverlässig verteilten NetSupport-UI-Einstellungen
- keine Speicherung von Passwörtern
- explizites Speichern dauerhafter Zielpräferenzen
- flüchtige Inventardaten bleiben flüchtig
- dokumentierte Windows-/Microsoft-Schnittstellen vor undokumentierten Workarounds

---

## Aktueller Bedienablauf

1. Anwendung starten oder aus dem Tray öffnen.
2. Rechner direkt per Name/IP eingeben oder aus Active Directory laden.
3. Liste über Text, Gruppe, Favoriten oder eine gespeicherte Ansicht filtern.
4. Ziel auswählen.
5. Optional Online-Status und Rechnerdetails laden.
6. Favorit, Gruppe, Standard-Provider und RDP-Zielpräferenzen mit **Speichern / Aktualisieren** sichern.
7. Direkte Schnellaktion oder **Standardverbindung starten** verwenden.
8. Unter **Zuletzt verwendet** jüngste Startversuche einsehen.

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

## Lokaler Startverlauf

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

Details: [`SAVED_VIEWS_AND_HISTORY.md`](SAVED_VIEWS_AND_HISTORY.md).

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

### Startverlauf

```text
%AppData%\NetSupportRemoteAdmin\session-history.json
```

### Generierte RDP-Verbindungsdateien

```text
%AppData%\NetSupportRemoteAdmin\rdp\
```

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
- optionaler Export/Filter des Startverlaufs
- weitere Discovery-, Details-, History- und Remote-Provider

---

## Entwicklungsbranch

```text
feature/extensible-remote-admin
PR #1 – Add extensible remote admin frontend
```
