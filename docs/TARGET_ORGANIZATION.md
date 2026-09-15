# Rechner organisieren: Favoriten, Gruppen und Standardaktion

> Diese Datei beschreibt die Organisationsfunktionen für größere Rechnerbestände in **NetSupport Remote Admin**.

---

## Ziel

Bei ungefähr 40 oder mehr Zielrechnern soll nicht jedes Mal über eine lange unsortierte Liste navigiert und anschließend dieselbe NetSupport-Aktion erneut gewählt werden müssen.

Ein dauerhaft gespeicherter Rechner kann organisatorische Eigenschaften erhalten:

```json
{
  "isFavorite": true,
  "group": "Büro",
  "preferredProviderId": "netsupport",
  "preferredAction": "Control"
}
```

Diese Werte gehören zur lokalen Bedienkonfiguration und werden in

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

gespeichert.

---

## Favoriten

Ein Rechner kann als **Favorit** markiert werden.

Auswirkungen:

- Favoriten werden vor normalen Rechnern sortiert.
- In der Liste erscheint ein `★`.
- **Nur Favoriten** reduziert die Liste auf diese Rechner.
- Der Favoritenstatus wird erst durch **Speichern / Aktualisieren** dauerhaft gespeichert.

Favoriten verändern weder Active Directory noch NetSupport-Konfigurationen.

---

## Gruppen

Jeder gespeicherte Rechner kann optional einer frei benannten Gruppe zugeordnet werden, zum Beispiel:

- Büro
- Werkstatt
- Verwaltung
- Server
- Schulungsraum
- Testgeräte

Verwendung:

- Die normale Suche berücksichtigt den Gruppennamen.
- Ein Gruppenfilter steht separat zur Verfügung.
- Innerhalb der Favoriten wird nach Gruppe und anschließend Rechnername sortiert.
- Neue Gruppennamen erscheinen nach dem Speichern automatisch im Gruppenfilter.

Rechner, die nur vorübergehend aus Active Directory geladen wurden, erhalten nicht automatisch eine Gruppe.

---

## Standard-Provider

Die Datenstruktur enthält weiterhin `preferredProviderId`, damit die Provider-Abstraktion sauber bleibt.

Für diese Domäne gilt jedoch immer:

```text
preferredProviderId = netsupport
```

Andere bzw. alte Provider-IDs werden beim Programmstart auf `netsupport` normalisiert.

Hintergrund: Die Domänenrichtlinie erlaubt Fernwartung ausschließlich über NetSupport Manager. Details: [`DOMAIN_REMOTE_POLICY.md`](DOMAIN_REMOTE_POLICY.md).

---

## Bevorzugte NetSupport-Aktion

Zusätzlich kann pro Rechner eine **Standardaktion** gespeichert werden.

Unterstützte Werte:

| Anzeige | Gespeicherter Wert | Bedeutung |
|---|---|---|
| Steuern | `Control` | NetSupport Control |
| Nur ansehen | `View` | View ohne Steuerung |
| Remote CMD | `CommandPrompt` | Remote Command Prompt |
| Dateien | `FileTransfer` | Dateiübertragung |
| Inventar | `Inventory` | NetSupport Inventory |
| Chat | `Chat` | Chat |

Die Auswahl erfolgt über die bereits vorhandene **Aktion**-Auswahl unter **Erweitert**.

Der sichtbare Standard-Button zeigt die aktuelle Auswahl, zum Beispiel:

```text
Standardaktion: Nur ansehen
```

### Fallback

Fehlt `preferredAction` in einer älteren Konfiguration oder enthält das Feld einen unbekannten Wert, verwendet die Anwendung automatisch:

```text
Control
```

Damit bleiben vorhandene Rechnerkonfigurationen kompatibel.

---

## Standardaktion starten

Die Schaltfläche **Standardaktion: …** verwendet die aktuell im Editor gewählte NetSupport-Aktion.

Das ist absichtlich getrennt von der Persistenz:

- eine geänderte Aktion kann sofort ausprobiert werden
- dadurch wird die Zielkonfiguration noch nicht verändert
- dauerhaft wird die Auswahl erst mit **Speichern / Aktualisieren**

Ein Doppelklick auf einen Rechner ist konservativer: Er verwendet immer die **zuletzt gespeicherte** `preferredAction`. Damit führt eine noch nicht gespeicherte Änderung nicht versehentlich zu einem anderen Doppelklick-Verhalten.

Die direkten Schnellaktionen bleiben unabhängig davon weiterhin verfügbar.

---

## Verhalten bei fehlendem NetSupport

Wenn `PCICTLUI.EXE` auf dem Admin-PC fehlt oder der konfigurierte Pfad ungültig ist, wird **kein alternativer Remote-Provider** verwendet.

Die Anwendung zeigt stattdessen einen verständlichen Fehler bzw. der Systemzustand markiert NetSupport als nicht verfügbar.

Das ist beabsichtigt: Ein technischer Fallback darf die Domänenrichtlinie nicht umgehen.

---

## Speichern und Aktualisieren

**Speichern / Aktualisieren** kann bestehende Ziele direkt ändern.

Persistiert werden:

- Name
- Host
- Beschreibung
- Favorit
- Gruppe
- `preferredProviderId = netsupport`
- `preferredAction`

Nicht gespeichert werden flüchtige Informationen wie:

- Online-/Offline-Status
- aktuelle IP-Adressen
- aktuell angemeldeter Benutzer
- Windows-Version aus der Laufzeitabfrage
- Zeitpunkt der letzten Statusprüfung

---

## Filter und Sortierung

Die Rechnerliste kann gleichzeitig mit mehreren Kriterien eingeschränkt werden:

```text
Textsuche + Gruppenfilter + Nur Favoriten
```

Die Textsuche berücksichtigt:

- Rechnername
- Hostname
- Beschreibung
- Gruppe

Sortierreihenfolge:

1. Favoriten zuerst
2. Rechner mit Gruppe vor Rechnern ohne Gruppe
3. Gruppenname alphabetisch
4. Rechnername alphabetisch

---

## Designentscheidung

Favoriten, Gruppen und die bevorzugte NetSupport-Aktion gehören in die lokale Bedienkonfiguration, weil sie administrative Arbeitsorganisation darstellen.

Active Directory bleibt die Quelle für Domänenrechner. Das Tool schreibt diese Organisationsinformationen nicht nach AD zurück und baut keine zweite zentrale Inventardatenbank auf.

Die Provider-Abstraktion bleibt als technische Architektur erhalten. Ein weiterer Remote-Provider darf jedoch nur nach ausdrücklicher Freigabe durch die Domänen-/Sicherheitsvorgaben registriert werden.
