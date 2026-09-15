# Entwicklungsprotokoll

Diese Datei hält die wesentlichen Entwicklungsschritte und Architekturentscheidungen von **NetSupport Remote Admin** fest.

---

## 2026-09-14 – Projektstart

Ausgangslage: Die Fernwartung von ungefähr 40 Domänenrechnern soll einfacher werden, ohne von zuverlässig verteilten NetSupport-UI-Einstellungen abhängig zu sein.

Grundentscheidungen:

- .NET 8 + WPF
- eigene kompakte Tray-Oberfläche
- NetSupport Manager als Backend statt Neuimplementierung des Remote-Protokolls
- Windows RDP als zweiter Provider
- eigene lokale Bedienkonfiguration
- Erweiterbarkeit über klar getrennte Schnittstellen

---

## Architekturgrundlage

Im Verlauf wurden folgende Erweiterungspunkte eingeführt:

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

Dadurch bleiben Hauptfenster, Remote-Technologien, Discovery, Inventardaten, Verlauf, Betrieb, Diagnose und Supportpaketerzeugung voneinander getrennt.

---

## NetSupport-Integration

`NetSupportProvider` startet `PCICTLUI.EXE` und bietet:

- Control
- View
- Chat
- Inventory
- Remote Command Prompt
- File Transfer

Die Oberfläche wurde anschließend auf rechnerbezogene Schnellaktionen umgestellt.

---

## Active Directory und Status

`DomainComputerDiscoveryService` verwendet `Get-ADComputer` und benötigt RSAT / ActiveDirectory PowerShell.

`HostAvailabilityService` prüft Rechner parallel mit begrenzter Parallelität. Online-/Offline-Status wird nur zur Laufzeit gehalten.

---

## Rechnerdetails

`ITargetDetailsService` wurde ergänzt.

Aktuelle Implementierung:

```text
PowerShellTargetDetailsService
   +--> DNS
   +--> Get-CimInstance
```

Angezeigt werden IP, angemeldeter Benutzer, Windows-Version und Hersteller/Modell. Blockiertes CIM/WSMan darf andere Remote-Funktionen nicht beeinträchtigen.

---

## Rechnerorganisation

`RemoteTarget` wurde um lokale Bedienattribute erweitert:

```json
{
  "isFavorite": true,
  "group": "Büro",
  "preferredProviderId": "netsupport"
}
```

Ergänzt wurden:

- Favoritenmarkierung und -filter
- freie Gruppen
- Gruppenfilter
- Standard-Provider pro Ziel
- Fallback auf verfügbaren Control-Provider
- **Speichern / Aktualisieren** für bestehende Ziele

Organisationsänderungen werden bewusst erst durch explizites Speichern dauerhaft.

---

## Gespeicherte Ansichten

Mit `SavedTargetView` können wiederkehrende Filterzustände gespeichert werden:

- Suchtext
- Gruppe
- Nur Favoriten

Ansichten werden in `settings.json` gespeichert und über ein editierbares Kombinationsfeld ausgewählt/aktualisiert/gelöscht.

---

## Lokaler Startverlauf

Neue Schicht:

```text
ISessionHistoryService
   +--> JsonSessionHistoryService
```

Datei:

```text
%AppData%\NetSupportRemoteAdmin\session-history.json
```

Pro Remote-Aktion werden Zeitpunkt, Ziel, Provider, Aktion und Start-Erfolg/Fehler erfasst. Der Verlauf ist auf 100 Einträge begrenzt und enthält keine Credentials oder Bildschirminhalte.

Bei NetSupport handelt es sich bewusst um einen Provider-Startverlauf und nicht um ein revisionssicheres Session-Audit.

Später wurde ein semikolongetrennter UTF-8/BOM-CSV-Export ergänzt.

---

## Eingebettetes RDP – Phase 1

`MsRdpClient12NotSafeForScripting` wurde über einen eigenen `AxHost` eingebettet.

Erste Funktionen:

- Verbinden / Neu verbinden / Trennen
- Vollbild
- `mstsc.exe`-Fallback

---

## RDP – Phase 2

Ergänzt:

- Connecting / Connected / Login / Disconnect Events
- Extended Disconnect Reason
- Fatal Error
- Remote-Auflösung
- SmartSizing
- Benutzername/Domäne
- normaler Windows-Credential-Prompt
- keine Passwortspeicherung

---

## RDP – Phase 3

Ergänzt:

- Zwischenablage
- Admin-Sitzung
- Remote Alt+Tab / Start / Task-Manager
- Persistenz nicht geheimer RDP-Präferenzen

---

## RDP – Phase 4

Ergänzt:

- `OnAutoReconnecting2`
- `OnAutoReconnected`
- Auto-Reconnect-Versuchszähler und Netzstatus
- Multi-Monitor über `UseMultimon`
- externer `/multimon`-Fallback

---

## RDP – Phase 5: gezielte Monitorwahl

