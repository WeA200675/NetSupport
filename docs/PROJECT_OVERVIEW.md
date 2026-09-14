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
- wiederkehrende Listenfilter als benannte Ansichten speichern
- Standardverbindung pro Rechner festlegen
- zuletzt gestartete Remote-Aktionen lokal nachvollziehen
- klare Trennung zwischen UI, Discovery, Rechnerdetails, Verlauf und Remote-Backends
- keine Speicherung von Passwörtern

---

## Aktueller Bedienablauf

1. Anwendung starten oder aus dem Tray öffnen.
2. Rechner direkt per Name/IP eingeben oder aus Active Directory laden.
3. Optional Online-/Offline-Status prüfen.
4. Liste über Text, Gruppe und/oder **Nur Favoriten** filtern oder eine gespeicherte Ansicht laden.
5. Rechner auswählen.
6. Optional **Rechnerdetails laden**.
7. Favorit, Gruppe und Standard-Provider setzen und mit **Speichern / Aktualisieren** übernehmen.
8. **Standardverbindung starten** oder den Rechner doppelklicken.
9. Direkte Schnellaktionen für NetSupport/RDP bleiben unabhängig davon verfügbar.
10. Unter **Zuletzt verwendet** können jüngste Verbindungsstarts eingesehen und Ziele erneut fokussiert werden.

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

## Gespeicherte Ansichten

Wiederkehrende Kombinationen aus Listenfiltern können benannt gespeichert werden.

Eine Ansicht enthält:

```json
{
  "name": "Server",
  "searchText": null,
  "group": "Server",
  "favoritesOnly": false
}
```

Gespeichert werden nur:

- Textsuche
- Gruppenfilter
- **Nur Favoriten**

Beim Auswählen einer Ansicht werden die Filter unmittelbar wieder angewendet. Speichern unter demselben Namen aktualisiert die bestehende Ansicht.

Die Ansichten liegen in `settings.json`, weil sie Teil der Bedienkonfiguration sind.

Details: [`SAVED_VIEWS_AND_HISTORY.md`](SAVED_VIEWS_AND_HISTORY.md).

---

## Lokaler Verbindungsverlauf

Jeder Start einer NetSupport-/RDP-Aktion wird über `ISessionHistoryService` protokolliert.

Aktuelle Implementierung:

```text
ISessionHistoryService
   +--> JsonSessionHistoryService
           +--> %AppData%\NetSupportRemoteAdmin\session-history.json
```

Gespeichert werden:

- Zeitpunkt
- Hostname und optional Anzeigename
- Provider
- Aktion
- Erfolg oder Fehler beim Starten
- Fehlertext, falls der Provider nicht gestartet werden konnte

Nicht gespeichert werden Passwörter, Credentials, Bildschirminhalte oder CIM-Inventardaten.

Der Verlauf ist auf 100 Einträge begrenzt und kann im UI vollständig gelöscht werden.

Wichtig: Bei externen Programmen wie NetSupport handelt es sich um einen **Startverlauf**, nicht um ein revisionssicheres Session-Audit.

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

Die dokumentierte ActiveX-Schnittstelle unterstützt Multi-Monitor als Ein/Aus-Modus über `UseMultimon`. Eine gezielte Auswahl einzelner Monitor-IDs wird deshalb nicht über eine undokumentierte COM-Eigenschaft implementiert. Die dokumentierte RDP-Eigenschaft `selectedmonitors` eignet sich für einen späteren externen-RDP-Ausbau.

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
- jüngster bekannter Verbindungsstart des ausgewählten Hosts

Die Remote-CIM-Abfrage verwendet die aktuelle Windows-Identität und die normale WSMan-Konfiguration. Ein UI-Zeitlimit verhindert langes Blockieren. Ist CIM/WSMan nicht verfügbar, bleiben DNS, Ping, NetSupport und RDP unabhängig nutzbar.

Diese Inventarinformationen werden bewusst nicht gespeichert, weil sie schnell veralten können.

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
   +--> ISessionHistoryService
   |       +--> JsonSessionHistoryService
   |               +--> session-history.json
   |
   +--> HostAvailabilityService
   |
   +--> ConfigService
           +--> settings.json
```

Erweiterungspunkte:

- `IRemoteProvider` – weitere Remote-Technologien
- `ITargetDiscoveryService` – weitere Rechnerquellen
- `ITargetDetailsService` – weitere Inventar-/Detailquellen
- `IRdpSessionLauncher` – alternatives RDP-Session-Hosting
- `ISessionHistoryService` – später andere Verlauf-/Audit-Backends

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
  ],
  "savedViews": [
    {
      "name": "Büro-Favoriten",
      "searchText": null,
      "group": "Büro",
      "favoritesOnly": true
    }
  ]
}
```

Separater Verlauf:

```text
%AppData%\NetSupportRemoteAdmin\session-history.json
```

Nicht gespeichert werden unter anderem:

- Passwörter
- RDP-Credentials
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

- gezielte Monitor-ID-Auswahl über einen dokumentierten externen RDP-Pfad
- weitere Tastatur-/Sondertasten-Werkzeuge
- detailliertere RDP-Fehlertexte
- weitere Redirects wie Laufwerke oder Audio
- optionaler Export des Startverlaufs
- weitere Discovery-, Details-, History- und Remote-Provider

---

## Entwicklungsprinzipien

- Bedienung zuerst
- wenig Klicks für häufige Aufgaben
- explizites Speichern dauerhafter Organisationsänderungen
- keine Speicherung von Passwörtern
- flüchtige Inventardaten nicht unnötig persistieren
- History getrennt von der Zielkonfiguration halten
- klare Trennung der Verantwortlichkeiten
- dokumentierte APIs vor undokumentierten COM-Tricks bevorzugen
- neue Funktionen so integrieren, dass Backends später austauschbar bleiben

---

## Entwicklungsbranch und Pull Request

```text
feature/extensible-remote-admin
PR #1 – Add extensible remote admin frontend
```
