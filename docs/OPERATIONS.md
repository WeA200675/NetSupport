# Betrieb, Einstellungen und Diagnose

> Diese Datei beschreibt die betriebliche Nutzung von **NetSupport Remote Admin**: Einstellungen, Autostart, Diagnoseprotokoll, Supportpaket und Export des lokalen Verbindungsverlaufs.

---

## Remotezugriffsrichtlinie

Für diese Domäne gilt:

```text
Remotezugriff = NetSupport Manager
```

RDP ist als Fernwartungsweg deaktiviert und wird von dieser Anwendung nicht angeboten.

Details: [`DOMAIN_REMOTE_POLICY.md`](DOMAIN_REMOTE_POLICY.md).

---

## Einstellungsfenster

Das Hauptfenster enthält unter **Erweitert → Einstellungen** eine eigene Einstellungsseite.

Dort können aktuell geändert bzw. ausgeführt werden:

- **Mit Windows starten**
- **Beim Start minimiert im Infobereich öffnen**
- **Diagnoseprotokoll schreiben**
- Pfad zu `PCICTLUI.EXE`
- **Systemzustand**
- **Diagnoseordner öffnen**
- **Supportpaket erstellen…**

Für diese Standardoptionen ist keine manuelle Bearbeitung von `settings.json` erforderlich.

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
- Start von NetSupport-Aktionen
- Provider und Aktion
- Zielhostname
- Active-Directory-Ladevorgänge und Fehler
- Statusprüfungen
- Fehler bei Rechnerdetails
- Fehler beim lokalen Verbindungsverlauf
- unbehandelte UI-/Task-Ausnahmen
- blockierte Starts eines nicht zugelassenen Providers

### Datenschutz und Sicherheit

Bewusst nicht protokolliert werden:

- Passwörter oder gespeicherte Credentials
- Bildschirm- oder Sitzungsinhalte
- Zwischenablageinhalte
- Remote-Dateiinhalte
- komplette CIM-/Inventardatensätze

Hostnamen und die gestartete Verwaltungsaktion sind Teil der betrieblichen Diagnose.

### Rotation

`application.log` wird bei ungefähr 2 MB rotiert.

Die vorherige Datei wird als

```text
application.log.1
```

erhalten.

Ein Fehler im Diagnose-Logging darf die Remoteverwaltung niemals blockieren.

---

## Systemzustand

Unter **Einstellungen → Diagnose und Support → Systemzustand** kann der Admin-PC lokal geprüft werden.

Aktuelle Checks:

- Remotezugriffsrichtlinie = NetSupport-only
- AppData-Verzeichnis beschreibbar
- `PCICTLUI.EXE` vorhanden
- RSAT / ActiveDirectory-PowerShell-Modul vorhanden
- lokale CIM-/WSMan-Grundfunktion
- Autostartzustand
- Diagnoseprotokollzustand

Es werden dabei keine Zielrechner gescannt.

Details: [`SYSTEM_HEALTH.md`](SYSTEM_HEALTH.md).

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
- Kennwörter/Credentials werden nicht aufgenommen
- bei aktiver Anonymisierung werden bekannte Host-/Rechner-/Benutzer-/Domainwerte in Logs und Verlauf ersetzt
- Zielrechner erscheinen z. B. als `target-001`
- Gruppen und Ansichten werden bei aktiver Anonymisierung abstrahiert
- die Konfigurationsübersicht enthält `remoteAccessPolicy = NetSupport-only`
- das Paket wird nur an den vom Benutzer ausgewählten Speicherort geschrieben

Technische Details: [`SUPPORT_BUNDLE.md`](SUPPORT_BUNDLE.md).

---

## Lokaler Verbindungsverlauf

Der Startverlauf liegt separat unter:

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

Dieser Verlauf ist ein lokaler Bedien-/Startverlauf und **kein Ersatz für die eigentliche NetSupport-/Unternehmensprotokollierung** einer Remote-Sitzung.

---

## CSV-Export

Im Bereich **Zuletzt verwendet** steht **CSV exportieren** zur Verfügung.

Der Export enthält:

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

---

## NetSupport-Pfad

Der Pfad zu `PCICTLUI.EXE` kann in der Einstellungsseite geändert werden.

Nach dem Speichern aktualisiert das Hauptfenster die verfügbare NetSupport-Providerinstanz sofort. Ein Neustart ist dafür nicht erforderlich.

Ein ungewöhnlicher oder nicht vorhandener Pfad erzeugt vor dem Speichern eine Warnung, kann bei Bedarf trotzdem übernommen werden.

---

## Konfigurationsdatei

Die Programmeinstellungen liegen unter:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

Beispiel:

```json
{
  "netSupportExecutable": "C:\\Program Files (x86)\\NetSupport\\NetSupport Manager\\PCICTLUI.EXE",
  "startMinimized": true,
  "diagnosticLoggingEnabled": true,
  "targets": [],
  "savedViews": []
}
```

Alte RDP-Felder aus früheren Entwicklungsständen werden beim Laden ignoriert und beim nächsten Speichern nicht mehr geschrieben.

Der Windows-Autostart selbst wird nicht in `settings.json`, sondern im HKCU-Run-Key verwaltet.

---

## Fehlerdiagnose

Bei einem reproduzierbaren Problem sind typischerweise hilfreich:

1. betroffenen Testbuild/Commit notieren
2. unter **Systemzustand** NetSupport-Pfad, RSAT und CIM prüfen
3. Fehler reproduzieren
4. Supportpaket mit aktivierter Anonymisierung erzeugen
5. ZIP kurz auf unerwünschte interne Daten prüfen
6. bei NetSupport-Problemen prüfen, ob `PCICTLUI.EXE` manuell mit demselben Ziel funktioniert
7. bei AD/CIM prüfen, ob RSAT bzw. WSMan grundsätzlich verfügbar sind

Wenn kein Supportpaket benötigt wird, können alternativ `application.log` und die sichtbare Fehlermeldung separat betrachtet werden.
