# Testen von NetSupport Remote Admin

> Kurzanleitung zum Testen eines Entwicklungsstands ohne lokale Visual-Studio-Umgebung.

---

## Automatischer Windows-Testbuild

Der GitHub-Actions-Workflow erzeugt nach einem erfolgreichen Build zusätzlich einen **self-contained Windows-x64-Testbuild**.

Artefaktname:

```text
NetSupport.RemoteAdmin-win-x64
```

Der Build enthält die benötigte .NET-Laufzeit und benötigt deshalb auf dem Testrechner keine separate .NET-8-Installation. Das Artefakt wird aktuell 14 Tage in GitHub Actions aufbewahrt.

---

## Testbuild herunterladen

1. Repository in GitHub öffnen.
2. **Actions** öffnen.
3. Einen erfolgreichen Lauf des Workflows **Build** auswählen.
4. Unter **Artifacts** `NetSupport.RemoteAdmin-win-x64` herunterladen.
5. ZIP-Datei entpacken.
6. `NetSupport.RemoteAdmin.exe` starten.

Das Artefakt ist ein Entwicklungs-/Testbuild. Für produktive Verteilung ist später ein signierter Installer beziehungsweise Release-Prozess sinnvoll.

---

## Voraussetzungen je Funktion

### Grundanwendung

- Windows 10 oder Windows 11, 64 Bit

### NetSupport-Aktionen

- NetSupport Manager Control muss auf dem Admin-PC installiert sein.
- `PCICTLUI.EXE` wird automatisch in den üblichen Installationspfaden gesucht.

### Active Directory

Für **Domäne laden** wird aktuell benötigt:

```text
RSAT / ActiveDirectory PowerShell
```

### Rechnerdetails

IP-Auflösung funktioniert über DNS. Für Betriebssystem, angemeldeten Benutzer und Modell verwendet das Tool Remote-CIM/WSMan mit der aktuellen Windows-Identität.

Je nach Domänenrichtlinie müssen deshalb Remoteverwaltung/WSMan und die entsprechenden Firewallregeln erlaubt sein. Ist das nicht der Fall, soll die Anwendung **nicht** abstürzen: IP/Status können weiterhin verfügbar sein und die Detailkarte zeigt einen Hinweis auf unvollständige Verwaltungsdaten.

### Eingebettetes RDP

Das eingebettete RDP verwendet das mit Windows bereitgestellte Microsoft Remote Desktop ActiveX Control. RDP-Passwörter werden nicht in der Anwendung gespeichert.

---

## Empfohlene Testreihenfolge

### 1. Programmstart

Prüfen:

- Anwendung startet ohne Fehlermeldung.
- Hauptfenster kann geschlossen und aus dem Tray wieder geöffnet werden.
- Tray-Menü **Beenden** beendet die Anwendung vollständig.

### 2. Rechnerliste

Prüfen:

- Rechnername/IP kann direkt eingegeben werden.
- **Domäne laden** liefert Rechner, wenn RSAT vorhanden ist.
- Filter funktioniert.
- **Status prüfen** zeigt erreichbare und nicht erreichbare Rechner.
- In der Detailkarte wird nach der Statusprüfung ein Prüfzeitpunkt angezeigt.

### 3. Rechnerdetails

Einen erreichbaren Domänenrechner auswählen und **Rechnerdetails laden** drücken.

Erwartet bei vollständigem CIM/WSMan-Zugriff:

- IP-Adresse wird angezeigt.
- angemeldeter Benutzer wird angezeigt, sofern Windows einen interaktiven Benutzer meldet.
- Windows-Edition/-Version wird angezeigt.
- Hersteller/Modell wird angezeigt.
- Zeitstempel wird aktualisiert.

Dann einen Rechner testen, auf dem WSMan/CIM absichtlich nicht erreichbar ist.

Erwartet:

- Anwendung bleibt bedienbar.
- Detailabfrage endet spätestens nach ungefähr 12 Sekunden.
- Statuszeile meldet nur teilweise verfügbare Verwaltungsdaten oder ein Zeitlimit.
- NetSupport/RDP/Statusprüfung bleiben unabhängig davon verwendbar.

