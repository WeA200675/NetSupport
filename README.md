# NetSupport Remote Admin

Eine erweiterbare .NET-8/WPF-Anwendung für die tägliche Fernwartung von Windows-/Domänenrechnern. NetSupport Manager bleibt als bewährtes Backend erhalten, bekommt aber eine kompaktere Bedienoberfläche; Windows RDP steht zusätzlich als alternativer Provider zur Verfügung.

## Dokumentation

- [`docs/PROJECT_OVERVIEW.md`](docs/PROJECT_OVERVIEW.md) – aktueller Funktions- und Architekturstand
- [`docs/DEVELOPMENT_LOG.md`](docs/DEVELOPMENT_LOG.md) – chronologische Entwicklungsentscheidungen
- [`docs/RDP_SESSION.md`](docs/RDP_SESSION.md) – eingebettetes RDP, Events, Sicherheit, Audio/Redirects und Multi-Monitor
- [`docs/RDP_SELECTED_MONITORS.md`](docs/RDP_SELECTED_MONITORS.md) – gezielte Auswahl bestimmter RDP-Monitore und externer RDP-Dateipfad
- [`docs/TARGET_ORGANIZATION.md`](docs/TARGET_ORGANIZATION.md) – Favoriten, Gruppen und Standard-Provider
- [`docs/SAVED_VIEWS_AND_HISTORY.md`](docs/SAVED_VIEWS_AND_HISTORY.md) – gespeicherte Filteransichten und lokaler Startverlauf
- [`docs/OPERATIONS.md`](docs/OPERATIONS.md) – Einstellungen, Autostart, Diagnose und CSV-Export
- [`docs/SUPPORT_BUNDLE.md`](docs/SUPPORT_BUNDLE.md) – anonymisierbares Diagnose-/Supportpaket
- [`docs/TESTING.md`](docs/TESTING.md) – Testbuild und praktische Prüfschritte

## Aktueller Funktionsumfang

### Hauptoberfläche

- Tray-/Infobereich-Betrieb
- direkte Verbindung per Rechnername/IP
- Active-Directory-Rechnersuche über `Get-ADComputer`
- Text-, Gruppen- und Favoritenfilter
- gespeicherte Filteransichten
- Favoriten und frei benennbare Rechnergruppen
- bevorzugter Remote-Provider pro Ziel
- Standardverbindung per Button oder Doppelklick
- parallele Online-/Offline-Prüfung
- Rechnerdetails über DNS + CIM/WSMan
- lokaler Verlauf der zuletzt gestarteten Remote-Aktionen
- CSV-Export des Verlaufs
- eigene Einstellungsseite
- optionaler Windows-Autostart pro Benutzer
- optionales lokales Diagnoseprotokoll
- anonymisierbares Supportpaket als ZIP

### NetSupport Manager

`PCICTLUI.EXE` wird als Backend verwendet. Unterstützt werden aktuell:

- Steuern
- Nur ansehen
- Chat
- Inventar
- Remote CMD
- Dateiübertragung

Der Pfad zu `PCICTLUI.EXE` kann direkt in **Erweitert → Einstellungen** geändert werden. Die Providerliste wird danach ohne Neustart aktualisiert.

### Windows Remote Desktop

Normalerweise kann RDP in einem eigenen Fenster der Anwendung eingebettet werden. Aktuell vorhanden:

- Connect / Reconnect / Disconnect
- Vollbild
- Connecting-/Connected-/Login-/Disconnect-Ereignisse
- verständlichere Disconnect-Informationen
- Auto-Reconnect-Anzeige
- SmartSizing
- Zwischenablage
- Laufwerksumleitung (standardmäßig aus)
- Mikrofonumleitung (standardmäßig aus)
- Audioausgabe lokal / remote / aus
- Admin-Sitzung
- Benutzername/Domäne ohne Passwortspeicherung
- Remote Alt+Tab, Start und Task-Manager
- Multi-Monitor über `UseMultimon`

