# Entwicklungsprotokoll

Diese Datei hält die wesentlichen Entwicklungsschritte und Architekturentscheidungen von **NetSupport Remote Admin** fest.

> **Aktuell gültige Betriebsregel:** Fernwartung in der Domäne erfolgt ausschließlich über NetSupport Manager. Frühere RDP-Entwicklungsphasen sind durch die spätere Domänenentscheidung aufgehoben und nicht Bestandteil des produktiven Builds.

---

## 2026-09-15 – Bevorzugte NetSupport-Aktion pro Rechner

Gespeicherte Ziele können jetzt zusätzlich enthalten:

```json
"preferredAction": "Control"
```

Unterstützte Werte:

```text
Control
View
Chat
Inventory
CommandPrompt
FileTransfer
```

Bedienlogik:

- die vorhandene Auswahl **Erweitert → Aktion** wird mit dem gespeicherten Wert vorbelegt
- der Standard-Button zeigt die aktuell gewählte NetSupport-Aktion lesbar an
- eine geänderte Aktion kann sofort ausprobiert werden, ohne automatisch gespeichert zu werden
- **Speichern / Aktualisieren** persistiert die Aktion
- Doppelklick verwendet absichtlich die zuletzt gespeicherte Aktion
- fehlende/ungültige Werte fallen auf `Control` zurück

Die zusätzliche Logik ist in `MainWindow.PreferredAction.cs` gekapselt.

Details: [`NETSUPPORT_PREFERRED_ACTIONS.md`](NETSUPPORT_PREFERRED_ACTIONS.md).

---

## 2026-09-15 – Installationserkennung gegen Fremd-EXE gehärtet

Beim Gegenprüfen wurde ein Randfall geschlossen: Eine existierende Datei gilt nicht allein deshalb als gültige NetSupport-Installation.

`NetSupportInstallationCandidate` unterscheidet jetzt:

```text
Exists
IsControlExecutable
IsUsable = Exists && IsControlExecutable
```

Damit gilt eine Installation nur dann als verwendbar, wenn die Datei existiert **und** `PCICTLUI.EXE` heißt.

Diese Definition wird konsistent verwendet in:

- automatischer Erkennung
- **NetSupport prüfen…**
- Einstellungszusammenfassung
- Systemzustand
- Supportpaket
- produktivem `NetSupportProvider`

Ein vorhandenes Fremdprogramm wird nicht mehr als NetSupport-Kandidat bevorzugt oder zur Übernahme freigegeben.

---

## 2026-09-15 – NetSupport-Installationserkennung und lokale Diagnose

Die NetSupport-spezifische Betriebsdiagnose wurde ausgebaut, damit unterschiedliche Admin-PCs mit abweichenden Installationspfaden/Versionen leichter vergleichbar sind.

Neue Schicht:

```text
INetSupportInstallationService
   +--> NetSupportInstallationService
```

Neue Datenstruktur:

```text
NetSupportInstallationCandidate
```

Lokale Erkennungsquellen:

- aktuell konfigurierter Pfad
- Program Files (x86)
- Program Files
- Windows-Uninstall-Registry unter HKLM/HKCU in 32-/64-Bit-Sicht

Bewusst **keine** rekursive Laufwerkssuche.

Neue Einstellungsfunktionen:

- **Automatisch erkennen**
- **NetSupport prüfen…**
- Kandidatenansicht mit Pfad, Quelle, Produktname, Produktversion, Dateiversion und Hersteller
- gültigen Kandidaten über **Pfad übernehmen** auswählen

Der Prüfvorgang ist rein lokal und startet keine Remoteverbindung.

Zusätzlich:

- `ConfigService` verwendet dieselbe Erkennung für neue Benutzerprofile
- `SystemHealthService` unterscheidet gültigen Pfad, veralteten Pfad mit gefundener Alternative und komplett fehlende Installation
- Supportpakete enthalten NetSupport-Produkt-/Dateiversion und Erkennungsstatus
- der vollständige Installationspfad wird in der bereinigten Support-Zusammenfassung nicht zusätzlich benötigt

Details: [`NETSUPPORT_INTEGRATION.md`](NETSUPPORT_INTEGRATION.md), [`SYSTEM_HEALTH.md`](SYSTEM_HEALTH.md) und [`SUPPORT_BUNDLE.md`](SUPPORT_BUNDLE.md).

---

## 2026-09-15 – NetSupport-Provider zusätzlich gegen fremde EXE gehärtet

Neben der Zielwertvalidierung prüft `NetSupportProvider` nun vor **jedem** Prozessstart:

- konfigurierter Pfad ist syntaktisch auflösbar
- Datei existiert
- Dateiname ist exakt `PCICTLUI.EXE`

