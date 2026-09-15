# Betrieb, Einstellungen und Diagnose

> Betriebsreferenz für **NetSupport Remote Admin**. In dieser Domäne ist ausschließlich NetSupport Manager als Remotezugriff zugelassen.

## Remotezugriffsrichtlinie

```text
Remotezugriff = NetSupport Manager
```

RDP wird weder angeboten noch als Provider registriert. Details: [`DOMAIN_REMOTE_POLICY.md`](DOMAIN_REMOTE_POLICY.md).

## Einstellungen

Unter **Erweitert → Einstellungen** stehen unter anderem zur Verfügung:

- Mit Windows starten
- beim Start minimiert im Infobereich öffnen
- Diagnoseprotokoll aktivieren/deaktivieren
- Pfad zu `PCICTLUI.EXE`
- NetSupport automatisch erkennen / Installation prüfen
- lokales NetSupport-Control-Profil und optionale `/F`-Bindung
- NetSupport-Client-Port für die Diagnose, Standard TCP 5405
- Systemzustand
- Diagnoseordner
- anonymisierbares Supportpaket

Für diese Optionen ist keine manuelle Bearbeitung von `settings.json` erforderlich.

## NetSupport-Installation

Der produktive Provider akzeptiert ausschließlich eine vorhandene Datei namens `PCICTLUI.EXE`.

Die automatische Erkennung prüft bounded lokale Quellen:

- konfigurierter Pfad
- Program Files / Program Files (x86)
- Windows-Uninstall-Registry in HKLM/HKCU und 32-/64-Bit-Sicht

Es findet keine rekursive Laufwerkssuche statt.

## NetSupport Client-Port und Statusprüfung

Der Diagnose-Port ist konfigurierbar. Standard:

```text
TCP 5405
```

**Status prüfen** führt pro Ziel zwei voneinander unabhängige Hinweise aus:

- Ping/ICMP
- TCP-Verbindung zum konfigurierten NetSupport-Client-Port

Beispiel:

```text
Ping keine Antwort · NetSupport erreichbar
```

Ein fehlgeschlagener Ping beweist nicht, dass ein PC ausgeschaltet ist. Ebenso beweist ein geschlossener TCP-Port nicht allein einen defekten Client; Firewall, Netzweg oder ein abweichend konfigurierter Port können die Ursache sein.

Die Diagnose **blockiert keinen** Start von `PCICTLUI.EXE`.

## Active Directory

**Domäne laden** verwendet bevorzugt RSAT/`Get-ADComputer`. Fehlt das Modul, steht ein read-only LDAP-Fallback über `LDAP://RootDSE` und `DirectorySearcher` zur Verfügung.

Die vollständige Discovery ist auf 30 Sekunden begrenzt und liest nur:

```text
Name
DNSHostName
Description
```

Es werden keine AD-Objekte verändert. Details: [`ACTIVE_DIRECTORY_DISCOVERY.md`](ACTIVE_DIRECTORY_DISCOVERY.md).

## Systemzustand

Die lokale Zustandsprüfung kontrolliert unter anderem:

- NetSupport-only-Richtlinie
- AppData-Schreibbarkeit
- `PCICTLUI.EXE` / Version / alternative Installation
- Control-Profil und `/F`-Konfiguration
- NetSupport-Client-Port als lokalen Konfigurationswert
- RSAT oder LDAP-Fallback
- lokales CIM/WSMan
- Windows-Autostart
- Diagnoseprotokoll

Die Systemzustandsseite scannt keine Zielrechner.

## Diagnoseprotokoll

```text
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

Das optionale Log enthält technische Start-/Fehlerdaten wie Zielhost, Aktion, erzeugte NetSupport-Kommandozeile und gestartete PID. Es rotiert ungefähr bei 2 MB nach `application.log.1`.

Bewusst nicht protokolliert werden Passwörter, gespeicherte Credentials, Bildschirm-/Sitzungsinhalte, Zwischenablage oder Remote-Dateiinhalte.

## Supportpaket

Das ZIP enthält eine bereinigte Konfigurationszusammenfassung, Runtime-Informationen, Verlauf, Fehler und bereinigte Logs. `settings.json` wird nicht kopiert.

Bei aktiver Anonymisierung werden bekannte Ziel-/Benutzer-/Domain-/Gruppen-/Profilwerte ersetzt. Die Zusammenfassung enthält unter anderem den konfigurierten `netSupportClientPort`, führt aber selbst keinen Portscan aus.

Details: [`SUPPORT_BUNDLE.md`](SUPPORT_BUNDLE.md).

## Lokaler Verbindungsverlauf und CSV

```text
%AppData%\NetSupportRemoteAdmin\session-history.json
```

Der Verlauf enthält maximal 100 Startversuche. Der CSV-Export ist UTF-8 mit BOM und Semikolontrennung. Zellen, die von Tabellenkalkulationen als Formel interpretiert werden könnten (`=`, `+`, `-`, `@`), werden als Text neutralisiert.

## Konfiguration, Backup und Recovery

Primärdatei:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

Recovery-Kopie:

```text
%AppData%\NetSupportRemoteAdmin\settings.json.bak
```

Die Konfiguration wird zentral normalisiert und atomisch über eine Temp-Datei geschrieben. Alte RDP-Felder gehören nicht mehr zum Datenmodell und verschwinden beim erneuten Speichern. Beschädigte Primärdateien können automatisch aus dem normalisierten Backup repariert werden.

Eine Konfiguration mit einer höheren Schema-Version als die laufende Anwendung wird bewusst abgewiesen.

Details: [`CONFIGURATION_LIFECYCLE.md`](CONFIGURATION_LIFECYCLE.md).

## Single Instance

Pro Windows-Sitzung läuft nur eine Instanz des Tools. Dadurch werden doppelte Tray-Symbole und konkurrierende lokale Schreibvorgänge vermieden.

## Fehlerdiagnose

Bei einem reproduzierbaren Problem:

1. Commit/Testbuild notieren.
2. **NetSupport prüfen…** und **Systemzustand** öffnen.
3. Bei Verbindungsproblemen konfigurierten Client-Port prüfen.
4. **Status prüfen** ausführen und Ping/NetSupport-Werte getrennt betrachten.
5. Fehler reproduzieren.
6. anonymisiertes Supportpaket erzeugen und vor Weitergabe kurz prüfen.
7. bei AD-Problemen RSAT/LDAP und bei Detailproblemen CIM/WSMan getrennt betrachten.

Keine dieser Diagnosefunktionen verändert GPOs, AD-Objekte, Ziel-Firewall oder NetSupport-Client-Konfigurationen.
