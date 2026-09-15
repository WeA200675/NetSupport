# Betrieb, Einstellungen und Diagnose

> Diese Datei beschreibt die betriebliche Nutzung von **NetSupport Remote Admin**: Einstellungen, NetSupport-Erkennung, Autostart, Diagnoseprotokoll, Supportpaket und Export des lokalen Verbindungsverlaufs.

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
- **Automatisch erkennen**
- **NetSupport prüfen…**
- **Systemzustand**
- **Diagnoseordner öffnen**
- **Supportpaket erstellen…**

Für diese Standardoptionen ist keine manuelle Bearbeitung von `settings.json` erforderlich.

---

## NetSupport-Pfad und Installationserkennung

Der produktive Remote-Provider akzeptiert ausschließlich eine vorhandene Datei mit dem Namen:

```text
PCICTLUI.EXE
```

### Manuell auswählen

Über **Durchsuchen…** kann die Datei manuell gewählt werden.

### Automatisch erkennen

**Automatisch erkennen** prüft lokal:

- aktuell eingetragenen Pfad
- Program Files (x86)
- Program Files
- Windows-Uninstall-Registry für NetSupport Manager in HKLM/HKCU, 32-/64-Bit

Es findet keine rekursive Laufwerkssuche statt.

Wenn eine gültige Installation gefunden wird, wird der Pfad in das Eingabefeld übernommen. Dauerhaft gespeichert wird er erst mit **Speichern**.

### NetSupport prüfen…

Das Prüffenster zeigt alle bekannten Kandidaten mit:

- gefunden / nicht gefunden
- Erkennungsquelle
- Produktname
- Produktversion
- Dateiversion
- Hersteller
- Pfad

Ein gültiger Kandidat kann bewusst über **Pfad übernehmen** gewählt werden.

Die Prüfung startet weder `PCICTLUI.EXE` noch eine Remoteverbindung.

### Neue Konfiguration

Bei einem ganz neuen Benutzerprofil verwendet auch `ConfigService` die Installationserkennung, um einen vorhandenen lokalen NetSupport-Control-Pfad als Ausgangswert zu setzen.

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

Es werden keine computerweiten Registry-Werte und keine Gruppenrichtlinien geändert.

Wenn **Beim Start minimiert** zusätzlich aktiviert ist, startet die Anwendung beim Windows-Login direkt im Infobereich.

---

## Diagnoseprotokoll

Das optionale Diagnoseprotokoll liegt unter:

