# Testen von NetSupport Remote Admin

> Kurzanleitung zum Testen eines Entwicklungsstands ohne lokale Visual-Studio-Umgebung.

---

## Automatischer Windows-Testbuild

Erfolgreiche GitHub-Actions-Läufe erzeugen einen self-contained Windows-x64-Testbuild:

```text
NetSupport.RemoteAdmin-win-x64
```

Der Build enthält die benötigte .NET-Laufzeit.

Download:

1. GitHub → **Actions**.
2. Erfolgreichen Workflow **Build** öffnen.
3. Unter **Artifacts** `NetSupport.RemoteAdmin-win-x64` herunterladen.
4. ZIP entpacken.
5. `NetSupport.RemoteAdmin.exe` starten.

---

## Voraussetzungen

### Grundanwendung

- Windows 10/11, 64 Bit

### NetSupport

- NetSupport Manager Control auf dem Admin-PC
- `PCICTLUI.EXE` in einem üblichen Installationspfad oder explizit konfiguriert

### Active Directory

Für **Domäne laden**:

```text
RSAT / ActiveDirectory PowerShell
```

### Rechnerdetails

Für vollständige Remote-Details müssen CIM/WSMan und die entsprechenden Berechtigungen/Firewallregeln funktionieren. Ein fehlender CIM-Zugriff darf die übrige Anwendung nicht blockieren.

---

## 1. Programmstart und Tray

Prüfen:

- Anwendung startet ohne Fehler.
- Fenster schließen → Anwendung bleibt im Tray.
- Tray-Doppelklick öffnet das Fenster.
- Tray-Menü **Beenden** beendet die Anwendung vollständig.

---

## 2. Rechnerliste und Status

Prüfen:

- Name/IP direkt eingeben.
- **Domäne laden** funktioniert mit RSAT.
- Textfilter funktioniert.
- Gruppenfilter funktioniert.
- **Nur Favoriten** funktioniert.
- **Status prüfen** setzt Online/Offline und Prüfzeitpunkt.

---

## 3. Favoriten, Gruppen und Standardverbindung

Testziel auswählen:

1. Favorit aktivieren.
2. Gruppe `Testgruppe` setzen.
3. Standard-Provider wählen.
4. **Speichern / Aktualisieren**.

Prüfen:

- `★` erscheint.
- Favorit wird zuerst sortiert.
- Gruppe erscheint im Filter.
- Suche findet `Testgruppe`.
- Neustart erhält Werte.
- **Standardverbindung starten** nutzt den gewählten Provider.
- Doppelklick nutzt den bevorzugten Provider.
- direkte **Steuern**-/RDP-Aktionen bleiben unabhängig davon verfügbar.

Nicht gespeicherte Änderungen dürfen durch einen reinen Verbindungsstart nicht dauerhaft werden.

---

## 4. Gespeicherte Ansichten

Beispiel:

- Gruppe `Testgruppe`
- **Nur Favoriten** aktiv
- Ansichtname `Testansicht`

Dann **Ansicht speichern**.

Prüfen:

- Filter verändern und `Testansicht` erneut auswählen → Filter werden wiederhergestellt.
- erneutes Speichern unter gleichem Namen aktualisiert statt zu duplizieren.
- Neustart erhält die Ansicht.
- **Ansicht löschen** entfernt nur die Ansicht.

---

## 5. Rechnerdetails

Mit einem erreichbaren Rechner **Rechnerdetails laden**:

Erwartet, soweit CIM/WSMan erlaubt:

- IP
- angemeldeter Benutzer
- Windows-Edition/-Version
- Hersteller/Modell
- Zeitstempel

Mit blockiertem CIM testen:

- Anwendung bleibt bedienbar.
- Abfrage endet durch Zeitlimit.
- NetSupport, RDP, DNS und Ping bleiben nutzbar.

---

## 6. NetSupport-Aktionen

Prüfen:

