# NetSupport Remote Admin – Projektübersicht

> Diese Datei fasst den aktuellen funktionalen und technischen Stand des Projekts zusammen und wird bei weiteren Ausbauschritten mitgepflegt.

---

## Ziel

**NetSupport Remote Admin** ist eine kompakte Windows-Anwendung für die tägliche Administration einer größeren Anzahl von Domänenrechnern.

NetSupport Manager bleibt das einzige freigegebene Remote-Control-Backend. Die Anwendung vereinfacht Suche, Auswahl, Statusprüfung, Rechnerdetails und das Starten der vorhandenen NetSupport-Funktionen.

> **Domänenvorgabe:** RDP ist für die Fernwartung in dieser Umgebung deaktiviert und wird von dieser Anwendung nicht angeboten.

Details: [`DOMAIN_REMOTE_POLICY.md`](DOMAIN_REMOTE_POLICY.md).

---

## Wichtige Prinzipien

- wenige Klicks für häufige Aufgaben
- Tray-/Infobereich-Betrieb
- NetSupport Manager als einziges Remote-Control-Backend
- keine Umgehung von Domänen-/Sicherheitsvorgaben
- keine Speicherung von Kennwörtern
- explizites Speichern dauerhafter Zielattribute
- flüchtige Inventardaten bleiben flüchtig
- Diagnose darf die Fernwartung nicht blockieren
- Supportdaten werden gezielt und anonymisierbar erzeugt
- Architektur bleibt erweiterbar, obwohl aktuell nur NetSupport freigegeben ist

---

## Aktueller Bedienablauf

1. Anwendung starten oder aus dem Tray öffnen.
2. Rechner direkt per Name/IP eingeben oder aus Active Directory laden.
3. Liste über Text, Gruppe, Favoriten oder gespeicherte Ansicht filtern.
4. Ziel auswählen.
5. Optional Online-Status und Rechnerdetails laden.
6. Favorit, Gruppe und Standard-Provider speichern.
7. NetSupport-Schnellaktion oder Standardverbindung starten.
8. Unter **Zuletzt verwendet** Startversuche einsehen oder als CSV exportieren.
9. Unter **Erweitert → Einstellungen** Start-, Autostart-, Diagnose- und NetSupport-Optionen ändern.
10. Bei Bedarf **Systemzustand** prüfen oder ein anonymisierbares Supportpaket erzeugen.

---

## Remotezugriffsrichtlinie

Im produktiven Startpfad wird ausschließlich registriert:

```text
NetSupportProvider
```

Architektur:

```text
App
  |
  +--> RemoteProviderRegistry
          |
          +--> NetSupportProvider
                  |
                  +--> PCICTLUI.EXE
```

Das Hauptfenster blockiert zusätzlich jeden Provider-Start, dessen ID nicht

```text
netsupport
```

entspricht.

Frühere Entwicklungsstände enthielten testweise RDP-Unterstützung. Nach Klärung der Domänenvorgabe wurden RDP-Provider, ActiveX-Control, RDP-Sessionfenster und `.rdp`-Dateierzeugung aus dem aktiven Projekt entfernt.

Alte RDP-Felder in vorhandenen `settings.json`-Dateien werden von `System.Text.Json` beim Einlesen ignoriert. Ein alter `preferredProviderId` wird auf `netsupport` normalisiert; beim nächsten Speichern wird nur noch das aktuelle Datenmodell geschrieben.

---

## NetSupport Manager

`NetSupportProvider` startet `PCICTLUI.EXE`.

Aktuelle Schnellaktionen:

| Aktion | NetSupport-Modus |
|---|---|
| **Steuern** | Control |
| **Nur ansehen** | View |
| **Chat** | Chat |
| **Inventar** | Inventory |
| **Remote CMD** | Remote Command Prompt |
| **Dateien** | File Transfer |

Der Pfad zu `PCICTLUI.EXE` ist über die Einstellungsseite änderbar. Die Providerverfügbarkeit wird anschließend ohne Neustart neu bewertet.

---

## Zielorganisation

Persistierbare Zielattribute:

