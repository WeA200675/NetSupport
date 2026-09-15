# NetSupport Remote Admin

Eine erweiterbare .NET-8/WPF-Anwendung für die tägliche Fernwartung von Windows-/Domänenrechnern über **NetSupport Manager**.

> **Domänenvorgabe:** Remotezugriff erfolgt ausschließlich über NetSupport Manager. RDP wird von dieser Anwendung weder angeboten noch als Provider registriert.

## Funktionsumfang

- Tray-/Infobereich-Betrieb und Single-Instance-Schutz pro Windows-Sitzung
- direkte Zielwahl per Rechnername oder IP
- Active-Directory-Rechnersuche: bevorzugt RSAT/`Get-ADComputer`, read-only LDAP-Fallback ohne zwingendes RSAT
- Text-, Gruppen- und Favoritenfilter sowie gespeicherte Ansichten
- bevorzugte NetSupport-Aktion pro gespeichertem Rechner
- Schnellaktionen: **Steuern**, **Nur ansehen**, **Chat**, **Inventar**, **Remote CMD**, **Dateiübertragung**
- parallele Ping- und NetSupport-TCP-Erreichbarkeitsdiagnose
- Einzelziel-Diagnose mit DNS/IP, Ping, NetSupport-Port und kopierbarem Kurzbericht
- konfigurierbarer NetSupport-Client-Port, Standard TCP 5405
- zusätzliche Rechnerdetails über DNS + CIM/WSMan
- lokaler Startverlauf, maximal 100 Einträge, plus gehärteter CSV-Export
- Windows-Autostart pro Benutzer
- optionales rotierendes Diagnoseprotokoll
- anonymisierbares Supportpaket
- lokale Systemzustandsprüfung
- schema-versionierte, atomisch geschriebene Konfiguration mit Backup/Recovery
- automatisierte Windows-CI-Tests vor jedem veröffentlichten Testartefakt

## NetSupport-only

Der produktive Startpfad registriert nur:

```text
netsupport
```

Frühere RDP-Felder aus alten Entwicklungsständen werden beim Laden ignoriert und beim erneuten Speichern nicht mehr geschrieben. Alte Providerpräferenzen werden zentral auf `netsupport` normalisiert.

## NetSupport Manager

Als Remote-Backend darf ausschließlich eine vorhandene `PCICTLUI.EXE` gestartet werden.

Der Startpfad validiert unter anderem:

- Rechnername/IP vor `Process.Start`
- exakte NetSupport-IP-Syntax
- alle unterstützten Aktionsargumente
- optionales lokales Control-Profil über `/N`
- optionale feste Profilbindung über `/F /N`
- Profilname und Vorhandensein des Profils für den aktuellen Windows-Benutzer
- Executable-Pfad und Dateiname

Ein fehlendes konfiguriertes Profil blockiert den Start bewusst, statt stillschweigend ein anderes NetSupport-Profil zu verwenden. Profilpasswörter bleiben vollständig innerhalb NetSupport.

## Erreichbarkeitsdiagnose

**Status prüfen** kombiniert für die Rechnerliste zwei unabhängige Hinweise:

```text
Ping/ICMP
NetSupport TCP <konfigurierter Port>
```

Beispiel:

```text
Ping keine Antwort · NetSupport erreichbar
```

Dadurch wird ein ICMP-Block nicht fälschlich als ausgeschalteter PC dargestellt. Ein fehlgeschlagener Portcheck ist ebenfalls nur Diagnose und blockiert **keinen** Start von `PCICTLUI.EXE`.

Für einen einzelnen Rechner gibt es zusätzlich **Verbindung diagnostizieren**. Diese read-only Prüfung zeigt DNS/IP-Auflösung, Ping und den konfigurierten NetSupport-Port inklusive Laufzeiten in einem eigenen Dialog. Ein kurzer Bericht kann bewusst in die Zwischenablage kopiert werden; die Funktion startet keine Remote-Sitzung.

Details: [`docs/TARGET_CONNECTION_DIAGNOSTICS.md`](docs/TARGET_CONNECTION_DIAGNOSTICS.md).

## Active Directory

**Domäne laden** nutzt:

