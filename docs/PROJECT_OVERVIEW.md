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
- lokale NetSupport-Installation wird nachvollziehbar statt durch Laufwerksscans erkannt
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
10. Bei Bedarf **NetSupport prüfen…**, **Systemzustand** oder ein anonymisierbares Supportpaket verwenden.

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

Alte RDP-Felder in vorhandenen `settings.json`-Dateien werden beim Einlesen ignoriert. Ein alter `preferredProviderId` wird auf `netsupport` normalisiert; beim nächsten Speichern wird nur noch das aktuelle Datenmodell geschrieben.

---

## NetSupport Manager

`NetSupportProvider` startet ausschließlich `PCICTLUI.EXE`.

Aktuelle Schnellaktionen:

| Aktion | NetSupport-Modus |
|---|---|
| **Steuern** | Control |
| **Nur ansehen** | View |
| **Chat** | Chat |
| **Inventar** | Inventory |
| **Remote CMD** | Remote Command Prompt |
| **Dateien** | File Transfer |

### Installationserkennung

Neue Schicht:

```text
INetSupportInstallationService
   +--> NetSupportInstallationService
```

Erkannt werden lokal:

- konfigurierter Pfad
- Program Files (x86)
- Program Files
- Windows-Uninstall-Registry in HKLM/HKCU und 32-/64-Bit-Sicht

Es findet keine rekursive Laufwerkssuche statt.

Unter **Erweitert → Einstellungen → NetSupport Manager** gibt es:

- **Durchsuchen…**
- **Automatisch erkennen**
- **NetSupport prüfen…**

Das Prüffenster zeigt Kandidaten, Quelle, Produkt-/Dateiversion und Hersteller. Ein gültiger Pfad kann bewusst übernommen werden.

### Start-Härtung

Vor jedem Prozessstart wird geprüft:

- Pfad ist auflösbar
- Datei existiert
- Dateiname ist exakt `PCICTLUI.EXE`
- Zielhost/IP ist für die kontrollierte NetSupport-CLI zulässig

Eine manuell manipulierte Konfiguration kann dadurch nicht benutzt werden, um über den Remote-Provider ein beliebiges anderes Programm zu starten.

Die optionale Diagnose protokolliert die erzeugte NetSupport-CLI und nach erfolgreichem `Process.Start` die Prozess-ID.

Details: [`NETSUPPORT_INTEGRATION.md`](NETSUPPORT_INTEGRATION.md).

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

```text
ITargetDetailsService
   +--> PowerShellTargetDetailsService
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

Gespeichert werden Zeitpunkt, Ziel, Provider, Aktion, Start-Erfolg/-Fehler und optionaler Fehlertext.

Der Verlauf ist auf 100 Einträge begrenzt und kann als semikolongetrennte UTF-8-CSV mit BOM exportiert werden.

---

## Einstellungen und Autostart

Unter **Erweitert → Einstellungen** stehen aktuell zur Verfügung:

- Mit Windows starten
- beim Start minimiert öffnen
- Diagnoseprotokoll aktivieren/deaktivieren
- NetSupport-Control-Pfad auswählen/erkennen/prüfen
- Systemzustand öffnen
- Diagnoseordner öffnen
- Supportpaket erzeugen

Autostart wird ausschließlich im Profil des aktuellen Benutzers verwaltet:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

---

## Systemzustand

`ISystemHealthService` prüft ausschließlich den lokalen Admin-PC.

Aktuelle Checks:

- Remotezugriffsrichtlinie = NetSupport-only
- AppData-Verzeichnis beschreibbar
- konfigurierter `PCICTLUI.EXE`-Pfad vorhanden
- NetSupport-Produkt-/Dateiversion
- alternative lokale NetSupport-Installation, falls der gespeicherte Pfad veraltet ist
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

`configuration-summary.json` enthält unter anderem:

```text
remoteAccessPolicy = NetSupport-only
NetSupport-Produkt-/Dateiversion
NetSupport-Erkennungsstatus
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
   +--> INetSupportInstallationService --> NetSupportInstallationService
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
INetSupportInstallationService
ITargetDiscoveryService
ITargetDetailsService
ISessionHistoryService
IAutoStartService
IDiagnosticLogService
ISupportBundleService
ISystemHealthService
```

Ein zusätzlicher Remote-Provider darf in dieser Umgebung nur nach ausdrücklicher Freigabe durch die Domänen-/Sicherheitsvorgaben registriert werden.

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

- NetSupport-Installationsordner optional auf notwendige Begleitdateien prüfen
- Startfehler von späteren NetSupport-Verbindungsfehlern besser unterscheiden
- freigegebene NetSupport-Konfigurationsprofile nur bei klarer administrativer Vorgabe integrieren
- zusätzliche Rechner-/Domänenmetadaten
- Filter/Zeitraum für History-Export
- weitere freigegebene Discovery-/Inventarquellen

---

## Entwicklungsbranch

```text
feature/extensible-remote-admin
PR #1 – Add NetSupport-only remote admin frontend
```
