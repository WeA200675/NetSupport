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

Die Fernsteuerung wurde hinter `IRemoteProvider` abstrahiert. Dadurch ist das Hauptfenster nicht direkt an NetSupport oder RDP gekoppelt.

Erste Provider:

- `NetSupportProvider`
- `RdpProvider`

NetSupport wird über `PCICTLUI.EXE` gestartet und unterstützt Control, View, Chat, Inventory, Remote Command Prompt und File Transfer. Windows RDP wurde zunächst über `mstsc.exe` integriert.

---

## Konfiguration

Die Anwendung verwendet bewusst eine eigene Konfiguration außerhalb von NetSupport-/GPO-Profilen:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

Damit werden die für die eigene Oberfläche relevanten Einstellungen nicht davon abhängig gemacht, ob NetSupport-Benutzereinstellungen zuverlässig über die Domäne übernommen werden.

---

## Tray-Betrieb

Beim normalen Schließen wird das Hauptfenster ausgeblendet und die Anwendung bleibt im Infobereich aktiv. Über das Tray-Menü kann die Oberfläche wieder geöffnet oder die Anwendung vollständig beendet werden.

---

## Active Directory

Für die Rechnerauswahl wurde `ITargetDiscoveryService` eingeführt. Die erste Implementierung `DomainComputerDiscoveryService` verwendet aktuell:

```powershell
Get-ADComputer
```

Voraussetzung ist deshalb das ActiveDirectory-PowerShell-Modul aus RSAT. Die Abstraktion ermöglicht zukünftig weitere Rechnerquellen wie CSV, SCCM/MECM, Intune oder eigene Inventardienste.

---

## Online-/Offline-Prüfung

Mit `HostAvailabilityService` wurde eine Erreichbarkeitsprüfung ergänzt. Die Statusprüfung läuft parallel mit begrenzter Parallelität, damit auch eine größere Anzahl von Rechnern zügig geprüft wird. Der Status wird nicht dauerhaft gespeichert, sondern nur als Laufzeitinformation verwendet.

---

## Bedienoberfläche – Schnellaktionen

Die Bedienung wurde von einer technischen Provider-/Aktionsauswahl auf eine rechnerbezogene Arbeitsweise umgestellt:

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

Die allgemeine Provider-/Aktionsauswahl bleibt unter „Erweitert“ erhalten. Ein Doppelklick auf einen Zielrechner startet direkt die NetSupport-Steuerung.

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

Der Stand mit eingebettetem RDP Phase 1 wurde anschließend erfolgreich unter Windows/.NET 8 gebaut. Phase 3 wurde ebenfalls erfolgreich im Windows-CI gebaut. Die CI-Prüfung bleibt Bestandteil jedes weiteren Entwicklungsschritts.

---

## Eingebettetes RDP – Phase 1

Der ursprüngliche RDP-Provider startete nur `mstsc.exe` als separates Fenster. Danach wurde ein eigener eingebetteter RDP-Session-Baustein ergänzt.

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

Phase 1 brachte:

- eingebettete RDP-Darstellung
- Verbinden / Neu verbinden
- Trennen
- Vollbild des Session-Fensters
- automatischen Fallback auf `mstsc.exe`

Die Einstellung `useEmbeddedRdp` aktiviert standardmäßig den eingebetteten Weg.

---

## Eingebettetes RDP – Phase 2

Phase 2 erweiterte den Viewer von einem reinen ActiveX-Host zu einer beobachtbaren und besser bedienbaren Session.

### Session-Ereignisse

`RdpActiveXControl` bindet ausgewählte Ereignisse aus `IMsTscAxEvents` an .NET-Ereignisse:

- `OnConnecting`
- `OnConnected`
- `OnLoginComplete`
- `OnDisconnected`
- `OnFatalError`
- `OnRemoteDesktopSizeChange`

Bei einer Trennung werden zusätzlich `ExtendedDisconnectReason` und – soweit möglich – `GetErrorDescription` ausgewertet.

### Skalierung

`SmartSizing` wurde gekapselt und kann über **An Fenster anpassen** während einer laufenden Verbindung ein- oder ausgeschaltet werden.

### Benutzername und Domäne