- Steuern
- Nur ansehen
- Chat
- Inventar
- Remote CMD
- Dateien

---

## 7. Verbindungsverlauf

Mehrere Aktionen starten.

Prüfen:

- Einträge erscheinen unter **Zuletzt verwendet**.
- Zeit, Ziel, Provider und Aktion stimmen.
- **Letzter Start** in der Rechnerkarte wird aktualisiert.
- Doppelklick auf History übernimmt nur das Ziel; keine automatische Verbindung.
- Fehlerhafter Providerstart wird als Fehler protokolliert.
- `session-history.json` enthält keine Passwörter/Credentials.
- **Verlauf löschen** löscht nur den Verlauf.
- Datei bleibt auf maximal 100 Einträge begrenzt.

### CSV-Export

1. Mehrere Verlaufszeilen erzeugen.
2. **CSV exportieren** anklicken.
3. Datei speichern und in Excel/LibreOffice/Texteditor öffnen.

Prüfen:

- Spalten `Zeitpunkt`, `Rechner`, `Name`, `Provider`, `Provider-ID`, `Aktion`, `Erfolg`, `Fehler` sind vorhanden.
- Datei ist semikolongetrennt.
- Umlaute werden korrekt dargestellt.
- Fehlermeldungen mit Sonderzeichen beschädigen die Spalten nicht.
- Export enthält keine Passwörter oder RDP-Credentials.

---

## 8. Eingebettetes RDP

Ohne `rdpSelectedMonitors` prüfen:

- eingebettetes Fenster öffnet sich.
- Credential-Prompt erscheint bei Bedarf.
- Connecting / Connected / Login werden angezeigt.
- SmartSizing funktioniert.
- Vollbild/Fenstermodus funktioniert.
- Trennen/Neu verbinden funktioniert.

---

## 9. RDP-Präferenzen

Bei einem bereits gespeicherten Ziel prüfen:

- Benutzername/Domäne bleiben erhalten.
- Zwischenablage bleibt erhalten.
- Laufwerksumleitung bleibt erhalten.
- Mikrofonumleitung bleibt erhalten.
- Audioausgabemodus bleibt erhalten.
- Admin-Sitzung bleibt erhalten.
- normaler Multi-Monitor-Schalter bleibt erhalten.
- `settings.json` enthält kein Passwort.

---

## 10. RDP-Audio und Geräteumleitung

Für diese Tests die Sitzung jeweils über **Neu verbinden** neu aufbauen, weil die Einstellungen vor `Connect()` gesetzt werden.

### Zwischenablage

- aktivieren → Text zwischen lokal und remote kopieren, soweit Zielrichtlinien dies erlauben
- deaktivieren → neue Sitzung aufbauen und prüfen, dass Umleitung nicht angeboten wird

### Laufwerke

1. **Laufwerke** zunächst deaktiviert lassen.
2. Verbindung herstellen und prüfen, dass lokale Laufwerke nicht absichtlich umgeleitet werden.
3. **Laufwerke** aktivieren.
4. Neu verbinden.
5. Prüfen, ob Windows RDP lokale Laufwerke in der Sitzung anbietet bzw. den entsprechenden Sicherheitsdialog zeigt.

Die Option muss bei neuen Zielen standardmäßig aus sein.

### Mikrofon

1. **Mikrofon** deaktiviert → neu verbinden.
2. Danach aktivieren → neu verbinden.
3. Auf einem geeigneten Ziel prüfen, ob das lokale Standardmikrofon als Remote-Audioeingang verfügbar wird.

### Audioausgabe

Alle drei Varianten einzeln mit Neuverbinden testen:

```text
Auf diesem Computer
Auf dem Remotecomputer
Kein Audio
```

Prüfen:

- Modus lokal: Remote-Audio wird am Admin-PC wiedergegeben.
- Modus remote: Audio bleibt am Zielrechner, soweit Windows/RDP dies zulässt.
- kein Audio: RDP-Audio wird nicht wiedergegeben.

