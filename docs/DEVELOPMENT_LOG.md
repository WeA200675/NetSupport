# Entwicklungsprotokoll

Diese Datei hält die wesentlichen Entwicklungsschritte und Architekturentscheidungen von **NetSupport Remote Admin** fest.

> **Aktuell gültige Betriebsregel:** Fernwartung in der Domäne erfolgt ausschließlich über NetSupport Manager. Frühere RDP-Entwicklungsphasen sind durch die spätere Domänenentscheidung aufgehoben und nicht Bestandteil des produktiven Builds.

---

## 2026-09-15 – Domänenrichtlinie geklärt: NetSupport-only

Im weiteren Projektverlauf wurde klargestellt, dass Windows Remote Desktop in der Domäne für die Fernwartung deaktiviert ist und wegen Nachvollziehbarkeit sowie Problemen auf unterschiedlichen PC-Systemen nicht mehr eingesetzt werden darf.

NetSupport Manager wurde gerade deshalb als einheitliches Remote-Control-Werkzeug eingeführt.

Diese Information hat Vorrang vor den früheren technischen RDP-Experimenten.

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

- IP-Verbindungen verwenden die von NetSupport dokumentierte Form `/c">Adresse"`
- Rechnernamen werden vor dem Einsetzen in die rohe Befehlszeile auf DNS-/NetBIOS-artige Zeichen validiert
- Anführungszeichen und Zeilenumbrüche werden nicht zugelassen
- `ProcessStartInfo.Arguments` wird bewusst direkt verwendet, damit .NET die eingebetteten NetSupport-Anführungszeichen nicht erneut escaped
- `UseShellExecute = false`, damit direkt `PCICTLUI.EXE` gestartet wird
- die erzeugte NetSupport-Befehlszeile wird im optionalen Diagnoseprotokoll nachvollziehbar erfasst
- der Systemzustand zeigt zusätzlich die installierte `PCICTLUI.EXE`-Produkt-/Dateiversion, soweit auslesbar

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
- `PCICTLUI.EXE` vorhanden und Version auslesbar
- ActiveDirectory-PowerShell-Modul / RSAT
- lokale CIM-/WSMan-Grundfunktion
- Windows-Autostart
- Diagnoseprotokoll

Die Prüfung scannt keine Domänenrechner und verändert keine Remote-Systeme.

Details: [`SYSTEM_HEALTH.md`](SYSTEM_HEALTH.md).

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

Details: [`OPERATIONS.md`](OPERATIONS.md).

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
- temporäre Paketdaten werden nach ZIP-Erzeugung best-effort entfernt

Details: [`SUPPORT_BUNDLE.md`](SUPPORT_BUNDLE.md).

---

## 2026-09-15 – Rechnerorganisation und gespeicherte Ansichten

`RemoteTarget` wurde um Favorit, Gruppe und bevorzugten Provider erweitert. Nach der Domänenentscheidung wird `preferredProviderId` ausschließlich auf `netsupport` normalisiert.

Mit `SavedTargetView` können Suchtext, Gruppe und **Nur Favoriten** als benannte Ansichten gespeichert werden.

Details: [`TARGET_ORGANIZATION.md`](TARGET_ORGANIZATION.md) und [`SAVED_VIEWS_AND_HISTORY.md`](SAVED_VIEWS_AND_HISTORY.md).

---

## 2026-09-15 – Lokaler Startverlauf

Neue Schicht:

```text
ISessionHistoryService
   +--> JsonSessionHistoryService
```

Datei:

```text
%AppData%\NetSupportRemoteAdmin\session-history.json
```

Pro NetSupport-Aktionsstart werden Zeitpunkt, Ziel, Provider, Aktion und Start-Erfolg/Fehler erfasst. Der Verlauf ist auf 100 Einträge begrenzt und enthält keine Credentials oder Bildschirminhalte. Später wurde ein semikolongetrennter UTF-8/BOM-CSV-Export ergänzt.

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

Die Oberfläche wurde anschließend auf rechnerbezogene Schnellaktionen umgestellt.

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

In einer Zwischenphase wurde RDP als möglicher zweiter Provider technisch untersucht und weitgehend implementiert. Diese Arbeit wurde später vollständig zurückgenommen, nachdem die verbindliche Domänenvorgabe NetSupport-only bekannt war. Sie ist daher **kein aktuelles Produktmerkmal**.

---

## Aktuelle Architekturgrundlage

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

- NetSupport-Version und Installationsdiagnose weiter ausbauen
- NetSupport-spezifische Start-/Fehlerdiagnose
- zusätzliche Domänen-/Rechnermetadaten
- Filter/Zeitraum für Verlauf/CSV
- weitere **freigegebene** Discovery-/Inventarquellen
