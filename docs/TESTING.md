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

Bei gespeichertem Ziel prüfen:

- Benutzername/Domäne bleiben erhalten.
- Zwischenablage bleibt erhalten.
- Admin-Sitzung bleibt erhalten.
- normaler Multi-Monitor-Schalter bleibt erhalten.
- `settings.json` enthält kein Passwort.

---

## 10. Multi-Monitor – alle Monitore

Mit mindestens zwei lokalen Monitoren:

1. Im eingebetteten RDP **Mehrere Monitore** aktivieren.
2. Neu verbinden.
3. Prüfen, ob Windows RDP die lokale Multi-Monitor-Anordnung verwendet.
4. Deaktivieren und erneut verbinden.

Bei Multi-Monitor wird SmartSizing nicht zusätzlich erzwungen.

---

## 11. Gezielte RDP-Monitorwahl

Voraussetzung: mehrere lokale Monitore.

### IDs ermitteln

1. Ziel auswählen.
2. **Erweitert** öffnen.
3. Unter **Gezielte RDP-Monitore** auf **IDs anzeigen** klicken.
4. Windows muss `mstsc.exe /l` öffnen und die lokalen Monitor-IDs anzeigen.

### Auswahl speichern

1. Gültige IDs eintragen, zum Beispiel:

```text
0,1
```

2. **Speichern / Aktualisieren**.
3. `settings.json` prüfen:

```json
"rdpSelectedMonitors": "0,1"
```

### RDP starten

RDP für das Ziel starten.

Erwartet:

- der externe Windows-RDP-Client wird verwendet, nicht der eingebettete Viewer
- unter `%AppData%\NetSupportRemoteAdmin\rdp\` entsteht eine `.rdp`-Datei
- Datei enthält `use multimon:i:1`
- Datei enthält `selectedmonitors:s:0,1`
- Datei enthält **kein Passwort**
- Zwischenablagepräferenz wird übernommen
- Admin-Sitzung nutzt weiterhin `/admin`

### Eingabevalidierung

Ungültig testen, zum Beispiel:

```text
0,a
```

Erwartet:

- **Speichern / Aktualisieren** zeigt eine verständliche Warnung
- ungültige Auswahl wird nicht gespeichert
- Anwendung bleibt stabil

Duplikate testen:

```text
0, 1, 1
```

Erwartet gespeichert:

```text
0,1
```

### Zurück zum eingebetteten Viewer

1. Monitor-ID-Feld leeren.
2. **Speichern / Aktualisieren**.
3. RDP erneut starten.

Bei aktiviertem `useEmbeddedRdp` muss wieder der eingebettete Viewer verwendet werden.

Hinweis: Windows selbst prüft, ob die ausgewählten Displays für `selectedmonitors` passend/zusammenhängend sind. Die Anwendung validiert nur die ID-Syntax.

---

## 12. Remote-Aktionen

In aktiver eingebetteter RDP-Sitzung:

- Alt+Tab remote
- Start remote
- Task-Manager

Nicht unterstützte Aktionen dürfen nur eine Statusmeldung erzeugen.

---

## 13. Auto-Reconnect

Netz kurz unterbrechen und wiederherstellen.

Erwartet:

- Auto-Reconnect-Status
- Versuchszähler
- Netzverfügbarkeit
- Meldung nach erfolgreicher Wiederverbindung

---

## 14. Normaler externer RDP-Fallback

`useEmbeddedRdp` optional auf `false` setzen, aber `rdpSelectedMonitors` leer lassen.

Prüfen:

- `mstsc.exe` startet.
- Admin-Sitzung → `/admin`.
- normaler Multi-Monitor → `/multimon`.

---

## 15. Einstellungsfenster

**Erweitert → Einstellungen** öffnen.

Prüfen:

- aktueller NetSupport-Pfad wird angezeigt.
- **Durchsuchen…** kann `PCICTLUI.EXE` auswählen.
- ungültiger/nicht vorhandener Pfad erzeugt eine Warnung.
- **Eingebetteten RDP-Viewer bevorzugen** wird in `settings.json` gespeichert.
- **Externes RDP standardmäßig im Vollbild starten** wird gespeichert.
- **Beim Start minimiert** wird gespeichert.
- **Diagnoseprotokoll schreiben** wird gespeichert.
- nach Änderung des NetSupport-Pfads aktualisiert sich die Providerliste ohne Programmneustart.

---

## 16. Windows-Autostart

In **Einstellungen** → **Mit Windows starten** aktivieren.

Prüfen:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

muss einen Wert `NetSupportRemoteAdmin` enthalten.

Danach deaktivieren und prüfen, dass genau dieser Wert entfernt wird.

Optional mit einem Testlogin prüfen:

- Autostart aktiviert → Anwendung startet beim Login.
- **Beim Start minimiert** zusätzlich aktiviert → Anwendung landet direkt im Tray.

Es dürfen keine HKLM-/maschinenweiten Autostartwerte angelegt werden.

---

## 17. Diagnoseprotokoll

Diagnose in den Einstellungen aktivieren und einige Aktionen ausführen.

Pfad:

```text
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

