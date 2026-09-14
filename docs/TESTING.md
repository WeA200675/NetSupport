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

Ist WSMan/CIM nicht verfügbar, soll die Anwendung nicht abstürzen. IP/Status und die Remote-Funktionen bleiben unabhängig verwendbar.

### Eingebettetes RDP

Das eingebettete RDP verwendet das mit Windows bereitgestellte Microsoft Remote Desktop ActiveX Control. RDP-Passwörter werden nicht in der Anwendung gespeichert.

---

## Empfohlene Testreihenfolge

### 1. Programmstart

Prüfen:

- Anwendung startet ohne Fehlermeldung.
- Hauptfenster kann geschlossen und aus dem Tray wieder geöffnet werden.
- Tray-Menü **Beenden** beendet die Anwendung vollständig.

### 2. Rechnerliste und Filter

Prüfen:

- Rechnername/IP kann direkt eingegeben werden.
- **Domäne laden** liefert Rechner, wenn RSAT vorhanden ist.
- Textfilter funktioniert.
- **Status prüfen** zeigt erreichbare und nicht erreichbare Rechner.
- Nach der Statusprüfung erscheint ein Prüfzeitpunkt.

### 3. Favoriten, Gruppen und Standardverbindung

Einen Testrechner auswählen und folgende Werte setzen:

- **Favorit** aktivieren
- Gruppe, z. B. `Testgruppe`
- Standard-Provider auswählen
- **Speichern / Aktualisieren** drücken

Prüfen:

1. Der Rechner zeigt ein `★`.
2. Favoriten stehen vor normalen Rechnern.
3. **Nur Favoriten** blendet normale Rechner aus.
4. `Testgruppe` erscheint im Gruppenfilter.
5. Die Textsuche findet den Rechner auch über `Testgruppe`.
6. Gruppenfilter `Testgruppe` zeigt nur passende Rechner.
7. Nach Neustart sind Favorit, Gruppe und Standard-Provider noch vorhanden.
8. **Standardverbindung starten** öffnet den gewählten Provider.
9. Doppelklick öffnet den gespeicherten Standard-Provider.
10. Direkte Schnellaktionen **Steuern** bzw. **RDP** funktionieren weiterhin unabhängig vom Standard-Provider.

Explizites Speicherverhalten prüfen:

1. Gruppe/Favorit im Editor ändern.
2. **Nicht** auf **Speichern / Aktualisieren** klicken.
3. Eine Standardverbindung starten und wieder schließen.
4. Anwendung neu starten.
5. Die nicht gespeicherte Organisationsänderung darf nicht dauerhaft übernommen worden sein.

Fallback prüfen, wenn möglich:

- einen bevorzugten Provider konfigurieren, der auf einem zweiten Test-Admin-PC nicht verfügbar ist
- die Standardverbindung soll auf einen verfügbaren Control-Provider zurückfallen und nicht abstürzen

### 4. Gespeicherte Ansichten

Eine Kombination einstellen, zum Beispiel:

- Textfilter leer
- Gruppe `Testgruppe`
- **Nur Favoriten** aktiv

Dann:

1. Im Feld **Ansicht** den Namen `Testansicht` eingeben.
2. **Ansicht speichern** drücken.
3. Filter verändern.
4. `Testansicht` wieder auswählen.

Erwartet:

- Textfilter, Gruppe und Favoritenfilter werden wiederhergestellt.
- Unter demselben Namen erneut speichern aktualisiert die bestehende Ansicht statt eine zweite anzulegen.
- Nach einem Neustart ist `Testansicht` weiterhin vorhanden.
- **Ansicht löschen** entfernt nur die Ansicht; Rechner/Favoriten/Gruppen bleiben erhalten.

Optional `settings.json` prüfen. Unter `savedViews` dürfen nur Filterdefinitionen stehen.

### 5. Rechnerdetails

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

### 6. NetSupport

Mit einem Testrechner prüfen:

- Steuern
- Nur ansehen
- Chat
- Inventar
- Remote CMD
- Dateien

### 7. Verbindungsverlauf

Mindestens zwei unterschiedliche Remote-Aktionen starten, zum Beispiel NetSupport **Steuern** und **RDP**.

Prüfen:

1. Unter **Zuletzt verwendet** erscheinen neue Einträge.
2. Zeitpunkt, Ziel, Provider und Aktion sind plausibel.
3. Auf der Rechnerkarte erscheint unter **Letzter Start** der jüngste Eintrag des ausgewählten Hosts.
4. Doppelklick auf einen Verlaufseintrag übernimmt den Host als aktuelles Ziel, startet aber **keine** Verbindung automatisch.
5. Ein absichtlich nicht verfügbarer/fehlerhafter Providerstart erscheint als Fehler-Eintrag.
6. In `%AppData%\NetSupportRemoteAdmin\session-history.json` befinden sich keine Passwörter oder Credentials.
7. **Verlauf löschen** entfernt die History, aber nicht `settings.json`, gespeicherte Ziele oder Ansichten.
8. Nach mehr als 100 Starts wächst die Datei nicht unbegrenzt; es sollen höchstens 100 Einträge erhalten bleiben.

