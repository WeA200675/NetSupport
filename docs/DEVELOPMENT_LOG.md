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

Die Bedienung wurde auf einen rechnerbezogenen Ablauf umgestellt:

```text
Rechner auswählen
        |
        +--> Steuern
        +--> Nur ansehen
        +--> RDP
        +--> CMD
        +--> Dateien
        +--> Inventar
        +--> Chat
```

Ein Doppelklick startet NetSupport Control direkt. Die allgemeine Provider-/Aktionsauswahl bleibt unter **Erweitert** erhalten.

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

Ergänzt wurden:

- `OnConnecting`
- `OnConnected`
- `OnLoginComplete`
- `OnDisconnected`
- `OnFatalError`
- `OnRemoteDesktopSizeChange`
- SmartSizing
- Benutzername und Domäne
- Windows-Credential-Prompt

RDP-Passwörter werden nicht gespeichert. Nur nicht geheime Werte wie Benutzername und Domäne dürfen in der lokalen Konfiguration erhalten bleiben.

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

Multi-Monitor wurde als nächste RDP-Präferenz ergänzt.

Technik:

- eingebettetes RDP setzt `UseMultimon` vor `Connect()`
- `SmartSizing` wird bei Multi-Monitor nicht parallel erzwungen
- Einstellung wird pro gespeichertem Ziel als `rdpUseMultiMonitor` erhalten
- der externe Fallback verwendet `mstsc.exe /multimon`
- Admin + Multi-Monitor können im Fallback gemeinsam als `/admin /multimon` verwendet werden

Die Implementierung bleibt defensiv: Falls das lokale ActiveX-Control `UseMultimon` nicht bereitstellt, bleibt eine normale Einzelmonitor-Sitzung möglich.

---

## Rechnerdetails – Phase 1

Die bisherige Rechnerkarte zeigte nur Name, Host und Erreichbarkeit. Für die tägliche Administration wurden zusätzliche Laufzeitinformationen ergänzt.

Neue Abstraktion:

```text
ITargetDetailsService
   +--> PowerShellTargetDetailsService
```

Die erste Implementierung kombiniert:

- lokale DNS-Auflösung für IP-Adressen
- `Get-CimInstance Win32_ComputerSystem`
- `Get-CimInstance Win32_OperatingSystem`

Angezeigt werden:

- IP-Adresse(n)
- aktuell von Windows gemeldeter interaktiver Benutzer
- Windows-Edition und Version
- Hersteller und Modell
- letzter Status-/Detailprüfzeitpunkt

Die Remote-CIM-Abfrage verwendet die aktuelle Windows-Identität und WSMan. Das UI setzt ein Zeitlimit von 12 Sekunden. Ist CIM durch Firewall oder Richtlinie blockiert, bleibt der Fehler lokal auf die Detailanzeige begrenzt; NetSupport, RDP, Ping und DNS funktionieren unabhängig weiter.

Die Daten werden bewusst nicht gespeichert, weil sie veralten können.

---

## CI / Build-Prüfung

GitHub Actions führt unter Windows aus:

1. Restore
2. Release-Build
3. self-contained Publish für Windows x64
4. Upload des Testartefakts `NetSupport.RemoteAdmin-win-x64`

CI hat im Verlauf unter anderem WPF/WinForms-Namenskonflikte und verschiedene Compilerprobleme gefunden, die direkt im Entwicklungsbranch behoben wurden.

---

## Nächste technische Schritte

- Auswahl bestimmter Monitor-IDs statt nur aller Monitore
- weitere Tastatur-/Sondertasten-Werkzeuge
- detailliertere RDP-Fehlertexte
- Session-Historie / letzte Verbindung
- weitere Redirects wie Laufwerke oder Audio
- Favoriten und Rechnergruppen
- bevorzugter Remote-Provider pro Rechner
- weitere Discovery-, Details- und Remote-Provider

---

## Dokumentationsregel

Die Dateien

```text
docs/PROJECT_OVERVIEW.md
docs/DEVELOPMENT_LOG.md
docs/RDP_SESSION.md
docs/TESTING.md
```

werden bei weiteren Entwicklungsschritten mit aktualisiert, damit die im Entwicklungsverlauf besprochenen Informationen direkt im Repository nachvollziehbar bleiben.