Damit kann auch eine manuell manipulierte `settings.json` nicht verwendet werden, um über den Remote-Provider ein beliebiges anderes Programm zu starten.

Nach erfolgreichem `Process.Start` wird die PID des gestarteten NetSupport-Prozesses im optionalen Diagnoseprotokoll erfasst.

---

## 2026-09-15 – Domänenrichtlinie geklärt: NetSupport-only

Im weiteren Projektverlauf wurde klargestellt, dass Windows Remote Desktop in der Domäne für die Fernwartung deaktiviert ist und wegen Nachvollziehbarkeit sowie Problemen auf unterschiedlichen PC-Systemen nicht mehr eingesetzt werden darf.

NetSupport Manager wurde gerade deshalb als einheitliches Remote-Control-Werkzeug eingeführt.

Umsetzung:

- im produktiven `RemoteProviderRegistry` wird nur `NetSupportProvider` registriert
- das Hauptfenster blockiert Provider-Starts ungleich `netsupport`
- RDP-Schaltflächen und -Einstellungen wurden entfernt
- RDP-ActiveX, RDP-Provider, RDP-Sessionfenster und `.rdp`-Dateierzeugung wurden aus dem Projekt entfernt
- RDP-Zielfelder wurden aus `RemoteTarget` entfernt
- RDP-Globaleinstellungen wurden aus `AppConfig` entfernt
- alte unbekannte RDP-Felder in `settings.json` werden beim Laden ignoriert und beim nächsten Speichern nicht mehr geschrieben
- alte `preferredProviderId`-Werte werden auf `netsupport` normalisiert
- der Systemzustand prüft die NetSupport-only-Regel anstatt RDP-Komponenten
- Supportpakete dokumentieren `remoteAccessPolicy = NetSupport-only`

Details: [`DOMAIN_REMOTE_POLICY.md`](DOMAIN_REMOTE_POLICY.md).

---

## 2026-09-15 – NetSupport-Startpfad gehärtet

Die Übergabe des Zielrechners an `PCICTLUI.EXE` wurde an die dokumentierte NetSupport-Kommandozeilensyntax angepasst.

Wichtige Punkte:

- IP-Verbindungen verwenden die dokumentierte Form `/c">Adresse"`
- Rechnernamen werden auf DNS-/NetBIOS-artige Zeichen validiert
- Anführungszeichen und Zeilenumbrüche werden nicht zugelassen
- `ProcessStartInfo.Arguments` wird bewusst direkt verwendet, damit .NET die eingebetteten NetSupport-Anführungszeichen nicht erneut escaped
- `UseShellExecute = false`, damit direkt `PCICTLUI.EXE` gestartet wird
- die erzeugte NetSupport-Befehlszeile wird im optionalen Diagnoseprotokoll erfasst

---

## 2026-09-15 – Systemzustand

Neue Schicht:

```text
ISystemHealthService
   +--> SystemHealthService
```

Neue Oberfläche:

```text
SettingsWindow
   +--> SystemHealthWindow
```

Lokale Checks:

- Remotezugriffsrichtlinie = NetSupport-only
- AppData-Verzeichnis beschreibbar
- `PCICTLUI.EXE` / NetSupport-Installation
- ActiveDirectory-PowerShell-Modul / RSAT
- lokale CIM-/WSMan-Grundfunktion
- Windows-Autostart
- Diagnoseprotokoll

Die Prüfung scannt keine Domänenrechner und verändert keine Remote-Systeme.

---

## 2026-09-15 – Betriebsphase: Einstellungen, Autostart und Diagnose

Ergänzt wurden:

```text
IAutoStartService
   +--> WindowsAutoStartService

IDiagnosticLogService
   +--> DiagnosticLogService
```

Funktionen:

- eigenes Einstellungsfenster
- NetSupport-Pfad ohne Neustart ändern
- Autostart nur über `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- Start minimiert
- optionales Diagnoseprotokoll
- Logrotation bei ungefähr 2 MB
- History-CSV-Export
- best-effort Erfassung unbehandelter UI-/Task-/AppDomain-Fehler

Kennwörter, Bildschirm-/Zwischenablageinhalte oder Remote-Dateiinhalte werden nicht protokolliert.

---

## 2026-09-15 – Supportphase: anonymisierbares Diagnosepaket

Neue Schicht:

```text
ISupportBundleService
   +--> SupportBundleService