Hinweis: Der Verlauf protokolliert den Startversuch eines Providers. Bei externen Programmen wie NetSupport ist er kein revisionssicheres Session-Audit.

### 8. Eingebettetes RDP

Prüfen:

- RDP-Fenster öffnet sich.
- Credential-Dialog erscheint bei Bedarf.
- Sessionstatus wechselt durch Connecting / Connected / Login.
- **An Fenster anpassen** funktioniert.
- Vollbild/Fenstermodus funktioniert.
- Trennen und Neu verbinden funktionieren.

### 9. RDP-Präferenzen

Bei einem gespeicherten Ziel prüfen:

- Benutzername/Domäne werden wieder angezeigt.
- Zwischenablage-Einstellung bleibt erhalten.
- Admin-Sitzungs-Einstellung bleibt erhalten.
- Multi-Monitor-Einstellung bleibt erhalten.
- Es befindet sich **kein Passwort** in `%AppData%\NetSupportRemoteAdmin\settings.json`.

### 10. Multi-Monitor

Voraussetzung: Admin-PC mit mindestens zwei aktiven Monitoren und ein RDP-Ziel, das Multi-Monitor unterstützt.

Prüfen:

1. **Mehrere Monitore** aktivieren.
2. **Neu verbinden** ausführen.
3. Prüfen, ob die RDP-Sitzung die lokalen Monitore verwendet.
4. Multi-Monitor wieder deaktivieren und erneut verbinden.
5. Prüfen, ob eine normale Einzelmonitor-Sitzung zurückkehrt.

Die eingebettete ActiveX-Integration verwendet aktuell alle vom Windows-RDP-Client vorgesehenen Monitore über `UseMultimon`. Eine Auswahl bestimmter Monitor-IDs wird bewusst noch nicht über eine undokumentierte COM-Eigenschaft umgesetzt.

Für einen späteren gezielten Monitor-Ausbau soll die dokumentierte RDP-Eigenschaft `selectedmonitors` über den externen RDP-Pfad verwendet werden.

Bei Multi-Monitor wird SmartSizing nicht zusätzlich erzwungen.

### 11. Remote-Aktionen

Während einer aktiven RDP-Sitzung prüfen:

- **Alt+Tab remote**
- **Start remote**
- **Task-Manager**

Einzelne Aktionen können abhängig von Client-/Serverversion nicht unterstützt werden. Dann soll nur eine Statusmeldung erscheinen.

### 12. Auto-Reconnect

Auf einem geeigneten Testsystem kurzzeitig die Netzwerkverbindung unterbrechen und wiederherstellen.

Erwartetes Verhalten:

- Sessionstatus zeigt den automatischen Wiederverbindungsversuch.
- Versuchszähler und Netzverfügbarkeit werden angezeigt, soweit vom Microsoft-Control gemeldet.
- Nach erfolgreicher Wiederverbindung erscheint eine entsprechende Statusmeldung.

### 13. Externer RDP-Fallback

Optional `useEmbeddedRdp` in `settings.json` auf `false` setzen.

Prüfen:

- RDP startet über `mstsc.exe`.
- aktivierte Admin-Sitzung wird als `/admin` übernommen.
- aktivierter Multi-Monitor-Modus wird als `/multimon` übernommen.

---

## Konfigurations- und Verlaufsdateien

Konfiguration:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

Lokaler Startverlauf:

```text
%AppData%\NetSupportRemoteAdmin\session-history.json
```

In `settings.json` dürfen nur dauerhafte Ziele und nicht geheime Präferenzen stehen. Dazu gehören Favorit, Gruppe, bevorzugter Provider, gespeicherte Ansichten und RDP-Präferenzen.

Laufzeitdetails wie aktuelle IP, Windows-Version, Benutzer und Online-Status werden bewusst nicht gespeichert. Passwörter und Credentials dürfen in keiner der beiden Dateien auftauchen.

---

## Fehler melden

Für einen reproduzierbaren Fehler sind besonders hilfreich:

- betroffene Funktion
- genaue sichtbare Fehlermeldung
- Windows-Version des Admin-PCs
- bei Organisationsproblemen: betroffene Gruppe und Provider-ID ohne vertrauliche Daten
- bei Ansichten: Name der Ansicht und erwartete Filterwerte
- bei Verlauf: Provider/Aktion und ob der Start erfolgreich oder fehlgeschlagen war
- bei Rechnerdetails: ob `Test-WSMan <rechner>` grundsätzlich funktioniert
- bei RDP: eingebettetes RDP oder `mstsc.exe`
- Anzahl/Anordnung der Monitore bei Multi-Monitor-Problemen
- ob der Fehler bei einem zweiten Zielrechner ebenfalls auftritt
- zugehöriger GitHub-Actions-Build bzw. Commit

Keine Kennwörter oder andere Zugangsdaten in Issues, Screenshots oder Logs aufnehmen.