### 4. NetSupport

Mit einem Testrechner prüfen:

- Steuern
- Nur ansehen
- Chat
- Inventar
- Remote CMD
- Dateien

### 5. Eingebettetes RDP

Prüfen:

- RDP-Fenster öffnet sich.
- Credential-Dialog erscheint bei Bedarf.
- Sessionstatus wechselt durch Connecting / Connected / Login.
- **An Fenster anpassen** funktioniert.
- Vollbild/Fenstermodus funktioniert.
- Trennen und Neu verbinden funktionieren.

### 6. RDP-Präferenzen

Bei einem gespeicherten Ziel prüfen:

- Benutzername/Domäne werden wieder angezeigt.
- Zwischenablage-Einstellung bleibt erhalten.
- Admin-Sitzungs-Einstellung bleibt erhalten.
- Multi-Monitor-Einstellung bleibt erhalten.
- Es befindet sich **kein Passwort** in `%AppData%\NetSupportRemoteAdmin\settings.json`.

### 7. Multi-Monitor

Voraussetzung: Admin-PC mit mindestens zwei aktiven Monitoren und ein RDP-Ziel, das Multi-Monitor unterstützt.

Prüfen:

1. **Mehrere Monitore** aktivieren.
2. **Neu verbinden** ausführen.
3. Prüfen, ob die RDP-Sitzung die lokalen Monitore verwendet.
4. Multi-Monitor wieder deaktivieren und erneut verbinden.
5. Prüfen, ob eine normale Einzelmonitor-Sitzung zurückkehrt.

Hinweis: Die aktuelle Version verwendet alle vom Windows-RDP-Client verfügbaren Monitore. Eine Auswahl bestimmter Monitor-IDs ist noch nicht implementiert.

Bei Multi-Monitor wird SmartSizing nicht zusätzlich erzwungen.

### 8. Remote-Aktionen

Während einer aktiven RDP-Sitzung prüfen:

- **Alt+Tab remote**
- **Start remote**
- **Task-Manager**

Einzelne Aktionen können abhängig von Client-/Serverversion nicht unterstützt werden. Dann soll nur eine Statusmeldung erscheinen.

### 9. Auto-Reconnect

Auf einem geeigneten Testsystem kurzzeitig die Netzwerkverbindung unterbrechen und wiederherstellen.

Erwartetes Verhalten:

- Sessionstatus zeigt den automatischen Wiederverbindungsversuch.
- Versuchszähler und Netzverfügbarkeit werden angezeigt, soweit vom Microsoft-Control gemeldet.
- Nach erfolgreicher Wiederverbindung erscheint eine entsprechende Statusmeldung.

### 10. Externer RDP-Fallback

Optional `useEmbeddedRdp` in `settings.json` auf `false` setzen.

Prüfen:

- RDP startet über `mstsc.exe`.
- aktivierte Admin-Sitzung wird als `/admin` übernommen.
- aktivierter Multi-Monitor-Modus wird als `/multimon` übernommen.

---

## Konfigurationsdatei

Pfad:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

Dort dürfen nur dauerhafte Ziele und nicht geheime Präferenzen stehen. Laufzeitdetails wie aktuelle IP, Windows-Version, Benutzer und Online-Status werden bewusst nicht gespeichert.

Passwörter dürfen dort niemals auftauchen.

---

## Fehler melden

Für einen reproduzierbaren Fehler sind besonders hilfreich:

- betroffene Funktion
- genaue sichtbare Fehlermeldung
- Windows-Version des Admin-PCs
- bei Rechnerdetails: ob `Test-WSMan <rechner>` grundsätzlich funktioniert
- bei RDP: eingebettetes RDP oder `mstsc.exe`
- Anzahl/Anordnung der Monitore bei Multi-Monitor-Problemen
- ob der Fehler bei einem zweiten Zielrechner ebenfalls auftritt
- zugehöriger GitHub-Actions-Build bzw. Commit

Keine Kennwörter oder andere Zugangsdaten in Issues, Screenshots oder Logs aufnehmen.
