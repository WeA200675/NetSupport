# NetSupport Remote Admin

Eine erweiterbare .NET-8/WPF-Anwendung für die tägliche Fernwartung von Windows-/Domänenrechnern über **NetSupport Manager**.

> **Domänenvorgabe:** RDP ist in dieser Umgebung als Fernwartungsweg deaktiviert und wird von dieser Anwendung nicht angeboten. Remotezugriff erfolgt ausschließlich über NetSupport Manager.

## Dokumentation

- [`docs/PROJECT_OVERVIEW.md`](docs/PROJECT_OVERVIEW.md) – aktueller Funktions- und Architekturstand
- [`docs/DOMAIN_REMOTE_POLICY.md`](docs/DOMAIN_REMOTE_POLICY.md) – NetSupport-only-Domänenrichtlinie und Migration älterer Konfigurationen
- [`docs/NETSUPPORT_INTEGRATION.md`](docs/NETSUPPORT_INTEGRATION.md) – Installationserkennung, PCICTLUI-CLI, Zielvalidierung und Startpfad
- [`docs/NETSUPPORT_CONTROL_PROFILES.md`](docs/NETSUPPORT_CONTROL_PROFILES.md) – lokale Control-Profile, `/N` und optionale `/F`-Profilbindung
- [`docs/NETSUPPORT_PREFERRED_ACTIONS.md`](docs/NETSUPPORT_PREFERRED_ACTIONS.md) – bevorzugte NetSupport-Aktion pro Rechner und Doppelklickverhalten
- [`docs/AUTOMATED_TESTS.md`](docs/AUTOMATED_TESTS.md) – automatisierte CLI-/Sicherheits-/Migrations-/Recovery-Tests und ihre Grenzen
- [`docs/CONFIGURATION_LIFECYCLE.md`](docs/CONFIGURATION_LIFECYCLE.md) – Schema-Version, Normalisierung, atomisches Speichern, Backup und Recovery
- [`docs/DEVELOPMENT_LOG.md`](docs/DEVELOPMENT_LOG.md) – chronologische Entwicklungsentscheidungen
- [`docs/TARGET_ORGANIZATION.md`](docs/TARGET_ORGANIZATION.md) – Favoriten, Gruppen und Standardaktion
- [`docs/SAVED_VIEWS_AND_HISTORY.md`](docs/SAVED_VIEWS_AND_HISTORY.md) – gespeicherte Filteransichten und lokaler Startverlauf
- [`docs/OPERATIONS.md`](docs/OPERATIONS.md) – Einstellungen, Autostart, Diagnose und CSV-Export
- [`docs/SUPPORT_BUNDLE.md`](docs/SUPPORT_BUNDLE.md) – anonymisierbares Diagnose-/Supportpaket
- [`docs/SYSTEM_HEALTH.md`](docs/SYSTEM_HEALTH.md) – lokaler Systemzustand des Admin-PCs
- [`docs/TESTING.md`](docs/TESTING.md) – Testbuild und praktische Prüfschritte

## Aktueller Funktionsumfang

### Hauptoberfläche

- Tray-/Infobereich-Betrieb
- pro Windows-Sitzung nur eine laufende Instanz
- direkte Verbindung per Rechnername oder IP
- Active-Directory-Rechnersuche über `Get-ADComputer`
- Text-, Gruppen- und Favoritenfilter
- gespeicherte Filteransichten
- Favoriten und frei benennbare Rechnergruppen
- bevorzugte NetSupport-Standardaktion pro gespeichertem Rechner
- Doppelklick verwendet die zuletzt gespeicherte NetSupport-Aktion
- parallele Erreichbarkeitsprüfung; fehlgeschlagener Ping wird bewusst als **Nicht erreichbar** statt als sicher „offline“ angezeigt
- Rechnerdetails über DNS + CIM/WSMan
- lokaler Verlauf der gestarteten Remote-Aktionen
- CSV-Export des Verlaufs
- eigene Einstellungsseite
- optionaler Windows-Autostart pro Benutzer
- optionales lokales Diagnoseprotokoll
- anonymisierbares Supportpaket als ZIP
- lokale Systemzustandsprüfung für Admin-PC-Voraussetzungen

### NetSupport Manager

`PCICTLUI.EXE` wird als einziges Remote-Control-Backend verwendet.

Unterstützte Schnell- und Standardaktionen:

