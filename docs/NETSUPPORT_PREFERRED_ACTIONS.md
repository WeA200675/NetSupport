# Bevorzugte NetSupport-Aktion pro Rechner

> Diese Datei beschreibt die Standardaktion für gespeicherte Rechner in **NetSupport Remote Admin**.

---

## Ziel

Nicht jeder Rechner wird im Alltag gleich benutzt.

Beispiele:

- normaler Arbeitsplatz → **Steuern**
- Server oder sensible Anzeige → **Nur ansehen**
- Wartungsrechner → **Remote CMD**
- häufige Dateiablage → **Dateien**

Darum kann ein gespeicherter Rechner zusätzlich zu Favorit und Gruppe eine bevorzugte NetSupport-Aktion erhalten.

---

## Speicherformat

Beispiel in:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

```json
{
  "name": "PC-001",
  "host": "PC-001",
  "preferredProviderId": "netsupport",
  "preferredAction": "Control"
}
```

`preferredProviderId` bleibt gemäß Domänenrichtlinie immer `netsupport`.

`preferredAction` steuert ausschließlich, **welche NetSupport-Funktion** als Standard verwendet wird.

---

## Unterstützte Werte

| Bedienung | `preferredAction` | NetSupport-Aufruf |
|---|---|---|
| Steuern | `Control` | `/vc /e` |
| Nur ansehen | `View` | `/v /e` |
| Chat | `Chat` | `/a /ea` |
| Inventar | `Inventory` | `/i /ei` |
| Remote CMD | `CommandPrompt` | `/m /em` |
| Dateien | `FileTransfer` | `/x /ex` |

Es wird dadurch **kein weiterer Remote-Provider** eingeführt. Alle Werte führen ausschließlich zu `NetSupportProvider` / `PCICTLUI.EXE`.

---

## Auswahl in der Oberfläche

Die bestehende Auswahl

```text
Erweitert → Aktion
```

wird für einen ausgewählten Rechner mit dessen gespeicherter Standardaktion vorbelegt.

Der Standard-Button zeigt die aktuell im Editor gewählte Aktion, beispielsweise:

```text
Standardaktion: Remote CMD
```

---

## Testen ohne Speichern

Eine im Aktionsfeld geänderte Auswahl kann über den Standard-Button sofort ausprobiert werden.

Wichtig:

- die Aktion wird dadurch **nicht** automatisch gespeichert
- Favorit/Gruppe/Standardaktion bleiben unverändert auf Datenträger
- erst **Speichern / Aktualisieren** schreibt `preferredAction` nach `settings.json`

Damit bleibt das bereits im Projekt verwendete Prinzip erhalten: Ein Verbindungsstart darf keine noch nicht bestätigte Zielkonfiguration nebenbei persistieren.

---

## Doppelklick

Ein Doppelklick auf einen Rechner verwendet absichtlich die **zuletzt gespeicherte** `preferredAction`.

Beispiel:

1. gespeichert ist `View`
2. im Editor wird testweise `CommandPrompt` ausgewählt
3. **Standardaktion: Remote CMD** startet den Test per Remote CMD
4. ohne **Speichern / Aktualisieren** bleibt der gespeicherte Wert `View`
5. ein späterer Doppelklick verwendet wieder `View`

Dadurch bleibt Doppelklick reproduzierbar.

---

## Kompatibilität / Fallback

Ältere Konfigurationen enthalten noch kein `preferredAction`.

Dann gilt automatisch:

```text
Control
```

Auch ein unbekannter oder ungültiger Wert fällt auf `Control` zurück.

Damit ändert das neue Feld das bisherige Verhalten vorhandener Rechner nicht.

---

## Sicherheit und Domänenrichtlinie

Die NetSupport-only-Regel bleibt unverändert:

```text
Remote-Provider = netsupport
```

`preferredAction` kann nur eine der ausdrücklich unterstützten `RemoteAction`-Varianten auswählen.

Es kann damit weder RDP noch ein beliebiges Programm oder ein anderer Remote-Provider aktiviert werden.

Der eigentliche `NetSupportProvider` validiert weiterhin `PCICTLUI.EXE` sowie Rechnername/IP vor jedem Start.

---

## Manuelle Prüfschritte

1. Rechner auswählen.
2. Unter **Erweitert → Aktion** `Nur ansehen` wählen.
3. Standard-Button drücken: View soll sofort starten.
4. Anwendung neu auswählen/neu starten, ohne zu speichern: bisherige gespeicherte Aktion muss erhalten bleiben.
5. erneut `Nur ansehen` wählen und **Speichern / Aktualisieren** drücken.
6. `settings.json` prüfen: `"preferredAction": "View"`.
7. Rechner doppelklicken: View muss verwendet werden.
8. `preferredAction` testweise manuell auf einen ungültigen Wert setzen.
9. Anwendung starten: Standardaktion muss auf **Steuern / Control** zurückfallen.

---

## Architektur

Die Eigenschaft liegt direkt am Zielmodell:

```text
RemoteTarget.PreferredAction
```

Die zusätzliche Bedienlogik ist separat gekapselt in:

```text
MainWindow.PreferredAction.cs
```

Damit bleibt die bereits große Hauptfensterdatei übersichtlicher und die NetSupport-only-Basislogik muss für diese Komfortfunktion nicht umgebaut werden.
