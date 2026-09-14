# NetSupport Remote Admin – Projektübersicht

> Diese Datei fasst den aktuellen funktionalen und technischen Stand des Projekts zusammen und wird bei weiteren Ausbauschritten mitgepflegt.

---

## Ziel des Projekts

NetSupport Remote Admin ist eine kompakte Windows-Anwendung für die tägliche Administration einer größeren Anzahl von Domänenrechnern.

Das Tool ersetzt NetSupport Manager nicht, sondern stellt eine eigene, einfachere Bedienoberfläche vor vorhandene Remote-Techniken. NetSupport und Windows RDP sind austauschbare Backends. Active Directory dient als Rechnerquelle; flüchtige Rechnerinformationen werden bei Bedarf abgefragt.

Wichtige Ziele:

- wenige Klicks für häufige Aktionen
- dauerhaft im Windows-Infobereich nutzbar
- keine Abhängigkeit von unzuverlässig verteilten NetSupport-UI-Einstellungen
- Rechner mit Favoriten und Gruppen organisieren
- Standardverbindung pro Rechner festlegen
- klare Trennung zwischen UI, Discovery, Rechnerdetails und Remote-Backends
- keine Speicherung von Passwörtern

---

## Aktueller Bedienablauf

1. Anwendung starten oder aus dem Tray öffnen.
2. Rechner direkt per Name/IP eingeben oder aus Active Directory laden.
3. Optional Online-/Offline-Status prüfen.
4. Liste über Text, Gruppe und/oder **Nur Favoriten** filtern.
5. Rechner auswählen.
6. Optional **Rechnerdetails laden**.
7. Favorit, Gruppe und Standard-Provider setzen und mit **Speichern / Aktualisieren** übernehmen.
8. **Standardverbindung starten** oder den Rechner doppelklicken.
9. Direkte Schnellaktionen für NetSupport/RDP bleiben unabhängig davon verfügbar.

---

## Zielrechner organisieren

Gespeicherte Ziele besitzen zusätzlich zu Name, Host und Beschreibung folgende Organisationsfelder:

```json
{
  "isFavorite": true,
  "group": "Büro",
  "preferredProviderId": "netsupport"
}
```

### Favoriten

- Favoriten werden vor normalen Rechnern sortiert.
- In der Liste erscheint ein `★`.
- **Nur Favoriten** reduziert die Liste entsprechend.

### Gruppen

Gruppen sind freie Textwerte wie `Büro`, `Werkstatt`, `Server` oder `Testgeräte`.

- Textsuche berücksichtigt Gruppen.
- Ein eigener Gruppenfilter steht oberhalb der Liste bereit.
- Neue Gruppen erscheinen nach dem Speichern automatisch im Filter.

### Standard-Provider

`preferredProviderId` bestimmt die normale Control-Verbindung eines Ziels.

- **Standardverbindung starten** verwendet diesen Provider.
- Doppelklick verwendet den gespeicherten Standard-Provider.
- Direkte Buttons **Steuern** und **RDP** bleiben weiterhin verfügbar.
- Ist der gespeicherte Provider nicht verfügbar, wird zunächst NetSupport und danach ein anderer verfügbarer Control-Provider verwendet.

Die vollständige Beschreibung liegt in [`TARGET_ORGANIZATION.md`](TARGET_ORGANIZATION.md).

---

## NetSupport Manager

Der NetSupport-Provider startet `PCICTLUI.EXE` und unterstützt:

| Aktion | Funktion |
|---|---|
| Steuern | Bildschirm, Maus und Tastatur übernehmen |
| Nur ansehen | Remote-Bildschirm ohne Steuerung |
| Chat | NetSupport Chat |
| Inventar | NetSupport Inventaransicht |
| Remote CMD | Remote Command Prompt |
| Dateien | File Transfer |

Der Pfad wird in den üblichen `Program Files`-Verzeichnissen automatisch gesucht und kann über die Konfiguration überschrieben werden.

---

## Windows Remote Desktop

RDP besitzt zwei Betriebsarten:

**Eingebettet:** Microsoft Remote Desktop ActiveX Control in einem eigenen Session-Fenster.

**Fallback:** `mstsc.exe`, falls eingebettetes RDP deaktiviert oder nicht verfügbar ist.

Aktuelle Funktionen:

- Verbinden / Neu verbinden / Trennen
- Vollbild / Fenstermodus
- Connecting-/Connected-/Login-/Disconnect-Status
- Extended Disconnect Reason und Fatal-Error-Anzeige
- Anzeige der Remote-Auflösung
- Auto-Reconnect-Status mit Versuchszähler und Netzstatus
- SmartSizing
- Multi-Monitor über `UseMultimon`
- Benutzername und Domäne
- normaler Windows-Credential-Prompt
- keine Passwortspeicherung
- Zwischenablageumleitung
- administrative RDP-Sitzung
- Remote-Aktionen für Alt+Tab, Start und Task-Manager
- persistente nicht geheime RDP-Präferenzen

