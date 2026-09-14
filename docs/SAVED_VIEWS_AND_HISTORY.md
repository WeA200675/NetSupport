# Gespeicherte Ansichten und Verbindungsverlauf

> Diese Datei beschreibt die wiederverwendbaren Listenfilter und den lokalen Verlauf von Remote-Verbindungsstarts in **NetSupport Remote Admin**.

---

## Ziel

Bei einem größeren Rechnerbestand werden häufig dieselben Teilmengen benötigt, zum Beispiel:

- nur Server
- nur Favoriten
- eine bestimmte Rechnergruppe
- ein Suchbegriff zusammen mit einer Gruppe

Zusätzlich soll nachvollziehbar sein, welcher Rechner zuletzt mit welchem Remote-Provider gestartet wurde, ohne dafür Passwörter oder eine zentrale Audit-Datenbank aufzubauen.

---

## Gespeicherte Ansichten

Eine Ansicht speichert genau die Filter, die oberhalb der Rechnerliste sichtbar sind:

```json
{
  "name": "Server",
  "searchText": "",
  "group": "Server",
  "favoritesOnly": false
}
```

Gespeichert werden:

- Name der Ansicht
- Textsuche
- Gruppenfilter
- **Nur Favoriten** an/aus

Die Ansichten liegen zusammen mit der übrigen Bedienkonfiguration in:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

### Bedienung

1. Gewünschten Suchtext, Gruppe und Favoritenfilter einstellen.
2. Im Feld **Ansicht** einen Namen eingeben.
3. **Ansicht speichern** drücken.
4. Später die gespeicherte Ansicht im Kombinationsfeld auswählen.

Beim Auswählen werden die drei Filter sofort wieder angewendet.

Eine bestehende Ansicht wird aktualisiert, wenn erneut unter demselben Namen gespeichert wird.

**Ansicht löschen** entfernt nur die gespeicherte Filterdefinition. Rechner, Gruppen und Favoriten bleiben unverändert.

---

## Verbindungsverlauf

Der lokale Verlauf liegt absichtlich in einer eigenen Datei:

```text
%AppData%\NetSupportRemoteAdmin\session-history.json
```

Damit bleibt `settings.json` eine reine Konfigurationsdatei.

Pro Verbindungsstart werden gespeichert:

- Zeitpunkt
- Hostname
- optionaler Anzeigename
- Provider-ID
- lesbarer Providername
- Aktion, zum Beispiel `Control`, `View`, `FileTransfer`
- Erfolg oder Fehler beim Starten
- bei einem Fehler die sichtbare Fehlermeldung

Nicht gespeichert werden:

- Passwörter
- RDP-Credentials
- aktuelle IP-/CIM-Inventardaten
- Bildschirminhalte
- Tastatureingaben
- Dateien aus der Remotesitzung

---

## Bedeutung von „Verbindungsstart“

Der Verlauf dokumentiert, dass die Anwendung einen Remote-Provider erfolgreich gestartet beziehungsweise nicht starten konnte.

Bei externen Programmen wie NetSupport `PCICTLUI.EXE` kann das Frontend nicht zuverlässig erkennen, wann der Benutzer die eigentliche Remotesitzung beendet oder ob die Gegenstelle nach dem Prozessstart später noch abgelehnt hat.

Deshalb ist der Verlauf bewusst ein **Startverlauf** und kein revisionssicheres Session-Audit.

---

## Anzeige im Hauptfenster

Der Bereich **Zuletzt verwendet** zeigt die neuesten Einträge mit:

- Erfolgssymbol
- Zeitpunkt
- Zielrechner
- Provider und Aktion

Ein Doppelklick auf einen Verlaufseintrag übernimmt den Rechner wieder als aktuelles Ziel. Es wird dabei absichtlich **keine Verbindung automatisch gestartet**.

Auf der Rechnerkarte erscheint zusätzlich unter **Letzter Start** der jüngste bekannte Verlaufseintrag für den ausgewählten Host.

---

## CSV-Export

Über **CSV exportieren** kann der komplette lokal gespeicherte Startverlauf in eine frei gewählte Datei exportiert werden.

Spalten:

```text
Zeitpunkt
Rechner
Name
Provider
Provider-ID
Aktion
Erfolg
Fehler
```

Technische Eigenschaften:

- Semikolon als Trennzeichen
- UTF-8 mit BOM
- lokaler Zeitstempel
- Quotes/Escaping für Semikolons und Anführungszeichen
- Zeilenumbrüche in Textwerten werden neutralisiert

Das Format ist bewusst so gewählt, dass die Datei auf deutschsprachigen Windows-Systemen in Excel zuverlässig geöffnet werden kann.

Der Export enthält dieselben sicherheitsbewussten Daten wie der JSON-Verlauf und insbesondere **keine Passwörter oder RDP-Credentials**.

---

## Begrenzung und Löschen

Die History-Datei wird auf maximal **100 Einträge** begrenzt.

Im Hauptfenster kann der komplette lokale Verlauf über **Löschen** entfernt werden.

Das Löschen betrifft nur:

```text
session-history.json
```

Gespeicherte Rechner, Favoriten, Gruppen, RDP-Einstellungen und Ansichten bleiben erhalten.

---

## Fehlerrobustheit

Der Verlauf ist eine Komfortfunktion und darf die Fernwartung nicht blockieren.

Deshalb gilt:

- Fehler beim Schreiben des Verlaufs verhindern keine Remote-Aktion.
- Eine beschädigte oder nicht lesbare History-Datei verhindert keinen Programmstart.
- Die History wird separat von der Hauptkonfiguration verarbeitet.
- Schreibvorgänge werden serialisiert und über eine temporäre Datei ersetzt.
- Fehler beim CSV-Export verändern oder löschen den JSON-Verlauf nicht.

---

## Architektur

```text
MainWindow
   |
   +--> AppConfig.SavedViews
   |       +--> settings.json
   |
   +--> ISessionHistoryService
           |
           +--> JsonSessionHistoryService
                   |
                   +--> session-history.json
                   +--> CSV-Export
```

Modelle:

```text
Models/SavedTargetView.cs
Models/SessionHistoryEntry.cs
```

Services:

```text
Services/ISessionHistoryService.cs
Services/JsonSessionHistoryService.cs
```

---

## Designentscheidung

Gespeicherte Ansichten sind Teil der persönlichen Bedienkonfiguration und gehören daher in `settings.json`.

Der Verlauf ändert sich dagegen bei jeder Verbindung und wird deshalb separat gehalten. So lässt er sich löschen, exportieren oder später durch einen anderen History-/Audit-Provider ersetzen, ohne die eigentliche Zielkonfiguration zu verändern.