Für Admin-Arbeitsplätze mit mehreren Displays wurde eine Auswahl bestimmter lokaler RDP-Monitore ergänzt.

Neues persistentes Zielfeld:

```json
"rdpSelectedMonitors": "0,1"
```

Neue Schicht:

```text
IRdpConnectionFileService
   +--> RdpConnectionFileService
```

### IDs ermitteln

Die UI-Schaltfläche **IDs anzeigen** startet:

```text
mstsc.exe /l
```

### Validierung

Monitorlisten werden normalisiert:

- nur nichtnegative Ganzzahlen
- Kommatrennung
- Leerzeichen entfernt
- Duplikate entfernt

### Verbindung

Ist keine ID-Liste gespeichert, bleibt der bisherige eingebettete RDP-Weg unverändert.

Ist eine ID-Liste vorhanden, erzeugt der Service eine minimale `.rdp`-Datei mit:

```text
use multimon:i:1
selectedmonitors:s:0,1
```

und startet `mstsc.exe` mit dieser Datei.

Die Dateien liegen unter:

```text
%AppData%\NetSupportRemoteAdmin\rdp\
```

Sie enthalten keine Passwörter. Der Zielwert wird gegen Zeilenumbrüche geprüft, und der Dateiname wird aus einem Hash des Hosts erzeugt.

Aktuelle Microsoft-Dokumentation führt `SelectedMonitors` zusätzlich als benannte Eigenschaft von `IMsRdpExtendedSettings`. Die bestehende ActiveX-Kapselung verzichtet bewusst auf generierte MSTSCLib-Interop-Assemblies; deshalb wurde für diese Phase der transparente `.rdp`-Dateipfad gewählt.

Details: `docs/RDP_SELECTED_MONITORS.md`.

---

## Betriebsphase: Einstellungen, Autostart und Diagnose

Ergänzt wurden:

```text
IAutoStartService
   +--> WindowsAutoStartService

IDiagnosticLogService
   +--> DiagnosticLogService
```

Neue Funktionen:

- eigenes Einstellungsfenster
- NetSupport-Pfad ohne Neustart ändern
- Autostart nur über `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- Start minimiert
- Embedded-RDP-/Vollbildoptionen
- optionales Diagnoseprotokoll
- Logrotation bei ungefähr 2 MB
- History-CSV-Export
- best-effort Erfassung unbehandelter UI-/Task-/AppDomain-Fehler

Passwörter, RDP-Credentials, Bildschirm-/Zwischenablageinhalte oder Remote-Dateiinhalte werden nicht protokolliert.

Details: `docs/OPERATIONS.md`.

---

## Supportphase: anonymisierbares Diagnosepaket

Neue Schicht:

```text
ISupportBundleService
   +--> SupportBundleService
```

Ziel war ein Support-ZIP, das für Fehlersuche brauchbar ist, aber nicht einfach komplette lokale Konfigurationsdateien kopiert.

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
- bekannte RDP-Benutzer-/Domainwerte werden beim Bereinigen von Logs ebenfalls ersetzt
- Gruppen/Ansichten werden in der Konfigurationsübersicht abstrahiert
- RDP-Benutzername/Domain erscheinen dort nur als `konfiguriert: ja/nein`
- Original-`settings.json` wird nie in das ZIP kopiert
- Passwörter/Credentials, Bildschirm-, Zwischenablage- und Remote-Dateiinhalte werden nicht aufgenommen
- temporäre Paketdaten werden nach ZIP-Erzeugung best-effort entfernt

Die UI liegt unter **Erweitert → Einstellungen → Diagnose und Support**.

Details: `docs/SUPPORT_BUNDLE.md`.

---

## CI / Testbuild

GitHub Actions führt auf Windows aus:

1. Restore
2. Release-Build
3. self-contained Publish für Windows x64
4. Upload von `NetSupport.RemoteAdmin-win-x64`

CI wird nach jedem größeren Block genutzt, um Compiler-/Interop-/XAML-Probleme sofort im Entwicklungsbranch zu korrigieren.

---

## Dokumentationsregel

Aktuell gepflegte Dokumente:

```text
README.md
docs/PROJECT_OVERVIEW.md
docs/DEVELOPMENT_LOG.md
docs/RDP_SESSION.md
docs/RDP_SELECTED_MONITORS.md
docs/TARGET_ORGANIZATION.md
docs/SAVED_VIEWS_AND_HISTORY.md
docs/OPERATIONS.md
docs/SUPPORT_BUNDLE.md
docs/TESTING.md
```

---

## Nächste technische Optionen

- `IMsRdpExtendedSettings.SelectedMonitors` typisiert für eingebettetes RDP anbinden
- weitere RDP-Redirects (Audio/Laufwerke)
- weitere Tastatur-/Sondertastenaktionen
- detailliertere Fehlertexte
- Filter/Zeitraum für Verlauf/CSV
- weitere Discovery-, Details-, History-, Support- und Remote-Provider
