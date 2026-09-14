# Entwicklungsprotokoll

Diese Datei dokumentiert die wesentlichen Entwicklungsschritte, technischen Entscheidungen und den jeweils erreichten Stand des Projekts **NetSupport Remote Admin**.

---

## 2026-09-14 – Projektstart

Ausgangslage war der Wunsch, die Bedienung von NetSupport bei rund 40 Domänenrechnern deutlich zu vereinfachen. Die NetSupport-Oberfläche und deren benutzer-/domänenabhängige Einstellungen sollten nicht mehr die tägliche Bedienung bestimmen.

Grundentscheidungen:

- eigene Windows-Oberfläche mit .NET 8 + WPF
- NetSupport als Backend statt Neuimplementierung des Remote-Protokolls
- Windows RDP als alternatives Backend
- Tray-Betrieb
- Erweiterbarkeit von Beginn an
- eigene Konfiguration unabhängig von NetSupport-/GPO-UI-Profilen

---

## Architekturgrundlage

Die Fernsteuerung wurde hinter `IRemoteProvider` abstrahiert. Rechnerquellen verwenden `ITargetDiscoveryService`. Später kamen `IRdpSessionLauncher` für eingebettetes RDP und `ITargetDetailsService` für flüchtige Rechnerinformationen hinzu.

```text
MainWindow
   +--> IRemoteProvider
   +--> ITargetDiscoveryService
   +--> ITargetDetailsService
   +--> HostAvailabilityService
```

---

## Konfiguration und Tray-Betrieb

Die Anwendungskonfiguration liegt unter:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

Beim normalen Schließen wird das Fenster ausgeblendet und die Anwendung läuft im Infobereich weiter.

---

## Active Directory und Statusprüfung

`DomainComputerDiscoveryService` verwendet `Get-ADComputer` und benötigt deshalb RSAT / das ActiveDirectory-PowerShell-Modul.

`HostAvailabilityService` prüft Rechner parallel per Ping mit begrenzter Parallelität. Der Online-/Offline-Status bleibt Laufzeitinformation.

---

## Bedienoberfläche – Schnellaktionen

Die Bedienung wurde auf einen rechnerbezogenen Ablauf umgestellt. Direkte Aktionen für NetSupport Control/View, RDP, CMD, Dateien, Inventar und Chat bleiben jederzeit sichtbar. Die allgemeine Provider-/Aktionsauswahl bleibt zusätzlich unter **Erweitert** erhalten.

---

## Eingebettetes RDP – Phase 1

Der ursprüngliche RDP-Provider startete nur `mstsc.exe`. Danach wurde `MsRdpClient12NotSafeForScripting` in einem eigenen WPF/WinForms-Host gekapselt.

Phase 1 brachte:

- eingebettete RDP-Darstellung
- Verbinden / Neu verbinden / Trennen
- Vollbild
- `mstsc.exe`-Fallback

---

## Eingebettetes RDP – Phase 2

Ergänzt wurden Session-Ereignisse, Disconnect-Informationen, SmartSizing, Benutzername/Domäne und der normale Windows-Credential-Prompt.

RDP-Passwörter werden nicht gespeichert.

---

## Eingebettetes RDP – Phase 3

Ergänzt wurden:

- Zwischenablageumleitung (`RedirectClipboard`)
- administrative Sitzung (`ConnectToAdministerServer`)
- Remote-App-Switch / Alt+Tab
- Remote-Start-Aktion
- Remote-Task-Manager-Aktion
- Persistenz der nicht geheimen RDP-Präferenzen

---

## Eingebettetes RDP – Phase 4: Auto-Reconnect

Die Anwendung verwendet die Auto-Reconnect-Logik des Microsoft-RDP-Controls statt einer eigenen parallelen Wiederverbindungsschleife.

Verwendete Ereignisse:

- `OnAutoReconnecting2` (`DISPID 34`)
- `OnAutoReconnected` (`DISPID 33`)

Die Oberfläche zeigt Versuchszähler, Netzverfügbarkeit und erfolgreiche Wiederverbindung an.

---

## Eingebettetes RDP – Phase 4: Multi-Monitor

Multi-Monitor wurde als weitere RDP-Präferenz ergänzt.

