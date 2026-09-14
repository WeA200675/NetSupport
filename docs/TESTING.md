# Testen von NetSupport Remote Admin

> Kurzanleitung zum Testen eines Entwicklungsstands ohne lokale Visual-Studio-Umgebung.

---

## Automatischer Windows-Testbuild

Der GitHub-Actions-Workflow erzeugt nach einem erfolgreichen Build zusätzlich einen **self-contained Windows-x64-Testbuild**.

Artefaktname:

```text
NetSupport.RemoteAdmin-win-x64
```

Der Build enthält die benötigte .NET-Laufzeit und benötigt deshalb auf dem Testrechner keine separate .NET-8-Installation.

Das Artefakt wird aktuell 14 Tage in GitHub Actions aufbewahrt.

---

## Testbuild herunterladen

1. Repository in GitHub öffnen.
2. **Actions** öffnen.
3. Einen erfolgreichen Lauf des Workflows **Build** auswählen.
4. Im Bereich **Artifacts** `NetSupport.RemoteAdmin-win-x64` herunterladen.
5. ZIP-Datei in einen normalen Ordner entpacken.
6. `NetSupport.RemoteAdmin.exe` starten.

Für produktive Verteilung ist später ein signierter Installer bzw. ein sauberer Release-Prozess sinnvoll. Das aktuelle Artefakt ist bewusst ein Entwicklungs-/Testbuild.

---

## Voraussetzungen je Funktion

### Grundanwendung

- Windows 10 oder Windows 11, 64 Bit

### NetSupport-Aktionen

- NetSupport Manager Control muss auf dem Admin-PC installiert sein.
- `PCICTLUI.EXE` wird automatisch in den üblichen Installationspfaden gesucht.

### Active Directory

Für **Domäne laden** wird aktuell das ActiveDirectory-PowerShell-Modul benötigt:

```text
RSAT / ActiveDirectory PowerShell
```

### Eingebettetes RDP

Das eingebettete RDP verwendet das mit Windows bereitgestellte Microsoft Remote Desktop ActiveX Control.

Es werden keine RDP-Passwörter in der Anwendung gespeichert. Kennwörter werden über den normalen Windows-RDP-Credential-Dialog eingegeben.

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

### 3. NetSupport

Mit einem Testrechner prüfen:

- Steuern
- Nur ansehen
- Chat
- Inventar
- Remote CMD
- Dateien

### 4. Eingebettetes RDP

Prüfen:

- RDP-Fenster öffnet sich innerhalb der Anwendung.
- Credential-Dialog erscheint bei Bedarf.
- Verbindung wird aufgebaut.
- Sessionstatus wechselt nachvollziehbar durch Connecting / Connected / Login.
- **An Fenster anpassen** funktioniert.
- Vollbild/Fenstermodus funktioniert.
- Trennen und Neu verbinden funktionieren.

### 5. RDP-Präferenzen

Bei einem gespeicherten Ziel prüfen:

- Benutzername/Domäne werden wieder angezeigt.
- Zwischenablage-Einstellung bleibt erhalten.
- Admin-Sitzungs-Einstellung bleibt erhalten.
- Es befindet sich **kein Passwort** in `%AppData%\NetSupportRemoteAdmin\settings.json`.

### 6. Remote-Aktionen

Während einer aktiven RDP-Sitzung prüfen:

- **Alt+Tab remote**
- **Start remote**
- **Task-Manager**

Hinweis: Einzelne Remote-Aktionen können abhängig von Windows-/RDP-Version des Clients und Servers nicht unterstützt werden. In diesem Fall soll die Anwendung nur eine Statusmeldung anzeigen und stabil weiterlaufen.

### 7. Auto-Reconnect

Auf einem geeigneten Testsystem kurzzeitig die Netzwerkverbindung unterbrechen und wiederherstellen.

Erwartetes Verhalten:

- Sessionstatus zeigt den automatischen Wiederverbindungsversuch.
- Versuchszähler und Netzverfügbarkeit werden angezeigt, soweit vom Microsoft-Control gemeldet.
- Nach erfolgreicher Wiederverbindung erscheint eine entsprechende Statusmeldung.

---

## Konfigurationsdatei

Pfad:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

Bei Fehlern ist diese Datei hilfreich, um die tatsächlich verwendeten Ziele und nicht geheimen Präferenzen zu prüfen.

Passwörter dürfen dort niemals auftauchen.

---

## Fehler melden

Für einen reproduzierbaren Fehler sind besonders hilfreich:

- betroffene Funktion
- Zielrechner nur in anonymisierter Form, falls erforderlich
- genaue sichtbare Fehlermeldung
- Windows-Version des Admin-PCs
- bei RDP: ob eingebettetes RDP oder `mstsc.exe` verwendet wurde
- ob der Fehler bei einem zweiten Zielrechner ebenfalls auftritt
- zugehöriger GitHub-Actions-Build bzw. Commit

Keine Kennwörter oder andere Zugangsdaten in Issues, Screenshots oder Logs aufnehmen.
