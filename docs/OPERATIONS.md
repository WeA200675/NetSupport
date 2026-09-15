# Betrieb, Einstellungen und Diagnose

> Diese Datei beschreibt die betriebliche Nutzung von **NetSupport Remote Admin**: Einstellungen, Autostart, Diagnoseprotokoll, Supportpaket und Export des lokalen Verbindungsverlaufs.

---

## Einstellungsfenster

Das Hauptfenster enthält unter **Erweitert → Einstellungen** eine eigene Einstellungsseite.

Dort können aktuell geändert bzw. ausgeführt werden:

- **Mit Windows starten**
- **Beim Start minimiert im Infobereich öffnen**
- **Diagnoseprotokoll schreiben**
- **Eingebetteten RDP-Viewer bevorzugen**
- **Externes RDP standardmäßig im Vollbild starten**
- Pfad zu `PCICTLUI.EXE`
- **Diagnoseordner öffnen**
- **Supportpaket erstellen…**

Für diese Standardoptionen ist dadurch keine manuelle Bearbeitung von `settings.json` mehr erforderlich.

---

## Windows-Autostart

Autostart wird ausschließlich für den aktuell angemeldeten Windows-Benutzer eingerichtet.

Registry-Pfad:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

Wert:

```text
NetSupportRemoteAdmin
```

Es werden keine computerweiten Registry-Werte und keine Gruppenrichtlinien geändert. Für das Aktivieren oder Deaktivieren des Autostarts sind deshalb normalerweise keine erhöhten Rechte erforderlich.

Wenn **Beim Start minimiert** zusätzlich aktiviert ist, startet die Anwendung beim Windows-Login direkt im Infobereich.

---

## Diagnoseprotokoll

Das optionale Diagnoseprotokoll liegt unter:

```text
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

Die Einstellungsseite enthält **Diagnoseordner öffnen**.

Das Protokoll erfasst für die Fehlersuche unter anderem:

- Programmstart und Programmende
- Änderungen wichtiger Einstellungen
- Start von Remote-Aktionen
- Provider und Aktion
- Zielhostname
- Active-Directory-Ladevorgänge und Fehler
- Statusprüfungen
- Fehler bei Rechnerdetails
- Fehler beim lokalen Verbindungsverlauf
- unbehandelte UI-/Task-Ausnahmen

### Datenschutz und Sicherheit

Bewusst nicht protokolliert werden:

- Passwörter
- RDP-Credentials
- Bildschirm- oder Sitzungsinhalte
- Zwischenablageinhalte
- Remote-Dateiinhalte
- CIM-/Inventardaten wie Seriennummern oder komplette Datensätze

Hostnamen und die gestartete Verwaltungsaktion sind dagegen Teil der betrieblichen Diagnose.

### Rotation

`application.log` wird bei ungefähr 2 MB rotiert.

Die vorherige Datei wird als

```text
application.log.1
```

erhalten. Damit wächst das Diagnoseverzeichnis nicht unbegrenzt durch eine einzelne Logdatei.

Ein Fehler im Diagnose-Logging darf die Remoteverwaltung niemals blockieren.

---

## Supportpaket

Unter **Einstellungen → Diagnose und Support** steht **Supportpaket erstellen…** zur Verfügung.

Die Anonymisierung ist standardmäßig aktiviert.

Das ZIP enthält eine eigens erzeugte, bereinigte Supportdarstellung:

```text
README.txt
system-info.json
configuration-summary.json
recent-history.json
recent-errors.txt
logs/
```

Wichtig:

- die Original-`settings.json` wird nicht kopiert
- RDP-Passwörter/Credentials werden nicht aufgenommen
- RDP-Benutzername/Domain werden in der Konfigurationsübersicht nur als `konfiguriert: ja/nein` abgebildet
- bei aktiver Anonymisierung werden bekannte Host-/Rechner-/Benutzer-/Domainwerte in Logs und Verlauf ersetzt
- Zielrechner erscheinen z. B. als `target-001`
- Gruppen und Ansichten werden bei aktiver Anonymisierung abstrahiert
- das Paket wird nur an den vom Benutzer ausgewählten Speicherort geschrieben

Technische Details: [`SUPPORT_BUNDLE.md`](SUPPORT_BUNDLE.md).

---

## Lokaler Verbindungsverlauf

Der bestehende Startverlauf liegt separat unter:

```text
%AppData%\NetSupportRemoteAdmin\session-history.json
```

Er enthält maximal 100 Einträge mit:

- Zeitpunkt
- Zielrechner
- optionalem Anzeigenamen
- Provider
- Aktion
- Erfolg/Fehler des Startversuchs
- optionaler Fehlermeldung

Dieser Verlauf ist ein lokaler Bedien-/Startverlauf und **kein Compliance-Audit** einer vollständigen Remote-Sitzung.

---

## CSV-Export

Im Bereich **Zuletzt verwendet** steht **CSV exportieren** zur Verfügung.

Der Export enthält die vollständigen aktuell gespeicherten History-Einträge mit folgenden Spalten:

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

Die Datei wird semikolongetrennt und als UTF-8 mit BOM geschrieben, damit sie auf deutschsprachigen Windows-/Excel-Systemen zuverlässig geöffnet werden kann.

Auch der CSV-Export enthält keine Passwörter oder gespeicherten RDP-Anmeldeinformationen.

---

## NetSupport-Pfad

Der Pfad zu `PCICTLUI.EXE` kann in der Einstellungsseite geändert werden.

Nach dem Speichern aktualisiert das Hauptfenster die verfügbaren Provider sofort. Ein Neustart ist dafür nicht erforderlich.

Ein ungewöhnlicher oder nicht vorhandener Pfad erzeugt vor dem Speichern eine Warnung, kann aber bei Bedarf trotzdem übernommen werden.

---

## Konfigurationsdatei

Die normalen Programmeinstellungen liegen weiterhin unter:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

Die Einstellungsseite schreibt unter anderem:

```json
{
  "startMinimized": true,
  "useEmbeddedRdp": true,
  "useFullScreenRdp": false,
  "diagnosticLoggingEnabled": true,
  "netSupportExecutable": "C:\\Program Files (x86)\\NetSupport\\NetSupport Manager\\PCICTLUI.EXE"
}
```

Der Windows-Autostart selbst wird nicht in `settings.json`, sondern im oben beschriebenen HKCU-Run-Key verwaltet.

---

## Fehlerdiagnose

Bei einem reproduzierbaren Problem sind typischerweise hilfreich:

1. betroffenen Testbuild/Commit notieren
2. Fehler reproduzieren
3. Supportpaket mit aktivierter Anonymisierung erzeugen
4. ZIP kurz auf unerwünschte interne Daten prüfen
5. bei RDP unterscheiden, ob eingebettetes RDP oder `mstsc.exe` verwendet wurde
6. bei AD/CIM prüfen, ob RSAT bzw. WSMan grundsätzlich verfügbar sind

Wenn kein Supportpaket benötigt wird, können alternativ `application.log` und die sichtbare Fehlermeldung separat betrachtet werden.