- **Steuern**
- **Nur ansehen**
- **Chat**
- **Inventar**
- **Remote CMD**
- **Dateiübertragung**

Für einen gespeicherten Rechner kann unter **Erweitert → Aktion** eine bevorzugte Aktion gewählt werden. **Speichern / Aktualisieren** schreibt sie als lesbares `preferredAction` in die lokale Konfiguration. Fehlt der Wert oder ist er ungültig, bleibt **Steuern / Control** der Fallback. Eine noch nicht gespeicherte Auswahl kann über den Standard-Button testweise gestartet werden; Doppelklick verwendet dagegen bewusst die zuletzt gespeicherte Aktion.

Details: [`docs/NETSUPPORT_PREFERRED_ACTIONS.md`](docs/NETSUPPORT_PREFERRED_ACTIONS.md).

Unter **Erweitert → Einstellungen → NetSupport Manager** stehen außerdem zur Verfügung:

- **Durchsuchen…** für manuelle Auswahl
- **Automatisch erkennen** über Standardpfade und lokale Installationsregistrierung
- **NetSupport prüfen…** mit Kandidaten, Quelle, Produkt-/Dateiversion und Hersteller
- vorhandenes lokales **Control-Profil** auswählen
- Profile aus `HKCU\Software\NetSupport Ltd\PCICTL\ConfigList` neu laden
- optional **Control auf dieses Profil festlegen (/F)**

Ein konfiguriertes Control-Profil wird beim Start mit `/N` an NetSupport übergeben. Mit aktivierter Profilbindung kommt `/F` hinzu. Fehlt das konfigurierte Profil lokal, wird der Remote-Start absichtlich blockiert, statt stillschweigend ein anderes Profil zu verwenden.

Details: [`docs/NETSUPPORT_CONTROL_PROFILES.md`](docs/NETSUPPORT_CONTROL_PROFILES.md).

Die Installationserkennung durchsucht keine Laufwerke rekursiv und baut keine Remoteverbindung auf.

Der Startpfad validiert außerdem:

- ausschließlich `PCICTLUI.EXE` darf als Remote-Backend gestartet werden
- Rechnername/IP wird vor dem Prozessstart geprüft
- IP-Ziele verwenden die dokumentierte `/c">Adresse"`-Form
- optionale Profilnamen werden vor der rohen Kommandozeile validiert; Anführungszeichen, Steuerzeichen und Backslashes sind nicht zulässig
- ein konfiguriertes Profil muss für den aktuellen Windows-Benutzer vorhanden sein
- die tatsächlich erzeugte NetSupport-Befehlszeile und gestartete PID können im Diagnoseprotokoll nachvollzogen werden

Details: [`docs/NETSUPPORT_INTEGRATION.md`](docs/NETSUPPORT_INTEGRATION.md).

## Domänenrichtlinie

Die Anwendung registriert im produktiven Startpfad nur den Provider

```text
netsupport
```

und blockiert andere Provider vor dem Start zusätzlich im Hauptfenster.

Frühere Entwicklungsstände enthielten testweise RDP-Komponenten. Diese wurden nach Klärung der Domänenvorgabe aus dem aktiven Projekt entfernt. Alte RDP-Felder in bestehenden `settings.json`-Dateien werden beim Laden ignoriert und beim nächsten Speichern nicht mehr geschrieben.

Details: [`docs/DOMAIN_REMOTE_POLICY.md`](docs/DOMAIN_REMOTE_POLICY.md).

## Einstellungen, Betrieb und Support

Unter **Erweitert → Einstellungen** stehen aktuell zur Verfügung:

- Mit Windows starten
- beim Start minimiert im Infobereich öffnen
- Diagnoseprotokoll aktivieren/deaktivieren
- NetSupport-Executable auswählen oder automatisch erkennen
- NetSupport-Installation/Version prüfen
- lokales NetSupport-Control-Profil auswählen und optional mit `/F` binden
- **Systemzustand** des Admin-PCs prüfen
- Diagnoseordner öffnen
- anonymisierbares Supportpaket erstellen

Windows-Autostart wird ausschließlich im Benutzerprofil verwaltet:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

Das optionale Diagnoseprotokoll liegt unter:

