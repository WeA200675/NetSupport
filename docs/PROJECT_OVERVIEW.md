# NetSupport Remote Admin – Projektübersicht

> Diese Datei fasst den aktuellen Stand des Projekts verständlich zusammen und wird bei weiteren Ausbauschritten mitgepflegt.

---

## Ziel des Projekts

NetSupport Remote Admin ist eine kompakte Windows-Anwendung, die die tägliche Fernwartung deutlich einfacher machen soll als die klassische NetSupport-Oberfläche.

Die Anwendung dient als eigene Steuerzentrale und verwendet vorhandene Remote-Techniken wie NetSupport Manager und Windows Remote Desktop als austauschbare Backends. Sie soll dauerhaft im Hintergrund laufen können, schnell erreichbar sein und typische Aktionen mit möglichst wenigen Klicks ausführen.

---

## Aktueller Bedienablauf

1. Anwendung starten – sie kann dauerhaft im Windows-Infobereich weiterlaufen.
2. Rechner direkt per Name/IP eingeben oder Rechner aus der Domäne laden.
3. Optional den Online-/Offline-Status der Rechner prüfen.
4. Rechner in der Liste auswählen.
5. Rechts erscheint die Aktionskarte des ausgewählten Rechners.
6. Aktion direkt starten: **Steuern**, **Nur ansehen**, **RDP**, **CMD**, **Dateien**, **Inventar** oder **Chat**.
7. Häufig benötigte Rechner können dauerhaft gespeichert werden.

Ein Doppelklick auf einen Rechner startet direkt die NetSupport-Steuerung.

---

## Aktuelle Funktionen

### Oberfläche

- WPF-Anwendung auf Basis von .NET 8
- Tray-/Infobereich-Betrieb
- kompakte Rechnerübersicht
- Such-/Filterfeld
- Zielrechner-Karte mit Schnellaktionen
- erweiterte Provider-/Aktionsauswahl für Sonderfälle
- Rechnername oder IP-Adresse direkt verwendbar

### NetSupport Manager

Der Provider verwendet `PCICTLUI.EXE` und unterstützt aktuell:

| Aktion | Funktion |
|---|---|
| Steuern | Bildschirm sowie Maus/Tastatur übernehmen |
| Nur ansehen | Remote-Bildschirm ohne Steuerung anzeigen |
| Chat | NetSupport Chat öffnen |
| Inventar | NetSupport Inventaransicht öffnen |
| Remote CMD | Remote Command Prompt öffnen |
| Dateien | NetSupport File Transfer öffnen |

Der Pfad zu `PCICTLUI.EXE` wird automatisch in den üblichen `Program Files`-Verzeichnissen gesucht und kann bei Bedarf über die Konfiguration überschrieben werden.

### Windows Remote Desktop

RDP ist als zweiter Remote-Provider vorhanden und unterstützt zwei Betriebsarten:

**Eingebettet:** Das Microsoft Remote Desktop ActiveX Control wird in einem eigenen Session-Fenster innerhalb der Anwendung gehostet.

**Fallback:** Falls der eingebettete Weg deaktiviert oder nicht verfügbar ist, wird weiterhin `mstsc.exe` verwendet.

Der eingebettete Session-Baustein bietet aktuell:

- eingebettete RDP-Darstellung
- Verbinden / Neu verbinden / Trennen
- Vollbild und Rückkehr in den Fenstermodus
- echte Sessionstatus-Ereignisse für Connecting, Connected und Login Complete
- Disconnect-Grund inklusive Extended Disconnect Reason, soweit vom Microsoft-Control verfügbar
- Fatal-Error-Anzeige
- Anzeige der aktuellen Remote-Auflösung
- automatische Wiederverbindungsanzeige mit Versuchszähler und Netzstatus
- Statusmeldung nach erfolgreichem Auto-Reconnect
- `SmartSizing` zur Anpassung an die Fenstergröße, auch während einer aktiven Verbindung
- optionalen Benutzernamen und Windows-/AD-Domäne
- normalen Windows-Credential-Prompt für das Kennwort
- keine Passwortspeicherung durch die Anwendung
- optionale Zwischenablageumleitung pro Zielrechner
- optionale administrative RDP-Sitzung pro Zielrechner
- Remote-Aktionen für App-Switch/Alt+Tab, Start und Task-Manager, soweit unterstützt
- Speicherung der nicht geheimen RDP-Präferenzen für gespeicherte Zielrechner
- isolierte ActiveX-Kapselung hinter `IRdpSessionLauncher`

Die ausführliche RDP-Dokumentation liegt in [`docs/RDP_SESSION.md`](RDP_SESSION.md).

### Active Directory

Die Rechnerliste kann aus Active Directory geladen werden.

Die aktuelle Implementierung verwendet `Get-ADComputer` und setzt deshalb das Microsoft ActiveDirectory-PowerShell-Modul (RSAT) auf dem Admin-Rechner voraus.

Die AD-Anbindung ist bewusst hinter `ITargetDiscoveryService` abstrahiert. Damit können später weitere Quellen ergänzt werden, ohne die Oberfläche umzubauen.

### Online-/Offline-Status

Rechner können parallel per Ping geprüft werden. Die Prüfung arbeitet mit begrenzter Parallelität, damit auch eine größere Anzahl von Rechnern zügig geprüft wird.

Der Status ist nur Laufzeitinformation und wird nicht dauerhaft in der Konfigurationsdatei gespeichert.

---

## Architektur

