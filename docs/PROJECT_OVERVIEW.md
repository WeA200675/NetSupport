# NetSupport Remote Admin – Projektübersicht

## Ziel

**NetSupport Remote Admin** ist eine kompakte Windows/.NET-8-WPF-Anwendung für die tägliche Administration von Domänenrechnern. NetSupport Manager bleibt das einzige freigegebene Remote-Control-Backend.

> **Domänenvorgabe:** RDP ist als Fernwartungsweg deaktiviert und wird von dieser Anwendung weder angeboten noch registriert.

## Bedienablauf

1. Anwendung starten oder aus dem Tray öffnen.
2. Rechner direkt per Name/IP eingeben oder aus Active Directory laden.
3. Liste über Text, Gruppe, Favoriten oder gespeicherte Ansicht filtern.
4. Optional **Status prüfen**: Ping plus NetSupport-TCP-Port.
5. Optional Rechnerdetails über DNS/CIM laden.
6. Favorit, Gruppe und bevorzugte NetSupport-Aktion speichern.
7. Schnellaktion oder gespeicherte Standardaktion starten.
8. Startverlauf einsehen oder gehärtet als CSV exportieren.
9. NetSupport-Pfad, Profil, Client-Port, Diagnose und Autostart unter Einstellungen verwalten.
10. Bei Problemen Systemzustand oder anonymisierbares Supportpaket verwenden.

## NetSupport-only-Architektur

```text
App
  +--> RemoteProviderRegistry
          +--> NetSupportProvider
                  +--> NetSupportCommandLine
                  +--> PCICTLUI.EXE
```

Der produktive Startpfad registriert ausschließlich Provider-ID `netsupport`. Alte RDP-Felder und alte Providerpräferenzen werden durch die zentrale Konfigurationsmigration bereinigt.

## NetSupport-Aktionen

Unterstützt werden:

```text
Control
View
Chat
Inventory
CommandPrompt
FileTransfer
```

`PCICTLUI.EXE` wird als einzig erlaubte Remote-Executable validiert. Zielwerte und optionale Control-Profilnamen werden vor der rohen Kommandozeile geprüft.

## Control-Profile

Vorhandene Profile werden read-only gelesen aus:

```text
HKCU\Software\NetSupport Ltd\PCICTL\ConfigList
```

Optional wird `/N "Profil"` verwendet, mit fester Bindung zusätzlich `/F`. Ein konfiguriertes, aber fehlendes Profil blockiert den Remote-Start bewusst; Profilpasswörter werden nicht vom Tool gespeichert.

## Bevorzugte Aktion pro Rechner

Gespeicherte Ziele können eine `preferredAction` aus den sechs unterstützten NetSupport-Aktionen enthalten. Doppelklick verwendet bewusst den zuletzt **gespeicherten** Wert. Fehlende oder ungültige Legacy-Werte fallen auf `Control` zurück.

## Zielorganisation

Persistierbar sind unter anderem:

```json
{
  "name": "PC-001",
  "host": "PC-001.example.local",
  "description": "Büro 1",
  "isFavorite": true,
  "group": "Büro",
  "preferredProviderId": "netsupport",
  "preferredAction": "Control"
}
```

Dazu kommen Favoritenfilter, Gruppenfilter, Textsuche und gespeicherte Ansichten.

## Active Directory

```text
ITargetDiscoveryService
  +--> DomainComputerDiscoveryService
          +--> Get-ADComputer (bevorzugt)
          +--> LDAP / DirectorySearcher (Fallback)
```

RSAT ist **nicht mehr zwingend erforderlich**. Fehlt `Get-ADComputer`, versucht die Anwendung read-only über `LDAP://RootDSE` den `defaultNamingContext` zu ermitteln und Computerobjekte per `DirectorySearcher` zu lesen.

Gelesen werden nur `Name`, `DNSHostName` und `Description`. Doppelte Hosts werden zusammengeführt. Die vollständige Discovery ist auf 30 Sekunden begrenzt.

Details: [`ACTIVE_DIRECTORY_DISCOVERY.md`](ACTIVE_DIRECTORY_DISCOVERY.md).

## Erreichbarkeitsdiagnose

Die Statusprüfung kombiniert:

```text
HostAvailabilityService        -> Ping/ICMP
NetSupportReachabilityService  -> TCP <NetSupportClientPort>
```

Standardport ist TCP 5405, kann aber konfiguriert werden.

Ein Ergebnis wie

```text
Ping keine Antwort · NetSupport erreichbar
```

ist ausdrücklich zulässig. Weder Ping- noch Porttest blockieren den eigentlichen NetSupport-Start.

## Rechnerdetails

```text
ITargetDetailsService
  +--> PowerShellTargetDetailsService
          +--> DNS
          +--> CIM Win32_ComputerSystem
          +--> CIM Win32_OperatingSystem
```

CIM/WSMan ist optional. Fehler bei Rechnerdetails dürfen die NetSupport-Funktionen nicht blockieren.

## Persistenz und Recovery

```text
ConfigService
  +--> ConfigNormalizer
  +--> settings.json
  +--> settings.json.bak
```

Die Konfiguration ist schema-versioniert, wird zentral auf NetSupport-only normalisiert und atomisch über Temp-Dateien geschrieben. Bei beschädigter Primärdatei kann ein gültiges Backup automatisch wiederherstellen. Eine neuere, nicht unterstützte Schema-Version wird bewusst abgewiesen.

## Verlauf und CSV

`JsonSessionHistoryService` hält maximal 100 lokale Startversuche in `session-history.json`. Der CSV-Export ist UTF-8/BOM, semikolongetrennt und neutralisiert potentiell als Tabellenkalkulationsformeln interpretierbare Zellen.

## Single Instance

Ein benutzersitzungsbezogener Mutex verhindert parallele produktive Instanzen mit doppelten Tray-Symbolen und konkurrierenden lokalen Schreibvorgängen.

## Diagnose und Support

- rotierendes lokales Diagnoseprotokoll
- lokale Systemzustandsprüfung ohne Zielscan
- anonymisierbares Support-ZIP
- Support-Zusammenfassung enthält unter anderem NetSupport-Installation/Version, Profilstatus und konfigurierten Client-Port
- originale `settings.json`, Credentials und Sitzungsinhalte werden nicht bewusst aufgenommen

## Systemzustand

Geprüft werden lokal unter anderem:

- NetSupport-only-Richtlinie
- AppData-Schreibbarkeit
- `PCICTLUI.EXE` und Versionsdaten
- Control-Profil und `/F`
- konfigurierter NetSupport-Client-Port
- RSAT oder LDAP-Fallback
- lokales CIM/WSMan
- Autostart und Diagnosezustand

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

Die Architektur bleibt erweiterbar, aber ein weiterer Remote-Provider darf in dieser Umgebung nur nach ausdrücklicher Sicherheitsfreigabe registriert werden.

## Automatisierte Tests und CI

Die Tests decken unter anderem ab:

- NetSupport-CLI und Injection-Schutz
- Profile `/N` und `/F`
- Fremd-EXE-Schutz
- Schema/Migration/Backup/Recovery
- Ping-/NetSupport-Statuskombinationen
- TCP-Portvalidierung und lokalen Listener
- AD-Discovery-Parsing/Deduplizierung
- Support-ZIP-Anonymisierung und Client-Port
- CSV-Formula-Injection
- Single Instance

CI:

```text
Restore → Build → Test → Publish → Artifact Upload
```

Ein roter Build oder Test verhindert die Veröffentlichung des self-contained Windows-x64-Testartefakts.
