# Konfiguration – Migration, Speicherung und Recovery

> Technische Referenz für den Lebenszyklus von `settings.json` in **NetSupport Remote Admin**.

---

## Dateien

Primäre Konfiguration:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

Normalisierte Recovery-Kopie:

```text
%AppData%\NetSupportRemoteAdmin\settings.json.bak
```

Temporäre Dateien werden ausschließlich während eines Schreibvorgangs im selben Ordner angelegt und nach erfolgreichem Austausch wieder entfernt.

---

## Schema-Version

Die persistierte Konfiguration enthält eine explizite Versionsnummer:

```json
{
  "schemaVersion": 1
}
```

Aktuell:

```text
CurrentSchemaVersion = 1
```

Unversionierte bisherige Konfigurationen des Projekts werden beim Einlesen als Schema `0` erkannt.

`ConfigNormalizer` migriert sie auf die aktuelle Version, bevor die restliche Anwendung die Werte verwendet.

### Neue Programmversion vs. alte Programmversion

Enthält eine Datei eine **höhere** Schema-Version als die laufende EXE unterstützt, wird sie nicht automatisch heruntergestuft.

Die Anwendung bricht mit einer klaren Meldung ab.

Ziel: Eine ältere Programmversion darf eine von einer neueren Version geschriebene Konfiguration nicht stillschweigend verändern oder Daten verlieren.

---

## Zentrale Normalisierung

`ConfigService` führt die Konfiguration nach dem Laden und vor jedem Speichern durch:

```text
ConfigNormalizer.Normalize(...)
```

Aktuelle Regeln:

- `Targets` und `SavedViews` werden bei ungültigem `null` wieder als leere Listen hergestellt
- leerer NetSupport-Pfad wird zu `null`
- leerer NetSupport-Profilname wird zu `null`
- Zielnamen und Hosts werden getrimmt
- optionale Beschreibung und Gruppe werden normalisiert
- `preferredProviderId` wird immer auf `netsupport` gesetzt
- unbekannte/ungültige `preferredAction` fällt auf `Control` zurück
- gültige Aktionsnamen werden kanonisch gespeichert (`View`, `CommandPrompt`, ...)
- Schema-Version wird auf die aktuelle Version gesetzt

Damit gelten NetSupport-only und die Standardaktionsregeln nicht nur im UI, sondern bereits auf Datenebene.

---

## Migration früherer RDP-Entwicklungsstände

Frühere Entwicklungsstände enthielten experimentelle RDP-Felder.

Diese Felder existieren nicht mehr im aktuellen `AppConfig`-/`RemoteTarget`-Modell. `System.Text.Json` ignoriert unbekannte Felder beim Laden.

Beim nächsten Speichern wird ausschließlich das aktuelle Modell serialisiert. Dadurch verschwinden beispielsweise alte Werte wie:

```text
useEmbeddedRdp
useFullScreenRdp
rdpUserName
rdpDomain
rdpUseMultiMonitor
```

Zusätzlich wird ein früherer

```text
preferredProviderId = rdp
```

zu

```text
preferredProviderId = netsupport
```

normalisiert.

Das wird automatisiert getestet.

---

## Atomisches Schreiben

`settings.json` wird nicht direkt während der JSON-Serialisierung überschrieben.

Ablauf:

```text
AppConfig
   |
   +--> Normalize
   |
   +--> vollständig zu UTF-8 serialisieren
   |
   +--> .settings.json.<GUID>.tmp schreiben + flush
   |
   +--> Temp-Datei über settings.json bewegen
   |
   +--> denselben normalisierten Payload atomar nach settings.json.bak schreiben
```

Die temporäre Datei liegt im selben Verzeichnis wie das Ziel. Damit findet der abschließende Austausch auf demselben Volume statt.

Eine teilweise geschriebene Temp-Datei wird nie als `settings.json` verwendet.

---

## Warum das Backup ebenfalls normalisiert ist

`settings.json.bak` ist **keine rohe Kopie** der vorherigen Konfiguration.

Stattdessen wird sie aus demselben bereits normalisierten JSON-Payload wie die Primärdatei geschrieben.

Dadurch konserviert die Recovery-Datei nicht versehentlich veraltete RDP-Felder oder alte Providerwerte.

---

## Automatische Recovery

### Primärdatei beschädigt

Wenn `settings.json` nicht als gültiges aktuelles Konfigurationsmodell gelesen werden kann:

1. `settings.json.bak` wird geprüft.
2. Ist das Backup gültig, wird es normalisiert geladen.
3. Die Primärdatei wird daraus neu geschrieben.
4. Die Anwendung arbeitet mit dem wiederhergestellten Stand weiter.

### Primärdatei fehlt

Fehlt `settings.json`, aber ein gültiges `settings.json.bak` ist vorhanden, wird ebenfalls daraus wiederhergestellt.

### Beide Dateien beschädigt

Sind Primärdatei und Backup unlesbar, erfolgt **keine stille Rücksetzung auf Defaults**.

Stattdessen wird ein klarer Konfigurationsfehler ausgelöst. Damit wird ein Datenverlust nicht durch eine scheinbar erfolgreiche Default-Konfiguration verdeckt.

---

## Single Instance

Die Anwendung verwendet zusätzlich einen benannten Mutex pro Windows-Sitzung:

```text
Local\NetSupport.RemoteAdmin.SingleInstance
```

Dadurch läuft normalerweise nur eine Instanz der Tray-Anwendung pro Sitzung.

Das reduziert konkurrierende Schreibvorgänge auf Konfiguration und lokalen Verlauf. Ein zweiter Start zeigt lediglich an, dass die Anwendung bereits läuft.

---

## Automatisierte Tests

Der CI-Testblock prüft unter anderem:

- Legacy-Provider `rdp` wird zu `netsupport`
- ungültige Standardaktion wird zu `Control`
- Groß-/Kleinschreibung gültiger Aktionen wird kanonisiert
- `null`-Listen aus fehlerhaften/alten Dateien werden repariert
- frühere RDP-Felder verschwinden beim erneuten Serialisieren
- unversionierte Dateien werden auf die aktuelle Schema-Version gehoben
- eine zukünftige, nicht unterstützte Schema-Version wird abgelehnt
- Primärdatei und Backup werden geschrieben
- beide Dateien enthalten normalisierte Daten
- beschädigte Primärdatei wird aus Backup wiederhergestellt
- fehlende Primärdatei wird aus Backup wiederhergestellt
- zwei beschädigte Dateien führen zu einem klaren Fehler
- erfolgreiche Schreibvorgänge hinterlassen keine Temp-Dateien
- Single-Instance-Mutex erlaubt nur einen Besitzer und wird nach Dispose wieder frei

Siehe auch [`AUTOMATED_TESTS.md`](AUTOMATED_TESTS.md).

---

## Sicherheitsgrenze

Diese Mechanismen schützen die **lokale Bedienkonfiguration** der Anwendung.

Sie ersetzen keine NetSupport-Sicherheitsrichtlinien, keine Windows-Dateirechte und keine Unternehmens-Backups.

Kennwörter werden weiterhin nicht von dieser Anwendung in `settings.json` verwaltet.