```json
{
  "name": "PC-001",
  "host": "PC-001",
  "description": "Büro 1",
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
- Textsuche über Name, Host, Beschreibung und Gruppe
- Doppelklick startet die NetSupport-Standardverbindung

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

`HostAvailabilityService` prüft Rechner parallel per Ping. Status und letzter Prüfzeitpunkt sind Laufzeitdaten und werden nicht dauerhaft gespeichert.

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

Angezeigt werden unter anderem:

- IP-Adresse(n)
- angemeldeter Windows-Benutzer
- Windows-Edition/-Version
- Hersteller/Modell
- letzter Prüfzeitpunkt
- jüngster bekannter Remote-Start

CIM/WSMan-Fehler blockieren die NetSupport-Funktionen nicht.

---

## Lokaler Startverlauf und CSV-Export

`ISessionHistoryService` speichert die letzten Remote-Aktionsstarts separat in:

```text
%AppData%\NetSupportRemoteAdmin\session-history.json
```

Gespeichert werden:

- Zeitpunkt
- Ziel
- Provider
- Aktion
- Start erfolgreich/fehlgeschlagen
- Fehlertext bei fehlgeschlagenem Start

Der Verlauf ist auf 100 Einträge begrenzt und kann als semikolongetrennte UTF-8-CSV mit BOM exportiert werden.

---

## Einstellungen und Autostart

Unter **Erweitert → Einstellungen** stehen aktuell zur Verfügung:

- Mit Windows starten
- beim Start minimiert öffnen
- Diagnoseprotokoll aktivieren/deaktivieren
- Pfad zu `PCICTLUI.EXE`
- Systemzustand öffnen
- Diagnoseordner öffnen
- Supportpaket erzeugen

Autostart wird ausschließlich im Profil des aktuellen Benutzers verwaltet:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

Wert:

```text
NetSupportRemoteAdmin
```

---

## Systemzustand

`ISystemHealthService` prüft ausschließlich den lokalen Admin-PC und scannt keine Domänenrechner.

Aktuelle Checks:

- Remotezugriffsrichtlinie = NetSupport-only
- AppData-Verzeichnis beschreibbar
- `PCICTLUI.EXE` vorhanden
- ActiveDirectory-PowerShell-Modul / RSAT
- lokale CIM-/WSMan-Grundfunktion
- Autostartzustand
- Diagnoseprotokollzustand

Details: [`SYSTEM_HEALTH.md`](SYSTEM_HEALTH.md).

---

## Diagnoseprotokoll

Optionales Log:

```text
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

Rotation bei ungefähr 2 MB nach:

```text
application.log.1
```

Das Log enthält Betriebsereignisse und Fehler, aber keine Kennwörter, Bildschirminhalte, Zwischenablageinhalte oder Inhalte übertragener Dateien.

---

## Supportpaket

`ISupportBundleService` erzeugt auf Wunsch ein lokales ZIP für Fehlersuche. Standardmäßig ist die Anonymisierung aktiviert.

Enthalten:

```text
README.txt
system-info.json
configuration-summary.json
recent-history.json
recent-errors.txt
logs/
```

Die originale `settings.json` wird nicht kopiert. Kennwörter, gespeicherte Credentials und Sitzungsinhalte werden nicht aufgenommen.

Die Konfigurationsübersicht enthält außerdem explizit:

```text
remoteAccessPolicy = NetSupport-only
```

Details: [`SUPPORT_BUNDLE.md`](SUPPORT_BUNDLE.md).

---

## Persistente Dateien

Konfiguration:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

Startverlauf:

```text
%AppData%\NetSupportRemoteAdmin\session-history.json
```

Diagnose:

```text
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

Support-ZIPs werden nur am vom Benutzer gewählten Zielpfad abgelegt.

---

## Architektur

```text
MainWindow
   |
   +--> RemoteProviderRegistry
   |       +--> NetSupportProvider --> PCICTLUI.EXE
   |
   +--> ITargetDiscoveryService --> DomainComputerDiscoveryService
   +--> ITargetDetailsService --> PowerShellTargetDetailsService
   +--> ISessionHistoryService --> JsonSessionHistoryService
   +--> IAutoStartService --> WindowsAutoStartService --> HKCU Run
   +--> IDiagnosticLogService --> DiagnosticLogService
   +--> ISupportBundleService --> SupportBundleService
   +--> ISystemHealthService --> SystemHealthService
   +--> HostAvailabilityService
   +--> ConfigService
```

Erweiterungspunkte:

```text
IRemoteProvider
ITargetDiscoveryService
ITargetDetailsService
ISessionHistoryService
IAutoStartService
IDiagnosticLogService
ISupportBundleService
ISystemHealthService
```

Die Architektur bleibt erweiterbar. Ein zusätzlicher Remote-Provider darf in dieser Umgebung jedoch nur nach ausdrücklicher Freigabe durch die Domänen-/Sicherheitsvorgaben registriert werden.

---

## Build und Test

```powershell
dotnet restore NetSupport.sln
dotnet build NetSupport.sln --configuration Release
```

GitHub Actions erzeugt zusätzlich einen self-contained Windows-x64-Testbuild:

```text
NetSupport.RemoteAdmin-win-x64
```

Praktische Prüfschritte: [`TESTING.md`](TESTING.md).

---

## Nächste sinnvolle Ausbaustufen

- NetSupport-spezifische Status-/Diagnoseverbesserungen
- bessere NetSupport-Prozess-/Startfehlertexte
- optionales Erkennen der installierten NetSupport-Version
- zusätzliche Rechner-/Domänenmetadaten
- Filter/Zeitraum für History-Export
- weitere freigegebene Discovery-/Inventarquellen

---

## Entwicklungsbranch

```text
feature/extensible-remote-admin
PR #1 – Add extensible remote admin frontend
```
