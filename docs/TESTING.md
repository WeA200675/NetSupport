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

## Lokale Dateien

Konfiguration:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

Startverlauf:

```text
%AppData%\NetSupportRemoteAdmin\session-history.json
```

Generierte gezielte RDP-Verbindungen:

```text
%AppData%\NetSupportRemoteAdmin\rdp\
```

In keiner dieser Dateien dürfen Passwörter oder andere Credentials auftauchen.

---

## Fehler melden

Hilfreich sind:

- Funktion und sichtbare Fehlermeldung
- Windows-Version des Admin-PCs
- Ziel anonymisiert, falls nötig
- eingebettetes oder externes RDP
- bei Monitorproblemen Ausgabe von `mstsc /l` ohne vertrauliche Daten
- gewünschte Monitor-ID-Liste
- zugehöriger GitHub-Actions-Build/Commit

Keine Kennwörter oder Zugangsdaten in Issues, Screenshots oder Logs aufnehmen.
