# NetSupport Remote Admin – Projektübersicht

> Diese Datei fasst den aktuellen Stand des Projekts verständlich zusammen und wird bei weiteren Ausbauschritten mitgepflegt.

---

## Ziel des Projekts

NetSupport Remote Admin ist eine kompakte Windows-Anwendung, die die tägliche Fernwartung deutlich einfacher machen soll als die klassische NetSupport-Oberfläche.

Das Ziel ist nicht, das komplette Remote-Protokoll neu zu entwickeln. Stattdessen dient die Anwendung als eigene, übersichtliche Steuerzentrale und verwendet vorhandene Remote-Techniken wie NetSupport Manager und Windows Remote Desktop als austauschbare Backends.

Die Anwendung soll dauerhaft im Hintergrund laufen können, schnell erreichbar sein und typische Aktionen mit möglichst wenigen Klicks ausführen.

---

## Aktueller Bedienablauf

1. Anwendung starten – sie kann dauerhaft im Windows-Infobereich weiterlaufen.
2. Rechner direkt per Name/IP eingeben oder Rechner aus der Domäne laden.
3. Optional den Online-/Offline-Status der Rechner prüfen.
4. Rechner in der Liste auswählen.
5. Rechts erscheint die Aktionskarte des ausgewählten Rechners.
6. Gewünschte Aktion direkt starten, zum Beispiel **Steuern**, **Nur ansehen**, **RDP**, **CMD**, **Dateien**, **Inventar** oder **Chat**.
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

Windows RDP ist als zweiter Remote-Provider vorhanden.

Aktuell wird dafür noch `mstsc.exe` gestartet. Der nächste größere Ausbauschritt ist ein eigener Session-Baustein, damit RDP später direkt innerhalb der Anwendung angezeigt werden kann.

### Active Directory

Die Rechnerliste kann aus Active Directory geladen werden.

Die aktuelle Implementierung verwendet `Get-ADComputer` und setzt deshalb das Microsoft ActiveDirectory-PowerShell-Modul (RSAT) auf dem Admin-Rechner voraus.

Die AD-Anbindung ist bewusst hinter `ITargetDiscoveryService` abstrahiert. Damit können später weitere Quellen ergänzt werden, ohne die Oberfläche umzubauen.

### Online-/Offline-Status

Rechner können parallel per Ping geprüft werden.

Die Prüfung arbeitet mit begrenzter Parallelität, damit auch eine größere Anzahl von Rechnern zügig geprüft wird, ohne unnötig viele gleichzeitige Netzwerkzugriffe zu erzeugen.

Der Status ist nur Laufzeitinformation und wird nicht dauerhaft in der Konfigurationsdatei gespeichert.

---

## Architektur

```text
MainWindow
   |
   +--> RemoteProviderRegistry
   |       |
   |       +--> NetSupportProvider --> PCICTLUI.EXE
   |       +--> RdpProvider --------> mstsc.exe
   |       +--> zukünftige Provider
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

Remote-Technologien implementieren:

```csharp
public interface IRemoteProvider
{
    string Id { get; }
    string DisplayName { get; }
    IReadOnlyCollection<RemoteAction> SupportedActions { get; }
    bool IsAvailable { get; }
    Task ConnectAsync(
        RemoteTarget target,
        RemoteAction action,
        CancellationToken cancellationToken = default);
}
```

Rechnerquellen implementieren:

```csharp
public interface ITargetDiscoveryService
{
    string DisplayName { get; }
    Task<IReadOnlyList<RemoteTarget>> DiscoverAsync(
        CancellationToken cancellationToken = default);
}
```

Dadurch bleiben Oberfläche, Rechnerquellen und Fernsteuerungs-Technologien voneinander getrennt.

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
  "useFullScreenRdp": false,
  "targets": [
    {
      "name": "PC-001",
      "host": "PC-001",
      "description": "Büro"
    }
  ]
}
```

---

## Projektstruktur

```text
NetSupport/
├── docs/
│   └── PROJECT_OVERVIEW.md
├── src/
│   └── NetSupport.RemoteAdmin/
│       ├── Models/
│       ├── Providers/
│       ├── Services/
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

Im bisherigen Verlauf wurden unter anderem WPF/WinForms-Namenskonflikte, fehlende `System.IO`-Imports und ungültige Ausdruckszeilen durch CI erkannt und behoben.

---

## Nächste Ausbaustufen

### 1. Eingebettete RDP-Session

Ziel ist, RDP nicht mehr ausschließlich in einem separaten `mstsc.exe`-Fenster zu starten, sondern eine eigene Session-Oberfläche innerhalb der Anwendung bereitzustellen.

Geplante Session-Funktionen:

- Verbinden / Trennen
- Vollbild
- Auf Fenstergröße skalieren
- Zwischenablage
- Multi-Monitor-Vorbereitung
- Sessionstatus
- zentrale Toolbar

### 2. Rechnerdetails

Geplant sind zusätzliche Informationen wie:

- angemeldeter Benutzer
- Betriebssystem
- IP-Adresse
- letzte Erreichbarkeit
- Beschreibung / Standort
- bevorzugte Verbindungsart

### 3. Weitere Discovery-Quellen

Durch `ITargetDiscoveryService` können später weitere Quellen ergänzt werden, zum Beispiel:

- CSV / JSON
- SCCM / MECM
- Intune
- eigener Inventardienst
- statische Rechnergruppen

### 4. Weitere Remote-Provider

Durch `IRemoteProvider` können weitere Fernsteuerungssysteme ergänzt werden, ohne das Hauptfenster umzubauen.

---

## Entwicklungsprinzipien

- Bedienung zuerst
- wenig Klicks für häufige Aufgaben
- keine Abhängigkeit von instabil verteilten NetSupport-UI-Einstellungen
- klare Trennung zwischen UI, Discovery und Remote-Backends
- möglichst wenige externe Abhängigkeiten
- Konfiguration verständlich und transparent halten
- neue Funktionen nur so integrieren, dass sie später austauschbar bleiben

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
