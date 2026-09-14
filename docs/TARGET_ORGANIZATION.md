# Rechner organisieren: Favoriten, Gruppen und Standardverbindung

> Diese Datei beschreibt die Organisationsfunktionen für größere Rechnerbestände in **NetSupport Remote Admin**.

---

## Ziel

Bei ungefähr 40 oder mehr Zielrechnern soll nicht jedes Mal über eine lange unsortierte Liste und anschließend über die gewünschte Fernsteuerung navigiert werden müssen.

Dafür kann ein dauerhaft gespeicherter Rechner jetzt drei zusätzliche, nicht geheime Eigenschaften erhalten:

```json
{
  "isFavorite": true,
  "group": "Büro",
  "preferredProviderId": "netsupport"
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

- Favoriten werden in der Rechnerliste vor normalen Rechnern sortiert.
- In der Liste erscheint ein `★`.
- Über **Nur Favoriten** kann die Liste auf diese Rechner reduziert werden.
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

Die Gruppenbezeichnung ist absichtlich nur Text und benötigt keine separate Gruppenverwaltung.

Verwendung:

- Die normale Suche berücksichtigt den Gruppennamen.
- Oben in der Oberfläche steht zusätzlich ein Gruppenfilter bereit.
- Die Liste wird innerhalb der Favoriten nach Gruppe und anschließend nach Rechnername sortiert.
- Neue Gruppennamen werden nach dem Speichern automatisch in den Gruppenfilter aufgenommen.

Rechner, die nur vorübergehend aus Active Directory geladen wurden, erhalten nicht automatisch eine Gruppe.

---

## Bevorzugter Remote-Provider

Ein Ziel kann einen **Standard-Provider** erhalten.

Aktuell sind je nach Installation typischerweise verfügbar:

```text
netsupport
rdp
```

Die technische Einstellung wird als `preferredProviderId` gespeichert.

Beispiele:

```json
"preferredProviderId": "netsupport"
```

oder

```json
"preferredProviderId": "rdp"
```

Der Benutzer sieht in der Oberfläche den lesbaren Providernamen statt der internen ID.

---

## Standardverbindung

Die Schaltfläche **Standardverbindung starten** verwendet den für diesen Rechner gewählten Provider und startet dessen normale `Control`-Aktion.

Ein Doppelklick auf einen Rechner verwendet ebenfalls die Standardverbindung.

Die direkten Schnellaktionen bleiben trotzdem erhalten. Man kann also beispielsweise einen Rechner standardmäßig per RDP öffnen und bei Bedarf weiterhin explizit **Steuern** anklicken, um NetSupport Control zu verwenden.

---

## Fallback-Verhalten

Ein gespeicherter Provider kann auf einem anderen Admin-PC fehlen. Deshalb darf ein alter oder nicht verfügbarer `preferredProviderId` die Bedienung nicht blockieren.

Die Auswahl erfolgt in dieser Reihenfolge:

1. gespeicherter, verfügbarer Provider mit `Control`-Unterstützung
2. NetSupport, falls verfügbar
3. erster anderer verfügbarer Provider mit `Control`-Unterstützung

Wenn überhaupt kein geeigneter Provider vorhanden ist, zeigt die Anwendung eine Statusmeldung statt abzustürzen.

---

## Speichern und Aktualisieren

Die frühere Schaltfläche **Speichern** wurde zu

```text
Speichern / Aktualisieren
```

erweitert.

Dadurch können bestehende Ziele jetzt direkt geändert werden.

Persistiert werden unter anderem:

- Name / Host / Beschreibung
- Favorit
- Gruppe
- bevorzugter Provider
- RDP-Benutzername und Domäne
- Zwischenablagepräferenz
- Admin-Sitzung
- Multi-Monitor-Präferenz

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

Favoriten, Gruppen und Standard-Provider gehören in die lokale Bedienkonfiguration, weil sie persönliche bzw. administrative Arbeitsorganisation darstellen.

Active Directory bleibt die Quelle für Domänenrechner. Das Tool versucht nicht, diese Organisationsinformationen zurück in AD zu schreiben und baut auch keine zweite zentrale Inventardatenbank auf.

Die zusätzlichen Felder sind bewusst einfache Eigenschaften von `RemoteTarget`, sodass später beispielsweise andere Discovery-Quellen oder weitere Remote-Provider dieselbe Organisationslogik weiterverwenden können.
