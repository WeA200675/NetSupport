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
```

Dadurch bleiben Hauptfenster, Remote-Technologien, Discovery, Inventardaten, Verlauf und RDP-Dateierzeugung voneinander getrennt.

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

### Microsoft-Dokumentation

Während dieser Phase wurde geprüft, dass aktuelle Microsoft-Dokumentation `SelectedMonitors` inzwischen auch als benannte Eigenschaft von `IMsRdpExtendedSettings` aufführt.

Die bestehende ActiveX-Kapselung verzichtet bewusst auf generierte MSTSCLib-Interop-Assemblies. Für diese Phase wurde deshalb der transparente `.rdp`-Dateipfad gewählt. Eine spätere typisierte Anbindung von `IMsRdpExtendedSettings.SelectedMonitors` kann die gezielte Auswahl zusätzlich in den eingebetteten Viewer bringen.

Details: `docs/RDP_SELECTED_MONITORS.md`.

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
docs/TESTING.md
```

---

## Nächste technische Optionen

- `IMsRdpExtendedSettings.SelectedMonitors` typisiert für eingebettetes RDP anbinden
- weitere RDP-Redirects (Audio/Laufwerke)
- weitere Tastatur-/Sondertastenaktionen
- detailliertere Fehlertexte
- Export/Filter des Startverlaufs
- weitere Discovery-, Details-, History- und Remote-Provider