```

Das Paket enthält:

```text
README.txt
system-info.json
configuration-summary.json
recent-history.json
recent-errors.txt
logs/
```

Designentscheidungen:

- Anonymisierung standardmäßig aktiv
- bekannte Zielhosts/-namen werden durch `target-...` ersetzt
- lokale Rechner-/Benutzer-/Domain-/Profilwerte werden ersetzt
- Gruppen/Ansichten werden abstrahiert
- Original-`settings.json` wird nie in das ZIP kopiert
- Kennwörter/Credentials, Bildschirm-, Zwischenablage- und Remote-Dateiinhalte werden nicht aufgenommen
- `remoteAccessPolicy = NetSupport-only` wird explizit dokumentiert
- temporäre Paketdaten werden best-effort entfernt

---

## 2026-09-15 – Rechnerorganisation und gespeicherte Ansichten

`RemoteTarget` wurde um Favorit, Gruppe und bevorzugten Provider erweitert. Nach der Domänenentscheidung wird `preferredProviderId` ausschließlich auf `netsupport` normalisiert.

Mit `SavedTargetView` können Suchtext, Gruppe und **Nur Favoriten** als benannte Ansichten gespeichert werden.

---

## 2026-09-15 – Lokaler Startverlauf

```text
ISessionHistoryService
   +--> JsonSessionHistoryService
```

Datei:

```text
%AppData%\NetSupportRemoteAdmin\session-history.json
```

Pro NetSupport-Aktionsstart werden Zeitpunkt, Ziel, Provider, Aktion und Start-Erfolg/Fehler erfasst. Der Verlauf ist auf 100 Einträge begrenzt; später wurde ein CSV-Export ergänzt.

Der lokale Verlauf ist eine Bedien-/Fehlersuchhilfe und kein Ersatz für die eigentliche Unternehmens-/NetSupport-Protokollierung einer Fernwartungssitzung.

---

## 2026-09-14 – Active Directory, Status und Rechnerdetails

`DomainComputerDiscoveryService` verwendet `Get-ADComputer` und benötigt RSAT / ActiveDirectory PowerShell.

`HostAvailabilityService` prüft Rechner parallel mit begrenzter Parallelität. Online-/Offline-Status wird nur zur Laufzeit gehalten.

`ITargetDetailsService` / `PowerShellTargetDetailsService` kombinieren DNS und `Get-CimInstance`. Angezeigt werden IP, angemeldeter Benutzer, Windows-Version und Hersteller/Modell. Blockiertes CIM/WSMan darf die NetSupport-Fernwartung nicht beeinträchtigen.

---

## 2026-09-14 – NetSupport-Integration

`NetSupportProvider` startet `PCICTLUI.EXE` und bietet:

- Control
- View
- Chat
- Inventory
- Remote Command Prompt
- File Transfer

---

## 2026-09-14 – Projektstart

Ausgangslage: Die Fernwartung von ungefähr 40 Domänenrechnern soll einfacher werden, ohne von zuverlässig verteilten NetSupport-UI-Einstellungen abhängig zu sein.

Grundentscheidungen:

- .NET 8 + WPF
- eigene kompakte Tray-Oberfläche
- NetSupport Manager als Backend statt Neuimplementierung des Remote-Protokolls
- eigene lokale Bedienkonfiguration
- Erweiterbarkeit über klar getrennte Schnittstellen

### Historischer Hinweis

In einer Zwischenphase wurde RDP als möglicher zweiter Provider technisch untersucht und weitgehend implementiert. Diese Arbeit wurde später vollständig zurückgenommen, nachdem die verbindliche Domänenvorgabe NetSupport-only bekannt war. Sie ist **kein aktuelles Produktmerkmal**.

---

## Aktuelle Architekturgrundlage

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

Produktiv registrierter Remote-Provider:

```text
NetSupportProvider
```

---

## CI / Testbuild

GitHub Actions führt auf Windows aus:

1. Restore
2. Release-Build
3. self-contained Publish für Windows x64
4. Upload von `NetSupport.RemoteAdmin-win-x64`

CI wird nach jedem größeren Block genutzt, um Compiler-/XAML-Probleme im Entwicklungsbranch zu korrigieren.

---

## Dokumentationsregel

Aktuell gepflegte Dokumente:

```text
README.md
docs/PROJECT_OVERVIEW.md
docs/DOMAIN_REMOTE_POLICY.md
docs/NETSUPPORT_INTEGRATION.md
docs/NETSUPPORT_PREFERRED_ACTIONS.md
docs/DEVELOPMENT_LOG.md
docs/TARGET_ORGANIZATION.md
docs/SAVED_VIEWS_AND_HISTORY.md
docs/OPERATIONS.md
docs/SUPPORT_BUNDLE.md
docs/SYSTEM_HEALTH.md
docs/TESTING.md
```

---

## Nächste technische Optionen

- benannte NetSupport-Control-Konfigurationen (`/N`, optional `/F`) bewusst anbinden
- NetSupport-Installationsordner optional auf Begleitdateien prüfen
- NetSupport-Startfehler von späteren Verbindungsfehlern besser unterscheiden
- zusätzliche Domänen-/Rechnermetadaten
- Filter/Zeitraum für Verlauf/CSV
