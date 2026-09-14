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
4. Rechner auswählen.
5. Rechts erscheinen Status und – nach **Rechnerdetails laden** – IP, Benutzer, Windows-Version, Modell und letzter Prüfzeitpunkt.
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
- Zielrechner-Karte mit Status, Details und Schnellaktionen
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

### Windows Remote Desktop

RDP unterstützt zwei Betriebsarten:

**Eingebettet:** Microsoft Remote Desktop ActiveX Control in einem eigenen Session-Fenster.

**Fallback:** `mstsc.exe`, falls der eingebettete Weg deaktiviert oder nicht verfügbar ist.

Der eingebettete Session-Baustein bietet aktuell:

- Verbinden / Neu verbinden / Trennen
- Vollbild und Fenstermodus
- echte Sessionstatus-Ereignisse
- Disconnect-Grund und Fatal-Error-Anzeige
- Remote-Auflösungsanzeige
- Auto-Reconnect-Status mit Versuchszähler und Netzstatus
- `SmartSizing`
- Multi-Monitor über `UseMultimon`
- Benutzername und Windows-/AD-Domäne
- normalen Windows-Credential-Prompt
- keine Passwortspeicherung
- Zwischenablageumleitung
- administrative RDP-Sitzung
- Remote-Aktionen für App-Switch/Alt+Tab, Start und Task-Manager
- Speicherung der nicht geheimen RDP-Präferenzen für gespeicherte Ziele

Beim externen Fallback werden Admin-Sitzung und Multi-Monitor über `/admin` beziehungsweise `/multimon` an `mstsc.exe` weitergegeben.

Die ausführliche RDP-Dokumentation liegt in [`docs/RDP_SESSION.md`](RDP_SESSION.md).

### Active Directory

Die Rechnerliste kann aus Active Directory geladen werden. Die aktuelle Implementierung verwendet `Get-ADComputer` und benötigt dafür das Microsoft ActiveDirectory-PowerShell-Modul (RSAT).

Die AD-Anbindung ist hinter `ITargetDiscoveryService` abstrahiert. Weitere Quellen wie CSV, SCCM/MECM, Intune oder ein Inventardienst können später ergänzt werden.

### Online-/Offline-Status

Rechner werden parallel per Ping geprüft. Die Prüfung arbeitet mit begrenzter Parallelität. Status und letzter Prüfzeitpunkt sind Laufzeitinformationen und werden nicht dauerhaft gespeichert.

### Rechnerdetails

Die Rechnerdetails sind hinter `ITargetDetailsService` abstrahiert. Die erste Implementierung `PowerShellTargetDetailsService` kombiniert lokale DNS-Auflösung mit einer Remote-CIM-Abfrage über `Get-CimInstance`.

Aktuell werden angezeigt:

- IP-Adresse(n)
- angemeldeter Benutzer
- Windows-Edition und Version
- Hersteller und Modell
- Zeitpunkt der letzten Status-/Detailprüfung

Die CIM-Abfrage verwendet die aktuelle Windows-Identität und die normale WSMan-Konfiguration der Domäne. Ein Zeitlimit von 12 Sekunden verhindert, dass ein blockierter Rechner die Oberfläche lange festhält. Wenn CIM/WSMan nicht verfügbar ist, bleiben DNS, Ping, NetSupport und RDP weiter nutzbar; die Oberfläche kennzeichnet die Detaildaten lediglich als unvollständig.

Die Detaildaten werden bewusst **nicht** in `settings.json` gespeichert, weil sie schnell veralten können.

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
   |               |                               +--> MsTscAx.dll / IMsTscAxEvents
   |               +--> mstsc.exe Fallback
   |
   +--> ITargetDiscoveryService
   |       +--> DomainComputerDiscoveryService --> Get-ADComputer
   |
   +--> ITargetDetailsService
   |       +--> PowerShellTargetDetailsService --> DNS + Get-CimInstance
   |
   +--> HostAvailabilityService
   |
   +--> ConfigService
           +--> %AppData%\NetSupportRemoteAdmin\settings.json
```

### Erweiterungspunkte

- Remote-Technologien: `IRemoteProvider`
- Rechnerquellen: `ITargetDiscoveryService`
- Rechnerdetailquellen: `ITargetDetailsService`
- eingebettete RDP-Sessions: `IRdpSessionLauncher`

Dadurch bleiben Oberfläche, Rechnerquellen, Inventardaten, Remote-Provider und Session-Hosting voneinander getrennt.

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
      "rdpAdminSession": false,
      "rdpUseMultiMonitor": false
    }
  ]
}
```

RDP-Passwörter werden bewusst **nicht** gespeichert. Rechnerdetails wie OS, IP und Benutzer sind ebenfalls nur Laufzeitdaten.

---

## Build und Test

Voraussetzungen:

- Windows 10 oder Windows 11
- .NET 8 SDK für lokale Entwicklung
- RSAT / ActiveDirectory-PowerShell-Modul für AD-Suche
- WSMan/CIM-Zugriff für vollständige Rechnerdetails
- installierter NetSupport Manager Control für NetSupport-Aktionen

```powershell
dotnet restore NetSupport.sln
dotnet build NetSupport.sln --configuration Release
```

GitHub Actions erzeugt zusätzlich einen self-contained Windows-x64-Testbuild als Artefakt `NetSupport.RemoteAdmin-win-x64`. Die praktische Testcheckliste liegt in [`docs/TESTING.md`](TESTING.md).

---

## Nächste Ausbaustufen

- ausgewählte Monitorgruppen statt nur „alle Monitore“
- weitere Tastatur-/Sondertasten-Werkzeuge
- detailliertere RDP-Fehlertexte
- weitere Redirects wie Laufwerke oder Audio
- Session-Historie / letzte Verbindung
- Favoriten und Rechnergruppen
- bevorzugter Remote-Provider pro Rechner
- weitere Discovery-, Details- und Remote-Provider

---

## Entwicklungsprinzipien

- Bedienung zuerst
- wenig Klicks für häufige Aufgaben
- keine Abhängigkeit von instabil verteilten NetSupport-UI-Einstellungen
- klare Trennung zwischen UI, Discovery, Rechnerdetails und Remote-Backends
- möglichst wenige externe Abhängigkeiten
- keine Speicherung von Passwörtern
- flüchtige Inventardaten nicht unnötig persistieren
- Konfiguration verständlich und transparent halten

---

## Aktueller Entwicklungsbranch

```text
feature/extensible-remote-admin
```

Aktueller Pull Request:

```text
PR #1 – Add extensible remote admin frontend
```