```text
MainWindow
   |
   +--> RemoteProviderRegistry
   |       |
   |       +--> NetSupportProvider --> PCICTLUI.EXE
   |       |
   |       +--> RdpProvider
   |               |
   |               +--> IRdpSessionLauncher
   |               |       |
   |               |       +--> EmbeddedRdpSessionLauncher
   |               |               |
   |               |               +--> RdpSessionWindow
   |               |                       |
   |               |                       +--> RdpActiveXControl
   |               |                               |
   |               |                               +--> MsTscAx.dll / IMsTscAxEvents
   |               |
   |               +--> mstsc.exe Fallback
   |
   +--> ITargetDiscoveryService
   |       |
   |       +--> DomainComputerDiscoveryService
   |       +--> zukünftige Quellen
   |
   +--> HostAvailabilityService
   |
   +--> ConfigService
           |
           +--> %AppData%\NetSupportRemoteAdmin\settings.json
```

### Erweiterungspunkte

Remote-Technologien implementieren `IRemoteProvider`.

Rechnerquellen implementieren `ITargetDiscoveryService`.

Eingebettete RDP-Sessions werden über `IRdpSessionLauncher` gestartet.

Dadurch bleiben Oberfläche, Rechnerquellen, Remote-Provider und Session-Hosting voneinander getrennt.

---

## Konfiguration

Die Konfiguration liegt unabhängig von NetSupport- und GPO-Profilen im Benutzerprofil:

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
      "description": "Büro",
      "rdpUserName": "max.mustermann",
      "rdpDomain": "CONTOSO",
      "rdpRedirectClipboard": true,
      "rdpAdminSession": false
    }
  ]
}
```

`useEmbeddedRdp` aktiviert standardmäßig den eingebetteten RDP-Viewer. Wird die Option auf `false` gesetzt, nutzt der Provider `mstsc.exe`.

RDP-Passwörter werden bewusst **nicht** in `settings.json` gespeichert. Benutzername, Domäne, Zwischenablage- und Admin-Sitzungspräferenz sind dagegen nicht geheim und können pro gespeichertem Ziel erhalten bleiben.

---

## Projektstruktur

```text
NetSupport/
├── docs/
│   ├── PROJECT_OVERVIEW.md
│   ├── DEVELOPMENT_LOG.md
│   └── RDP_SESSION.md
├── src/
│   └── NetSupport.RemoteAdmin/
│       ├── Controls/
│       │   └── RdpActiveXControl.cs
│       ├── Models/
│       │   ├── RemoteTarget.cs
│       │   ├── RdpRemoteAction.cs
│       │   └── RdpSessionEvents.cs
│       ├── Providers/
│       ├── Services/
│       ├── Views/
│       │   ├── RdpSessionWindow.xaml
│       │   └── RdpSessionWindow.xaml.cs
│       ├── App.xaml
│       ├── MainWindow.xaml
│       └── NetSupport.RemoteAdmin.csproj
├── .github/
│   └── workflows/
├── NetSupport.sln
└── README.md
```

---

## Build

Voraussetzungen:

- Windows 10 oder Windows 11
- .NET 8 SDK
- für AD-Suche: RSAT / ActiveDirectory-PowerShell-Modul
- für NetSupport-Aktionen: installierter NetSupport Manager Control

Build:

```powershell
dotnet restore NetSupport.sln
dotnet build NetSupport.sln --configuration Release
```

Start:

```powershell
dotnet run --project .\src\NetSupport.RemoteAdmin\NetSupport.RemoteAdmin.csproj
```

---

## CI / Qualitätssicherung

Das Repository enthält einen GitHub-Actions-Workflow für Windows.

Bei jedem Push bzw. Pull Request werden Restore und Release-Build ausgeführt. Compilerfehler werden dadurch früh erkannt und direkt im Entwicklungsbranch korrigiert.

RDP Phase 3 wurde erfolgreich unter Windows/.NET 8 gebaut. Die Erweiterungen aus Phase 4 werden über denselben Workflow fortlaufend revalidiert.

Im bisherigen Verlauf wurden unter anderem WPF/WinForms-Namenskonflikte, fehlende `System.IO`-Imports und ungültige Ausdruckszeilen durch CI erkannt und behoben.

---

## Nächste Ausbaustufen

### RDP Phase 4 / 5

Als nächste RDP-Schritte sind vorgesehen:

- Multi-Monitor-Unterstützung
- weitere Tastatur-/Sondertasten-Werkzeuge
- noch verständlichere Fehlertexte
- optionale weitere Redirects wie Laufwerke oder Audio
- Session-Historie bzw. letzte Verbindung

### Rechnerdetails

Geplant sind zusätzliche Informationen wie angemeldeter Benutzer, Betriebssystem, IP-Adresse, letzte Erreichbarkeit, Beschreibung/Standort und bevorzugte Verbindungsart.

### Weitere Discovery-Quellen

Durch `ITargetDiscoveryService` können später unter anderem CSV/JSON, SCCM/MECM, Intune, eigene Inventardienste oder statische Rechnergruppen ergänzt werden.

### Weitere Remote-Provider

Durch `IRemoteProvider` können weitere Fernsteuerungssysteme ergänzt werden, ohne das Hauptfenster umzubauen.

---

## Entwicklungsprinzipien

- Bedienung zuerst
- wenig Klicks für häufige Aufgaben
- keine Abhängigkeit von instabil verteilten NetSupport-UI-Einstellungen
- klare Trennung zwischen UI, Discovery und Remote-Backends
- möglichst wenige externe Abhängigkeiten
- keine Speicherung von Passwörtern in der Anwendungskonfiguration
- Konfiguration verständlich und transparent halten
- neue Funktionen so integrieren, dass sie später austauschbar bleiben

---

## Aktueller Entwicklungsbranch

```text
feature/extensible-remote-admin
```

Aktueller Pull Request:

```text
PR #1 – Add extensible remote admin frontend
```

Diese Datei wird bei weiteren Änderungen als technische und funktionale Projektübersicht weitergeführt.