Prüfen:

- Programmstart wird protokolliert.
- Remote-Aktionsstart erscheint mit Provider/Aktion/Zielhost.
- AD-/Statusfehler erscheinen bei absichtlichem Fehlerfall.
- **Diagnoseordner öffnen** öffnet den richtigen Ordner.
- kein Passwort/Credential steht im Log.
- keine Bildschirm-/Zwischenablageinhalte stehen im Log.

Rotation testen, wenn praktikabel:

- Log über ungefähr 2 MB wachsen lassen oder Testdatei entsprechend vorbereiten.
- beim nächsten Schreibvorgang entsteht `application.log.1` und ein neues `application.log`.

Diagnose deaktivieren und prüfen, dass neue normale Logeinträge ausbleiben.

---

## 18. Supportpaket und Anonymisierung

Vorbereitung:

1. Diagnose aktivieren.
2. Mindestens einen gespeicherten Testrechner mit erkennbarem Namen/Host anlegen.
3. Einige erfolgreiche Aktionen und mindestens einen absichtlich fehlerhaften Providerstart erzeugen.
4. **Erweitert → Einstellungen → Diagnose und Support** öffnen.

### Mit Anonymisierung

1. **Supportpaket anonymisieren (empfohlen)** aktiviert lassen.
2. **Supportpaket erstellen…** wählen.
3. ZIP speichern und entpacken.

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
- bekannte Hostnamen/Anzeigenamen erscheinen in der bereinigten Konfiguration/History als `target-...`.
- lokale Rechner-/Benutzer-/Domainwerte werden in bereinigten Logs ersetzt, soweit sie als bekannte Werte vorliegen.
- RDP-Benutzername und RDP-Domain stehen in `configuration-summary.json` nicht im Klartext; nur `...Configured`-Informationen sind enthalten.
- Passwörter/Credentials sind nirgends enthalten.
- Bildschirm-/Zwischenablage-/Remote-Dateiinhalte sind nicht enthalten.
- `recent-errors.txt` enthält die erwarteten technischen Fehler in bereinigter Form.
- `system-info.json` enthält bei aktiver Anonymisierung `local-machine`, `local-user`, `local-domain` statt Klartextidentitäten.

### Ohne Anonymisierung

Test optional wiederholen und die Anonymisierung deaktivieren.

Prüfen:

- Host-/Anzeigenamen dürfen jetzt für die Fehlersuche im Paket enthalten sein.
- Passwörter/Credentials dürfen trotzdem nicht aufgenommen werden.
- vor einer externen Weitergabe muss das Paket manuell geprüft werden.

### Temporäre Dateien

Nach Erzeugung des Pakets sollte kein dauerhaftes `NetSupportRemoteAdmin-support-*`-Arbeitsverzeichnis im Temp-Pfad zurückbleiben, sofern Windows die Bereinigung nicht durch einen Dateilock verhindert hat.

Details: `docs/SUPPORT_BUNDLE.md`.

---

## Lokale Dateien

Konfiguration:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

Startverlauf:

```text
%AppData%\NetSupportRemoteAdmin\session-history.json
```

Diagnose:

```text
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

Generierte gezielte RDP-Verbindungen:

```text
%AppData%\NetSupportRemoteAdmin\rdp\
```

Supportpakete liegen ausschließlich am beim Speichern ausgewählten Zielort.

In den normalen AppData-Dateien dürfen keine Passwörter oder andere Credentials auftauchen.

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