```text
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

Das Protokoll erfasst für die Fehlersuche unter anderem:

- Programmstart und Programmende
- Änderungen wichtiger Einstellungen
- Start von NetSupport-Aktionen
- tatsächlich erzeugte NetSupport-Kommandozeile
- gestartete `PCICTLUI.EXE`-Prozess-ID
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

Hostnamen und die gestartete Verwaltungsaktion sind Teil der lokalen technischen Diagnose.

### Rotation

`application.log` wird bei ungefähr 2 MB nach

```text
application.log.1
```

rotiert.

Ein Fehler im Diagnose-Logging darf die Remoteverwaltung nicht blockieren.

---

## Systemzustand

Unter **Einstellungen → Diagnose und Support → Systemzustand** kann der Admin-PC lokal geprüft werden.

Aktuelle Checks:

- Remotezugriffsrichtlinie = NetSupport-only
- AppData-Verzeichnis beschreibbar
- konfigurierte `PCICTLUI.EXE` vorhanden
- NetSupport-Produkt-/Dateiversion
- bei defektem Pfad: alternative lokale NetSupport-Installation vorhanden
- RSAT / ActiveDirectory-PowerShell-Modul
- lokale CIM-/WSMan-Grundfunktion
- Autostartzustand
- Diagnoseprotokollzustand

Es werden dabei keine Zielrechner gescannt.

Details: [`SYSTEM_HEALTH.md`](SYSTEM_HEALTH.md).

---

## Supportpaket

Unter **Einstellungen → Diagnose und Support** steht **Supportpaket erstellen…** zur Verfügung.

Die Anonymisierung ist standardmäßig aktiviert.

Das ZIP enthält:

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
- Gruppen und Ansichten werden abstrahiert
- `configuration-summary.json` enthält `remoteAccessPolicy = NetSupport-only`
- NetSupport-Produkt-/Dateiversion und Erkennungsstatus werden aufgenommen
- der vollständige NetSupport-Installationspfad wird in der bereinigten Zusammenfassung nicht zusätzlich benötigt
- das Paket wird nur an den ausgewählten Speicherort geschrieben

Die Anonymisierung des tatsächlich erzeugten ZIPs wird zusätzlich automatisiert getestet: bekannte Zielnamen, Hostnamen, Gruppen und der NetSupport-Profilname dürfen bei aktiver Anonymisierung nicht als Klartext enthalten sein; `settings.json` darf nicht als ZIP-Eintrag vorkommen.

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

Die Datei wird semikolongetrennt und als UTF-8 mit BOM geschrieben.

---

## Schutz vor falscher EXE

Selbst wenn `settings.json` manuell verändert wurde, startet `NetSupportProvider` nicht beliebige Programme.

Vor jeder Aktion wird geprüft:

- Pfad lässt sich vollständig auflösen
- Datei existiert
- Dateiname ist exakt `PCICTLUI.EXE`

Ein anderer Dateiname wird mit Fehler abgewiesen.

---

## Konfigurationsdatei, Migration und Recovery

Die Programmeinstellungen liegen unter:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

Zusätzlich hält die Anwendung eine normalisierte Recovery-Kopie unter:

```text
%AppData%\NetSupportRemoteAdmin\settings.json.bak
```

Beispiel:

```json
{
  "schemaVersion": 1,
  "netSupportExecutable": "C:\\Program Files (x86)\\NetSupport\\NetSupport Manager\\PCICTLUI.EXE",
  "startMinimized": true,
  "diagnosticLoggingEnabled": true,
  "targets": [],
  "savedViews": []
}
```

### Zentrale Normalisierung

Vor der Nutzung und vor jedem Speichern normalisiert `ConfigNormalizer` die Konfiguration:

- Remote-Provider wird auf `netsupport` festgelegt
- ungültige/alte Standardaktionen fallen auf `Control` zurück
- Zielnamen/Hosts und optionale Textwerte werden bereinigt
- fehlende Listen werden repariert
- unversionierte Altdateien werden auf die aktuelle Schema-Version gehoben
- unbekannte frühere RDP-Felder gehören nicht mehr zum Datenmodell und werden beim nächsten Speichern nicht mehr geschrieben

### Schema-Version

Unversionierte bisherige Dateien werden als Schema `0` behandelt und aktuell auf Schema `1` migriert.

Eine Konfigurationsdatei mit einer **höheren** Schema-Version als die laufende Anwendung wird absichtlich abgewiesen. Eine ältere EXE darf eine Konfiguration aus einer neueren Programmversion nicht stillschweigend überschreiben oder zurückmigrieren.

### Atomares Speichern

`settings.json` wird nicht mehr direkt während der Serialisierung überschrieben. Stattdessen wird zuerst vollständig in eine temporäre Datei im selben Ordner geschrieben und diese anschließend über die Zieldatei bewegt.

Danach wird aus demselben bereits normalisierten Payload `settings.json.bak` geschrieben. Dadurch enthält auch das Backup keine unbekannten alten RDP-Felder.

Bleiben durch einen abrupten Prozessabbruch temporäre Dateien zurück, werden diese beim nächsten Speichern nicht als Konfiguration verwendet.

### Recovery

Wenn `settings.json` beschädigt oder nicht mehr lesbar ist, versucht die Anwendung automatisch `settings.json.bak` zu laden und repariert daraus die Primärdatei.

Fehlt die Primärdatei vollständig, aber ein gültiges Backup ist vorhanden, wird ebenfalls aus dem Backup wiederhergestellt.

Sind Primärdatei **und** Backup unlesbar, erfolgt keine stille Rücksetzung auf Defaults; stattdessen wird ein klarer Konfigurationsfehler ausgelöst.

Der Windows-Autostart wird nicht in `settings.json`, sondern im HKCU-Run-Key verwaltet.

---

## Reichweitenstatus

Die Statusprüfung verwendet Ping/ICMP. Ein fehlgeschlagener Ping beweist nicht, dass ein Rechner ausgeschaltet ist, da ICMP durch Firewall oder Netzrichtlinien blockiert sein kann.

Daher zeigt die Oberfläche für einen fehlgeschlagenen Ping bewusst:

```text
Nicht erreichbar
```

und nicht mehr die stärkere Aussage `Offline`.

---

## Fehlerdiagnose

Bei einem reproduzierbaren Problem:

1. Testbuild/Commit notieren
2. **NetSupport prüfen…** öffnen und Version/Pfad kontrollieren
3. **Systemzustand** ausführen
4. Fehler reproduzieren
5. Supportpaket mit aktivierter Anonymisierung erzeugen
6. ZIP kurz auf unerwünschte interne Daten prüfen
7. bei NetSupport-Problemen `application.log` auf CLI und Prozess-ID prüfen
8. bei Bedarf testen, ob dieselbe `PCICTLUI.EXE` außerhalb des Frontends mit demselben Ziel grundsätzlich funktioniert
9. bei AD/CIM-Problemen RSAT bzw. WSMan getrennt prüfen

Wenn kein Supportpaket benötigt wird, können alternativ `application.log` und die sichtbare Fehlermeldung betrachtet werden.