1. `Get-ADComputer`, wenn RSAT/ActiveDirectory PowerShell vorhanden ist.
2. Sonst einen read-only LDAP-Fallback über `LDAP://RootDSE` und `System.DirectoryServices.DirectorySearcher`.

Gelesen werden nur `Name`, `DNSHostName` und `Description`. Die Abfrage ist auf 30 Sekunden begrenzt und verändert keine AD-Objekte oder Gruppenrichtlinien.

Details: [`docs/ACTIVE_DIRECTORY_DISCOVERY.md`](docs/ACTIVE_DIRECTORY_DISCOVERY.md).

## Persistenz

```text
%AppData%\NetSupportRemoteAdmin\settings.json
%AppData%\NetSupportRemoteAdmin\settings.json.bak
%AppData%\NetSupportRemoteAdmin\session-history.json
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

Die Konfiguration wird normalisiert, schema-versioniert und atomisch gespeichert. Bei einer beschädigten Primärdatei kann aus dem normalisierten Backup repariert werden. Eine Konfiguration aus einer neueren Schema-Version wird von einer älteren Anwendung bewusst nicht überschrieben.

## Datenschutz und Diagnose

Das Supportpaket enthält keine originale `settings.json`, keine bewusst gespeicherten Credentials und keine Sitzungs-/Bildschirminhalte. Bei aktiver Anonymisierung werden bekannte Ziel-, Benutzer-, Domain-, Gruppen- und Profilnamen ersetzt.

Der konfigurierte NetSupport-Client-Port wird als Diagnosewert aufgenommen; das Paket führt selbst keinen Portscan aus.

Der CSV-Verlauf neutralisiert außerdem Werte, die Tabellenkalkulationen als Formel interpretieren könnten.

## Build

Voraussetzungen für Entwicklung:

- Windows
- .NET 8 SDK
- für praktische Remote-Tests: NetSupport Manager Control
- optional RSAT; ohne RSAT kann in einer passenden Domänenumgebung der LDAP-Fallback verwendet werden
- optional CIM/WSMan für zusätzliche Rechnerdetails

```powershell
dotnet restore NetSupport.sln
dotnet build NetSupport.sln --configuration Release
dotnet test NetSupport.sln --configuration Release --no-build
```

CI-Reihenfolge:

```text
Restore → Build → Test → Publish → Artifact Upload
```

Der veröffentlichte Testbuild heißt:

```text
NetSupport.RemoteAdmin-win-x64
```

## Dokumentation

- [`docs/PROJECT_OVERVIEW.md`](docs/PROJECT_OVERVIEW.md)
- [`docs/DOMAIN_REMOTE_POLICY.md`](docs/DOMAIN_REMOTE_POLICY.md)
- [`docs/NETSUPPORT_INTEGRATION.md`](docs/NETSUPPORT_INTEGRATION.md)
- [`docs/NETSUPPORT_CONTROL_PROFILES.md`](docs/NETSUPPORT_CONTROL_PROFILES.md)
- [`docs/NETSUPPORT_PREFERRED_ACTIONS.md`](docs/NETSUPPORT_PREFERRED_ACTIONS.md)
- [`docs/ACTIVE_DIRECTORY_DISCOVERY.md`](docs/ACTIVE_DIRECTORY_DISCOVERY.md)
- [`docs/TARGET_CONNECTION_DIAGNOSTICS.md`](docs/TARGET_CONNECTION_DIAGNOSTICS.md)
- [`docs/CONFIGURATION_LIFECYCLE.md`](docs/CONFIGURATION_LIFECYCLE.md)
- [`docs/AUTOMATED_TESTS.md`](docs/AUTOMATED_TESTS.md)
- [`docs/TARGET_ORGANIZATION.md`](docs/TARGET_ORGANIZATION.md)
- [`docs/SAVED_VIEWS_AND_HISTORY.md`](docs/SAVED_VIEWS_AND_HISTORY.md)
- [`docs/OPERATIONS.md`](docs/OPERATIONS.md)
- [`docs/SUPPORT_BUNDLE.md`](docs/SUPPORT_BUNDLE.md)
- [`docs/SYSTEM_HEALTH.md`](docs/SYSTEM_HEALTH.md)
- [`docs/TESTING.md`](docs/TESTING.md)
- [`docs/DEVELOPMENT_LOG.md`](docs/DEVELOPMENT_LOG.md)