```text
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

Es rotiert bei ungefähr 2 MB nach `application.log.1`. Kennwörter und Sitzungsinhalte werden nicht protokolliert.

Das Supportpaket enthält zusätzlich NetSupport-Produkt-/Dateiversion, lokalen Erkennungsstatus und den bereinigten Profilstatus, aber keine Credentials oder Original-`settings.json`. Die Anonymisierung des tatsächlich erzeugten ZIPs wird automatisiert mitgetestet.

## Konfiguration und Recovery

Die Konfiguration wird zentral normalisiert und versioniert. Unversionierte Altdateien werden auf das aktuelle Schema migriert; eine Konfiguration aus einer neueren, noch nicht unterstützten Schema-Version wird bewusst nicht von einer älteren EXE überschrieben.

Schreibvorgänge erfolgen über temporäre Dateien und anschließenden Austausch, statt `settings.json` während der JSON-Serialisierung zu truncaten.

Primärdatei:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

Normalisierte Recovery-Datei:

```text
%AppData%\NetSupportRemoteAdmin\settings.json.bak
```

Ist die Primärdatei beschädigt oder fehlt, wird ein gültiges Backup automatisch zur Wiederherstellung verwendet. Sind beide Dateien beschädigt, erfolgt keine stille Rücksetzung auf Defaults.

Details: [`docs/CONFIGURATION_LIFECYCLE.md`](docs/CONFIGURATION_LIFECYCLE.md).

## Systemzustand

Die lokale Zustandsprüfung kontrolliert unter anderem:

- NetSupport-only-Remotezugriffsrichtlinie
- AppData-Schreibbarkeit
- Pfad/Verfügbarkeit und Version von `PCICTLUI.EXE`
- alternative NetSupport-Installation, falls der konfigurierte Pfad veraltet ist
- konfiguriertes NetSupport-Control-Profil und optionale `/F`-Bindung
- RSAT / ActiveDirectory PowerShell
- lokales CIM / WSMan
- Windows-Autostart
- Diagnosezustand

Es werden dabei keine Domänenrechner gescannt und keine Remote-Einstellungen verändert.

## Persistente Dateien

Konfiguration:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
%AppData%\NetSupportRemoteAdmin\settings.json.bak
```

Lokaler Startverlauf:

```text
%AppData%\NetSupportRemoteAdmin\session-history.json
```

Diagnose:

```text
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

## Erweiterungspunkte

```text
IRemoteProvider
INetSupportInstallationService
INetSupportProfileService
ITargetDiscoveryService
ITargetDetailsService
ISessionHistoryService
IAutoStartService
IDiagnosticLogService
ISupportBundleService
ISystemHealthService
```

Die Provider-Abstraktion bleibt für saubere Architektur erhalten. In dieser Domäne ist aktuell jedoch ausschließlich `NetSupportProvider` freigegeben und registriert.

## Build und automatisierte Tests

Voraussetzungen:

- Windows 10/11
- .NET 8 SDK
- NetSupport Manager Control für praktische Remote-Tests
- optional: RSAT ActiveDirectory PowerShell für Domänensuche
- optional: CIM/WSMan-Zugriff für Rechnerdetails

```powershell
dotnet restore NetSupport.sln
dotnet build NetSupport.sln --configuration Release
dotnet test NetSupport.sln --configuration Release --no-build
```

Die automatisierte Testsuite prüft inzwischen insbesondere:

- rohe NetSupport-CLI-Syntax und alle Aktionsargumente
- Host-/Profilvalidierung, `/F`/`/N` und Fremd-EXE-Schutz
- NetSupport-only-Migration alter Konfigurationen
- Schema-Version und Schutz vor einem unbeabsichtigten Downgrade
- atomisches Speichern sowie Backup-/Recovery-Verhalten
- Anonymisierung des tatsächlich erzeugten Support-ZIPs
- Single-Instance-Sperre
- vorsichtige Statusanzeige für fehlgeschlagenen Ping

Details: [`docs/AUTOMATED_TESTS.md`](docs/AUTOMATED_TESTS.md).

## CI-Testbuild

Erfolgreiche GitHub-Actions-Läufe führen in dieser Reihenfolge aus:

```text
Restore → Build → Test → Publish → Artifact Upload
```

Erst wenn die automatisierten Tests grün sind, wird ein self-contained Windows-x64-Build veröffentlicht:

```text
NetSupport.RemoteAdmin-win-x64
```

Damit kann der aktuelle Stand auf einem Windows-x64-Admin-PC ohne separat installierte .NET-8-Laufzeit praktisch getestet werden.