### Externer mstsc-Fallback

`Eingebetteten RDP-Viewer bevorzugen` deaktivieren und erneut verbinden.

Die generierte Datei unter

```text
%AppData%\NetSupportRemoteAdmin\rdp\
```

soll je nach Einstellung passende Werte enthalten:

```text
redirectclipboard:i:0|1
drivestoredirect:s:*     oder leer
audiocapturemode:i:0|1
audiomode:i:0|1|2
```

Die Datei darf keinen Benutzernamen und kein Passwort enthalten.

---

## 11. Multi-Monitor – alle Monitore

Mit mindestens zwei lokalen Monitoren:

1. Im eingebetteten RDP **Mehrere Monitore** aktivieren.
2. Neu verbinden.
3. Prüfen, ob Windows RDP die lokale Multi-Monitor-Anordnung verwendet.
4. Deaktivieren und erneut verbinden.

Bei Multi-Monitor wird SmartSizing nicht zusätzlich erzwungen.

---

## 12. Gezielte RDP-Monitorwahl

Voraussetzung: mehrere lokale Monitore.

### IDs ermitteln

1. Ziel auswählen.
2. **Erweitert** öffnen.
3. Unter **Gezielte RDP-Monitore** auf **IDs anzeigen** klicken.
4. Windows muss `mstsc.exe /l` öffnen und die lokalen Monitor-IDs anzeigen.

### Auswahl speichern

1. Gültige IDs eintragen, zum Beispiel `0,1`.
2. **Speichern / Aktualisieren**.
3. `settings.json` auf `"rdpSelectedMonitors": "0,1"` prüfen.

### RDP starten

Erwartet:

- der externe Windows-RDP-Client wird verwendet
- unter `%AppData%\NetSupportRemoteAdmin\rdp\` entsteht eine `.rdp`-Datei
- Datei enthält `use multimon:i:1`
- Datei enthält `selectedmonitors:s:0,1`
- Redirection-/Audioeinstellungen werden ebenfalls übernommen
- Datei enthält **kein Passwort**
- Admin-Sitzung nutzt weiterhin `/admin`

### Eingabevalidierung

Ungültig `0,a` testen: verständliche Warnung, nicht speichern, App bleibt stabil.

Duplikate `0, 1, 1` testen: gespeichert wird `0,1`.

### Zurück zum eingebetteten Viewer

Monitor-ID-Feld leeren, **Speichern / Aktualisieren**, erneut RDP starten. Bei aktiviertem `useEmbeddedRdp` muss wieder der eingebettete Viewer verwendet werden.

---

## 13. Remote-Aktionen

In aktiver eingebetteter RDP-Sitzung:

- Alt+Tab remote
- Start remote
- Task-Manager

Nicht unterstützte Aktionen dürfen nur eine Statusmeldung erzeugen.

---

## 14. Auto-Reconnect

Netz kurz unterbrechen und wiederherstellen.

Erwartet:

- Auto-Reconnect-Status
- Versuchszähler
- Netzverfügbarkeit
- Meldung nach erfolgreicher Wiederverbindung

---

## 15. Einstellungsfenster

**Erweitert → Einstellungen** öffnen.

Prüfen:

- aktueller NetSupport-Pfad wird angezeigt.
- **Durchsuchen…** kann `PCICTLUI.EXE` auswählen.
- ungültiger/nicht vorhandener Pfad erzeugt eine Warnung.
- Embedded-RDP, externer Vollbildmodus, Start minimiert und Diagnose werden gespeichert.
- nach Änderung des NetSupport-Pfads aktualisiert sich die Providerliste ohne Programmneustart.

---

## 16. Windows-Autostart

**Mit Windows starten** aktivieren.

Prüfen:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

muss einen Wert `NetSupportRemoteAdmin` enthalten. Danach deaktivieren und prüfen, dass genau dieser Wert entfernt wird. Es dürfen keine HKLM-/maschinenweiten Autostartwerte angelegt werden.

---

## 17. Diagnoseprotokoll

Diagnose aktivieren und einige Aktionen ausführen.

Pfad:

```text
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