Das Session-Fenster enthält optionale Felder für Benutzername und Windows-/AD-Domäne. Auch `DOMÄNE\Benutzer` wird unterstützt und bei leerem Domänenfeld automatisch aufgeteilt.

### Passwort- und Credential-Entscheidung

RDP-Passwörter werden **nicht** in der Anwendung gespeichert. Das Microsoft-RDP-Control darf den normalen Windows-Credential-Dialog anzeigen; das interne Credential-Saving des eingebetteten Controls ist deaktiviert.

Nicht geheime Werte wie `rdpUserName` und `rdpDomain` können bei gespeicherten Zielrechnern erhalten bleiben.

---

## Eingebettetes RDP – Phase 3

Phase 3 ergänzt Komfortfunktionen, die in der täglichen Administration häufig benötigt werden.

### Zwischenablage

Pro Zielrechner gibt es jetzt die Einstellung:

```json
"rdpRedirectClipboard": true
```

Sie steuert `RedirectClipboard` des RDP-Clients. Änderungen werden beim nächsten Verbindungsaufbau bzw. nach **Neu verbinden** angewendet.

### Administrative Sitzung

Pro Zielrechner kann eine administrative RDP-Sitzung angefordert werden:

```json
"rdpAdminSession": false
```

Technisch wird dafür `ConnectToAdministerServer` gesetzt. Auch diese Einstellung wird vor dem Verbindungsaufbau angewendet und für gespeicherte Ziele erhalten.

### Remote-Aktionen

Über `IMsRdpClient8.SendRemoteAction` stehen nun definierte Remote-Aktionen zur Verfügung:

- Remote-App-Switch / Alt+Tab
- Remote-Start-Aktion
- Remote-Task-Manager-Aktion, sofern vom verwendeten RDP-Client/Server unterstützt

Die Werte sind in `Models/RdpRemoteAction.cs` typisiert gekapselt. Nicht unterstützte Aktionen führen nicht zum Absturz der Sitzung, sondern werden im Sessionstatus gemeldet.

### Persistenz

Für gespeicherte Rechner werden nun folgende nicht geheimen RDP-Präferenzen erhalten:

- `rdpUserName`
- `rdpDomain`
- `rdpRedirectClipboard`
- `rdpAdminSession`

Passwörter bleiben weiterhin vollständig außerhalb der Anwendungskonfiguration.

### Dokumentation

`docs/RDP_SESSION.md` wurde um Phase 3, Clipboard, Admin-Sitzung und Remote-Aktionen erweitert.

---

## Eingebettetes RDP – Phase 4: Auto-Reconnect-Status

Für kurzzeitige Netzwerkunterbrechungen verwendet das Tool jetzt die bereits im Microsoft-RDP-Control vorhandene automatische Wiederverbindung, statt selbst eine zweite Reconnect-Schleife zu implementieren.

Verwendete `IMsTscAxEvents`-Ereignisse:

- `OnAutoReconnecting2` (`DISPID 34`)
- `OnAutoReconnected` (`DISPID 33`)

Während der Wiederverbindung zeigt das Session-Fenster:

- aktuellen Versuch
- maximale Versuchszahl, sofern vom Control gemeldet
- Netzverfügbarkeit
- numerischen Disconnect-Grund

Nach erfolgreicher Wiederverbindung wird der Status auf **Automatisch wieder verbunden** gesetzt.

Dafür wurde `RdpSessionEvents.cs` um `RdpAutoReconnectingEventArgs` erweitert und `RdpActiveXControl` kapselt die zusätzlichen COM-Events weiterhin außerhalb des WPF-Hauptfensters.

---

## Nächste technische Schritte

Als nächste RDP-Ausbaustufe sind insbesondere vorgesehen:

- Multi-Monitor-Unterstützung
- weitere Tastatur-/Sondertasten-Werkzeuge
- detailliertere Fehlertexte
- Session-Historie / letzte Verbindung
- optionale weitere Redirects wie Laufwerke oder Audio

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
docs/RDP_SESSION.md
```

werden bei weiteren Entwicklungsschritten mit aktualisiert, damit die im Entwicklungsverlauf besprochenen Informationen direkt im Repository nachvollziehbar bleiben.