Für eine **gezielte Auswahl einzelner lokaler Monitore** gibt es zusätzlich einen dokumentierten externen RDP-Pfad:

1. **IDs anzeigen** startet `mstsc.exe /l`.
2. Gewünschte IDs, z. B. `0,1`, unter **Erweitert → Gezielte RDP-Monitore** eintragen.
3. **Speichern / Aktualisieren**.
4. Beim nächsten RDP-Start erzeugt das Tool eine credential-freie `.rdp`-Datei mit `selectedmonitors` und startet den Windows-RDP-Client.

Ohne eingetragene Monitor-IDs bleibt das eingebettete RDP-Verhalten erhalten, sofern es global aktiviert ist.

Der normale externe `mstsc.exe`-Fallback verwendet ebenfalls eine erzeugte `.rdp`-Datei. Dadurch werden Zwischenablage, Audio, Mikrofon, Laufwerke und Multi-Monitor auch im externen Pfad konsistent übertragen, ohne Benutzernamen oder Kennwörter in die Datei zu schreiben.

## Einstellungen, Betrieb und Support

Unter **Erweitert → Einstellungen** stehen aktuell zur Verfügung:

- Mit Windows starten
- beim Start minimiert im Infobereich öffnen
- Diagnoseprotokoll aktivieren/deaktivieren
- eingebetteten RDP-Viewer bevorzugen
- externes RDP standardmäßig im Vollbild starten
- NetSupport-Executable auswählen
- Diagnoseordner öffnen
- anonymisierbares Supportpaket erstellen

Windows-Autostart wird ausschließlich im Benutzerprofil über

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

verwaltet.

Das optionale Diagnoseprotokoll liegt unter:

```text
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

Es rotiert bei ungefähr 2 MB nach `application.log.1`. Passwörter, RDP-Credentials und Sitzungsinhalte werden nicht protokolliert.

Das Supportpaket erzeugt eine ZIP-Datei mit bereinigten System-, Konfigurations-, History- und Logdaten. Die Original-`settings.json` sowie Passwörter/Credentials werden nicht aufgenommen. Die Anonymisierung von Host-/Benutzer-/Rechnerkennungen ist standardmäßig aktiviert.

Details stehen in [`docs/OPERATIONS.md`](docs/OPERATIONS.md) und [`docs/SUPPORT_BUNDLE.md`](docs/SUPPORT_BUNDLE.md).

## Persistente Dateien

Konfiguration:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

Lokaler Startverlauf:

```text
%AppData%\NetSupportRemoteAdmin\session-history.json
```

Diagnose:

```text
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

Generierte externe RDP-Verbindungen:

```text
%AppData%\NetSupportRemoteAdmin\rdp\
```

RDP-Passwörter oder andere Credentials werden von der Anwendung nicht gespeichert.

## Erweiterungspunkte

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

Damit bleiben Remote-Backends, Rechnerquellen, Inventardaten, Verlauf, RDP-Verbindungsdateien, Autostart, Diagnose und Supportpaketerzeugung voneinander getrennt.

## Build

Voraussetzungen:

- Windows 10/11
- .NET 8 SDK
- optional: RSAT ActiveDirectory PowerShell für Domänensuche
- optional: CIM/WSMan-Zugriff für Rechnerdetails
- NetSupport Manager Control für NetSupport-Aktionen

```powershell
dotnet restore NetSupport.sln
dotnet build NetSupport.sln --configuration Release
```

## CI-Testbuild

Erfolgreiche GitHub-Actions-Läufe veröffentlichen zusätzlich einen self-contained Windows-x64-Build:

```text
NetSupport.RemoteAdmin-win-x64
```

Damit kann der aktuelle Stand auf einem Windows-x64-Admin-PC ohne separat installierte .NET-8-Laufzeit getestet werden.