- eingebettetes RDP setzt `UseMultimon` vor `Connect()`
- `SmartSizing` wird bei Multi-Monitor nicht parallel erzwungen
- Einstellung wird als `rdpUseMultiMonitor` erhalten
- der externe Fallback verwendet `mstsc.exe /multimon`
- Admin + Multi-Monitor können gemeinsam als `/admin /multimon` verwendet werden

---

## Rechnerdetails – Phase 1

Neue Abstraktion:

```text
ITargetDetailsService
   +--> PowerShellTargetDetailsService
```

Die erste Implementierung kombiniert lokale DNS-Auflösung mit Remote-CIM über `Get-CimInstance`.

Angezeigt werden:

- IP-Adresse(n)
- angemeldeter Windows-Benutzer
- Windows-Edition und Version
- Hersteller und Modell
- letzter Status-/Detailprüfzeitpunkt

Die Daten werden bewusst nicht gespeichert, weil sie veralten können. Ein Zeitlimit und partielle Fehlerbehandlung sorgen dafür, dass blockiertes CIM/WSMan die übrige Fernwartung nicht beeinträchtigt.

---

## Rechnerorganisation – Favoriten, Gruppen und Standardverbindung

Für einen Bestand von ungefähr 40 Rechnern wurde die Rechnerliste um eine lokale Organisationsschicht ergänzt.

Neue persistente Zielattribute:

```json
{
  "isFavorite": true,
  "group": "Büro",
  "preferredProviderId": "netsupport"
}
```

### Favoriten

- Favoriten erhalten einen `★`.
- Favoriten werden vor normalen Rechnern sortiert.
- **Nur Favoriten** kann die Liste entsprechend reduzieren.

### Gruppen

- Gruppen sind frei benennbar.
- Textsuche berücksichtigt Gruppen.
- Ein eigener Gruppenfilter wurde ergänzt.
- Neue Gruppen erscheinen nach dem Speichern automatisch im Filter.

### Standardverbindung

- Jeder gespeicherte Rechner kann einen bevorzugten Control-Provider erhalten.
- **Standardverbindung starten** verwendet die aktuelle Provider-Auswahl des Zielrechners.
- Doppelklick verwendet den gespeicherten bevorzugten Provider.
- Ist dieser nicht verfügbar, wird auf NetSupport bzw. einen anderen verfügbaren Control-Provider zurückgefallen.
- Direkte NetSupport-/RDP-Schnellaktionen bleiben erhalten.

### Explizites Speichern

Die frühere **Speichern**-Aktion wurde zu **Speichern / Aktualisieren** erweitert. Bestehende Ziele können damit direkt geändert werden.

Favorit, Gruppe und bevorzugter Provider werden erst durch diese explizite Aktion dauerhaft in die Konfiguration übernommen. Eine reine Verbindung soll diese Organisationswerte nicht nebenbei speichern.

Die Detailbeschreibung liegt in `docs/TARGET_ORGANIZATION.md`.

---

## CI / Build-Prüfung

GitHub Actions führt unter Windows aus:

1. Restore
2. Release-Build
3. self-contained Publish für Windows x64
4. Upload des Testartefakts `NetSupport.RemoteAdmin-win-x64`

CI hat im Verlauf unter anderem WPF/WinForms-Namenskonflikte, fehlende Imports und weitere Compilerprobleme gefunden, die direkt im Entwicklungsbranch behoben wurden.

---

## Nächste technische Schritte

- Auswahl bestimmter Monitor-IDs statt nur aller Monitore
- weitere Tastatur-/Sondertasten-Werkzeuge
- detailliertere RDP-Fehlertexte
- Session-Historie / letzte Verbindung
- weitere Redirects wie Laufwerke oder Audio
- optional gespeicherte Ansichten/Filter
- weitere Discovery-, Details- und Remote-Provider

---

## Dokumentationsregel

Die Dateien

```text
docs/PROJECT_OVERVIEW.md
docs/DEVELOPMENT_LOG.md
docs/RDP_SESSION.md
docs/TARGET_ORGANIZATION.md
docs/TESTING.md
```

werden bei weiteren Entwicklungsschritten mit aktualisiert, damit die im Entwicklungsverlauf besprochenen Informationen direkt im Repository nachvollziehbar bleiben.
