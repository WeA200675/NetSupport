# Entwicklungsprotokoll

Diese Datei dokumentiert die wesentlichen Entwicklungsschritte, technischen Entscheidungen und den jeweils erreichten Stand des Projekts **NetSupport Remote Admin**.

---

## 2026-09-14 – Projektstart

Ausgangslage war der Wunsch, die Bedienung von NetSupport bei rund 40 Domänenrechnern deutlich zu vereinfachen. Die NetSupport-Oberfläche und deren benutzer-/domänenabhängige Einstellungen sollten nicht mehr die tägliche Bedienung bestimmen.

Entscheidung:

- eigene Windows-Oberfläche
- .NET 8 + WPF
- NetSupport nicht ersetzen, sondern als Backend verwenden
- Windows RDP zusätzlich als alternatives Backend bereitstellen
- Anwendung soll im Hintergrund im Windows-Infobereich laufen können
- Erweiterbarkeit von Beginn an berücksichtigen

---

## Architekturgrundlage

Die Fernsteuerung wurde hinter `IRemoteProvider` abstrahiert.

Dadurch ist das Hauptfenster nicht direkt an NetSupport oder RDP gekoppelt.

Erste Provider:

- `NetSupportProvider`
- `RdpProvider`

NetSupport wird über `PCICTLUI.EXE` gestartet.

Unterstützte NetSupport-Aktionen:

- Control / Steuern
- View / Nur ansehen
- Chat
- Inventory
- Remote Command Prompt
- File Transfer

Windows RDP wurde zunächst über `mstsc.exe` integriert.

---

## Konfiguration

Die Anwendung verwendet bewusst eine eigene Konfiguration außerhalb von NetSupport-/GPO-Profilen.

Pfad:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

Damit werden die für die eigene Oberfläche relevanten Einstellungen nicht davon abhängig gemacht, ob NetSupport-Benutzereinstellungen zuverlässig über die Domäne übernommen werden.

---

## Tray-Betrieb

Die Anwendung wurde so aufgebaut, dass sie im Hintergrund weiterlaufen kann.

Beim normalen Schließen wird das Hauptfenster ausgeblendet und die Anwendung bleibt im Infobereich aktiv.

Über das Tray-Menü kann die Oberfläche wieder geöffnet oder die Anwendung vollständig beendet werden.

---

## Active Directory

Für die Rechnerauswahl wurde `ITargetDiscoveryService` eingeführt.

Die erste Implementierung ist `DomainComputerDiscoveryService`.

Sie verwendet aktuell:

```powershell
Get-ADComputer
```

Voraussetzung ist deshalb das ActiveDirectory-PowerShell-Modul aus RSAT.

Die Abstraktion ermöglicht zukünftig weitere Rechnerquellen wie CSV, SCCM/MECM, Intune oder eigene Inventardienste.

---

## Online-/Offline-Prüfung

Mit `HostAvailabilityService` wurde eine Erreichbarkeitsprüfung ergänzt.

Die Statusprüfung läuft parallel mit begrenzter Parallelität, damit auch eine größere Anzahl von Rechnern zügig geprüft wird.

Der Status wird nicht dauerhaft gespeichert, sondern nur als Laufzeitinformation verwendet.

---

## Bedienoberfläche – Schnellaktionen

Die Bedienung wurde von einer technischen Provider-/Aktionsauswahl auf eine rechnerbezogene Arbeitsweise umgestellt.

Aktueller Ablauf:

```text
Rechner auswählen
        |
        v
Aktionskarte
        |
        +--> Steuern
        +--> Nur ansehen
        +--> RDP
        +--> CMD
        +--> Dateien
        +--> Inventar
        +--> Chat
```

Die allgemeine Provider-/Aktionsauswahl bleibt unter „Erweitert“ erhalten.

Ein Doppelklick auf einen Zielrechner startet direkt die NetSupport-Steuerung.

---

## CI / Build-Prüfung

Ein Windows-GitHub-Actions-Workflow führt Restore und Release-Build aus.

Durch CI wurden während der Entwicklung mehrere Probleme gefunden und korrigiert:

- Mehrdeutigkeit zwischen WPF- und WinForms-`Application`
- Mehrdeutigkeit von `KeyEventArgs`
- Mehrdeutigkeit von `MessageBox`
- Mehrdeutigkeit von `Button`
- fehlende `System.IO`-Imports
- ungültige Null-Coalescing-Ausdrucksstatements bei `Process.Start`

Die CI-Prüfung bleibt Bestandteil jedes weiteren Entwicklungsschritts.

---

## Eingebettetes RDP – Phase 1

Der ursprüngliche RDP-Provider startete nur `mstsc.exe` als separates Fenster.

Als nächster Schritt wurde ein eigener eingebetteter RDP-Session-Baustein ergänzt.

Technische Grundlage ist Microsofts **Remote Desktop ActiveX Control** in der nicht scriptbaren Variante für Desktop-/Managed-Code-Anwendungen.

Verwendeter Control-Typ:

```text
MsRdpClient12NotSafeForScripting
```

CLSID:

```text
3F859AA3-C2D4-4FAA-B0E4-FD0C9C4E5E3A
```

Neue Bausteine:

```text
Controls/RdpActiveXControl.cs
Views/RdpSessionWindow.xaml
Views/RdpSessionWindow.xaml.cs
Services/IRdpSessionLauncher.cs
Services/EmbeddedRdpSessionLauncher.cs
```

Die ActiveX-Integration ist bewusst gekapselt und verwendet keine generierten `AxInterop.MSTSCLib`-Projektabhängigkeiten.

Das Session-Fenster bietet in Phase 1:

- eingebettete RDP-Darstellung
- Verbinden
- Neu verbinden
- Trennen
- Vollbild des Session-Fensters
- automatischen Fallback auf `mstsc.exe`, falls der eingebettete Weg nicht verwendet werden kann

Die Einstellung:

```json
"useEmbeddedRdp": true
```

aktiviert standardmäßig den eingebetteten RDP-Weg.

---

## Nächste technische Schritte

Für den eingebetteten RDP-Viewer sind als nächste Ausbaustufen vorgesehen:

- zuverlässige Connection-/Disconnect-Events
- bessere Skalierung bei Fenstergrößenänderung
- Credential-/Benutzername-Handling
- Zwischenablageoptionen
- Multi-Monitor-Unterstützung
- RDP-Fehlercodes verständlich anzeigen
- Session-Toolbar weiter ausbauen

Zusätzlich geplant:

- angemeldeten Benutzer eines Rechners anzeigen
- IP-Adresse und Betriebssysteminformationen ergänzen
- Favoriten / Gruppen
- bevorzugten Remote-Provider pro Rechner speichern
- weitere Discovery- und Remote-Provider

---

## Dokumentationsregel

Die Dateien

```text
docs/PROJECT_OVERVIEW.md
docs/DEVELOPMENT_LOG.md
```

werden bei weiteren Entwicklungsschritten mit aktualisiert, damit die im Entwicklungsverlauf besprochenen Informationen direkt im Repository nachvollziehbar bleiben.