Prüfen:

- Programmstart und Remote-Aktionsstarts werden protokolliert.
- AD-/Statusfehler erscheinen bei Fehlerfällen.
- **Diagnoseordner öffnen** funktioniert.
- kein Passwort/Credential steht im Log.
- keine Bildschirm-/Zwischenablageinhalte stehen im Log.
- Rotation erzeugt bei ungefähr 2 MB `application.log.1`.
- nach Deaktivieren der Diagnose bleiben neue normale Logeinträge aus.

---

## 18. Supportpaket und Anonymisierung

Vorbereitung:

1. Diagnose aktivieren.
2. Mindestens einen gespeicherten Testrechner mit erkennbarem Namen/Host anlegen.
3. Erfolgreiche und eine absichtlich fehlerhafte Remote-Aktion erzeugen.
4. **Erweitert → Einstellungen → Diagnose und Support** öffnen.

### Mit Anonymisierung

**Supportpaket anonymisieren (empfohlen)** aktiviert lassen, ZIP erzeugen und entpacken.

Erwarteter Inhalt:

```text
README.txt
system-info.json
configuration-summary.json
recent-history.json
recent-errors.txt
logs/
```

Prüfen:

- Original-`settings.json` ist **nicht** enthalten.
- bekannte Hostnamen/Anzeigenamen erscheinen als `target-...`.
- lokale Rechner-/Benutzer-/Domainwerte werden in bereinigten Logs ersetzt, soweit bekannt.
- RDP-Benutzername und RDP-Domain stehen nicht im Klartext; nur `...Configured`-Informationen.
- nicht geheime RDP-Schalter für Clipboard/Laufwerke/Mikrofon/Audio dürfen zur Diagnose enthalten sein.
- Passwörter/Credentials sind nirgends enthalten.
- Bildschirm-/Zwischenablage-/Remote-Dateiinhalte sind nicht enthalten.
- `recent-errors.txt` enthält erwartete technische Fehler in bereinigter Form.
- `system-info.json` verwendet `local-machine`, `local-user`, `local-domain`.

### Ohne Anonymisierung

Optional wiederholen. Host-/Anzeigenamen dürfen enthalten sein; Passwörter/Credentials trotzdem nicht. Vor externer Weitergabe manuell prüfen.

### Temporäre Dateien

Nach Erzeugung sollte kein dauerhaftes `NetSupportRemoteAdmin-support-*`-Arbeitsverzeichnis im Temp-Pfad zurückbleiben, sofern Windows die Bereinigung nicht durch einen Dateilock verhindert hat.

Details: `docs/SUPPORT_BUNDLE.md`.

---

## Lokale Dateien

```text
%AppData%\NetSupportRemoteAdmin\settings.json
%AppData%\NetSupportRemoteAdmin\session-history.json
%AppData%\NetSupportRemoteAdmin\logs\application.log
%AppData%\NetSupportRemoteAdmin\rdp\
```

Supportpakete liegen ausschließlich am beim Speichern ausgewählten Zielort. In den normalen AppData-Dateien dürfen keine Passwörter oder andere Credentials auftauchen.

---

## Fehler melden

Hilfreich sind:

- Funktion und sichtbare Fehlermeldung
- Windows-Version des Admin-PCs
- möglichst ein **anonymisiertes Supportpaket**
- eingebettetes oder externes RDP
- bei Monitorproblemen Ausgabe von `mstsc /l` ohne vertrauliche Daten
- gewünschte Monitor-ID-Liste
- zugehöriger GitHub-Actions-Build/Commit

Keine Kennwörter oder Zugangsdaten in Issues, Screenshots, ZIPs oder Logs aufnehmen.