Beim externen Client werden Multi-Monitor und Admin-Sitzung über `/multimon` bzw. `/admin` weitergegeben.

Details: [`RDP_SESSION.md`](RDP_SESSION.md).

---

## Active Directory

Die Rechnerliste kann über `ITargetDiscoveryService` aus Active Directory geladen werden.

Aktuelle Implementierung:

```text
DomainComputerDiscoveryService -> Get-ADComputer
```

Dafür wird RSAT / das ActiveDirectory-PowerShell-Modul benötigt.

AD-gefundene Rechner bleiben zunächst flüchtig und werden erst mit **Speichern / Aktualisieren** dauerhaft in die lokale Bedienkonfiguration aufgenommen.

---

## Online-/Offline-Status

`HostAvailabilityService` prüft Rechner parallel per Ping mit begrenzter Parallelität.

Der Status und der Zeitpunkt der letzten Prüfung sind Laufzeitinformationen und werden nicht in `settings.json` gespeichert.

---

## Rechnerdetails

Rechnerdetails sind hinter `ITargetDetailsService` gekapselt.

Aktuelle Implementierung:

```text
PowerShellTargetDetailsService
   +--> lokale DNS-Auflösung
   +--> Get-CimInstance Win32_ComputerSystem
   +--> Get-CimInstance Win32_OperatingSystem
```

Angezeigt werden:

- IP-Adresse(n)
- aktuell gemeldeter Windows-Benutzer
- Windows-Edition und Version
- Hersteller und Modell
- letzter Status-/Detailprüfzeitpunkt

Die Remote-CIM-Abfrage verwendet die aktuelle Windows-Identität und die normale WSMan-Konfiguration. Ein UI-Zeitlimit verhindert langes Blockieren. Ist CIM/WSMan nicht verfügbar, bleiben DNS, Ping, NetSupport und RDP unabhängig nutzbar.

Diese Informationen werden bewusst nicht gespeichert, weil sie schnell veralten können.

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
   |               |                               +--> MsTscAx.dll
   |               +--> mstsc.exe fallback
   |
   +--> ITargetDiscoveryService
   |       +--> DomainComputerDiscoveryService
   |
   +--> ITargetDetailsService
   |       +--> PowerShellTargetDetailsService
   |
   +--> HostAvailabilityService
   |
   +--> ConfigService
           +--> %AppData%\NetSupportRemoteAdmin\settings.json
```

Erweiterungspunkte:

- `IRemoteProvider` – weitere Remote-Technologien
- `ITargetDiscoveryService` – weitere Rechnerquellen
- `ITargetDetailsService` – weitere Inventar-/Detailquellen
- `IRdpSessionLauncher` – alternatives RDP-Session-Hosting

---

## Konfiguration

Pfad:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

Beispiel:

```json
{
  "netSupportExecutable": "C:\\Program Files (x86)\\NetSupport\\NetSupport Manager\\PCICTLUI.EXE",
  "startMinimized": false,
  "useEmbeddedRdp": true,
  "useFullScreenRdp": false,
  "targets": [
    {
      "name": "PC-001",
      "host": "PC-001",
      "description": "Büro-PC",
      "isFavorite": true,
      "group": "Büro",
      "preferredProviderId": "netsupport",
      "rdpUserName": "max.mustermann",
      "rdpDomain": "CONTOSO",
      "rdpRedirectClipboard": true,
      "rdpAdminSession": false,
      "rdpUseMultiMonitor": false
    }
  ]
}
```

Nicht gespeichert werden unter anderem:

- Passwörter
- aktueller Online-/Offline-Status
- aktuelle IP-Adressen
- aktuell angemeldeter Benutzer
- Laufzeit-Windowsinformationen

---

## Build und Test

Lokaler Build:

```powershell
dotnet restore NetSupport.sln
dotnet build NetSupport.sln --configuration Release
```

GitHub Actions führt zusätzlich einen self-contained Windows-x64-Publish aus und lädt das Artefakt

```text
NetSupport.RemoteAdmin-win-x64
```

hoch.

Die praktische Testcheckliste liegt in [`TESTING.md`](TESTING.md).

---

## Nächste sinnvolle Ausbaustufen

- Auswahl bestimmter Monitor-IDs statt nur aller Monitore
- weitere Tastatur-/Sondertasten-Werkzeuge
- detailliertere RDP-Fehlertexte
- weitere Redirects wie Laufwerke oder Audio
- Session-Historie / letzte Verbindung
- optional gespeicherte Ansichten/Filter
- weitere Discovery-, Details- und Remote-Provider

---

## Entwicklungsprinzipien

- Bedienung zuerst
- wenig Klicks für häufige Aufgaben
- explizites Speichern dauerhafter Organisationsänderungen
- keine Speicherung von Passwörtern
- flüchtige Inventardaten nicht unnötig persistieren
- klare Trennung der Verantwortlichkeiten
- neue Funktionen so integrieren, dass Backends später austauschbar bleiben

---

## Entwicklungsbranch und Pull Request

```text
feature/extensible-remote-admin
PR #1 – Add extensible remote admin frontend
```
