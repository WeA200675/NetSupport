# NetSupport-Manager-Integration

> Technische Beschreibung der produktiven Remote-Control-Anbindung von **NetSupport Remote Admin**.

---

## Betriebsregel

In dieser Domäne ist **NetSupport Manager** der einzige freigegebene Fernwartungsweg.

```text
RemoteProviderRegistry
   |
   +--> NetSupportProvider
           |
           +--> PCICTLUI.EXE
```

Es gibt keinen RDP- oder sonstigen automatischen Remote-Control-Fallback.

Details: [`DOMAIN_REMOTE_POLICY.md`](DOMAIN_REMOTE_POLICY.md).

---

## Executable

Verwendet wird das NetSupport-Manager-Control:

```text
PCICTLUI.EXE
```

Typische Installationsorte, die `ConfigService` beim ersten Start prüft:

```text
%ProgramFiles(x86)%\NetSupport\NetSupport Manager\PCICTLUI.EXE
%ProgramFiles%\NetSupport\NetSupport Manager\PCICTLUI.EXE
```

Der tatsächlich verwendete Pfad kann unter **Erweitert → Einstellungen** geändert werden.

Der **Systemzustand** zeigt zusätzlich die aus der Datei auslesbare Produkt-/Dateiversion an.

---

## Offizielle NetSupport-Kommandozeile

NetSupport dokumentiert für `PCICTLUI.EXE` unter anderem:

```text
/C ClientName | "Address"
/V
/VC
/VW
/VS
/E
/A
/I
```

Für eine TCP/IP-Adresse verwendet NetSupport die besondere Adressnotation:

```text
/ c">address"
```

ohne das Leerzeichen zwischen `/` und `c`, zum Beispiel sinngemäß:

```text
PCICTLUI.EXE /c">10.0.0.1" /vc /e
```

Die NetSupport-DNA-Dokumentation verwendet ebenfalls die Form:

```text
PCICTLUI.exe /c">%address%" /v /e
```

Offizielle Quellen:

- `https://kb.netsupportsoftware.com/knowledge-base/netsupport-manager-control-command-line-options/`
- `https://kb.netsupportsoftware.com/knowledge-base/how-to-configure-remote-control-within-netsupport-dna/`
- `https://kb.netsupportsoftware.com/knowledge-base/how-to-use-pin-connect-via-the-command-line/`

---

## Unterstützte Aktionen

Die Anwendung bildet aktuell folgende Funktionen ab:

| UI / `RemoteAction` | NetSupport-Argumente |
|---|---|
| Steuern / `Control` | `/vc /e` |
| Nur ansehen / `View` | `/v /e` |
| Chat / `Chat` | `/a /ea` |
| Inventar / `Inventory` | `/i /ei` |
| Remote CMD / `CommandPrompt` | `/m /em` |
| Dateien / `FileTransfer` | `/x /ex` |

Die `m`-/`x`-Varianten und ihre Exit-Schalter sind von NetSupport auch für die Kommandozeilenintegration dokumentiert.

---

## Zielaufbau

### IP-Adresse

Bei einer IP-Adresse wird der NetSupport-spezifische `>`-Präfix verwendet:

```text
10.20.30.40
```

wird zu:

```text
/c">10.20.30.40"
```

`IPAddress.TryParse` übernimmt die Unterscheidung zwischen IP und Rechnername.

### Rechnername / FQDN

Ein Domänenrechner wird beispielsweise als

```text
PC-001
```

oder

```text
PC-001.example.local
```

übergeben.

Die Anwendung akzeptiert für Namen nur einen begrenzten DNS-/NetBIOS-artigen Zeichensatz:

```text
A-Z a-z 0-9 . _ -
```

Anführungszeichen, Zeilenumbrüche und beliebiger zusätzlicher Kommandozeilentext sind nicht erlaubt.

---

## Warum `Arguments` statt `ArgumentList`?

Die NetSupport-IP-Syntax enthält absichtlich eingebettete Anführungszeichen:

```text
/c">10.0.0.1"
```

`ProcessStartInfo.ArgumentList` escaped Argumente selbstständig für Windows. Dadurch könnte die für NetSupport relevante kompakte Form verändert werden.

Deshalb wird nach strenger Zielvalidierung eine rohe, kontrollierte Argumentzeichenfolge erzeugt und über

```csharp
ProcessStartInfo.Arguments
```

an **direkt** gestartetes `PCICTLUI.EXE` übergeben.

```text
UseShellExecute = false
```

verhindert dabei, dass eine Shell zwischen Anwendung und NetSupport liegt.

---

## Diagnose

Wenn das Diagnoseprotokoll aktiviert ist, wird die gestartete NetSupport-CLI protokolliert, zum Beispiel:

```text
PCICTLUI.EXE /c PC-001 /vc /e
```

oder bei IP:

```text
PCICTLUI.EXE /c">10.20.30.40" /vc /e
```

Da Hostname/IP für die technische Fehlersuche relevant sind, gehören sie zum lokalen Diagnoseprotokoll. Das anonymisierte Supportpaket ersetzt bekannte Zielkennungen dagegen durch `target-...`-Aliase.

---

## Fehlerfälle

### `PCICTLUI.EXE` fehlt

Die Anwendung startet keinen alternativen Remote-Provider. Stattdessen wird ein Fehler angezeigt und der Systemzustand markiert NetSupport als nicht verfügbar.

### Ungültiger Zielwert

Ein Ziel mit unerlaubten Zeichen wird **vor** `Process.Start` abgewiesen.

Damit kann ein frei eingegebener Zielwert nicht als zusätzlicher NetSupport-/Windows-Kommandozeilentext interpretiert werden.

### NetSupport selbst lehnt die Verbindung ab

Der Prozessstart kann technisch erfolgreich sein, obwohl NetSupport den Client später nicht erreicht oder die Verbindung aufgrund seiner eigenen Konfiguration/Berechtigung ablehnt.

Der lokale Startverlauf dokumentiert deshalb den **Startversuch**, nicht den vollständigen Erfolg oder die Dauer einer NetSupport-Sitzung.

Für verbindliche Sitzungsnachvollziehbarkeit bleibt die vorhandene NetSupport-/Unternehmenskonfiguration maßgeblich.

---

## Nächste NetSupport-spezifische Ausbauschritte

- installierte NetSupport-Version zusätzlich in das Supportpaket aufnehmen
- optional Installationsordner auf notwendige NetSupport-Begleitdateien prüfen
- besser unterscheiden zwischen `PCICTLUI.EXE`-Startfehler und späterem NetSupport-Verbindungsfehler
- bei Bedarf freigegebene NetSupport-Konfigurationsprofile (`/N`, `/F`) explizit in die Anwendung integrieren

Konfigurationsprofile sollten nur ergänzt werden, wenn klar ist, welches Profil administrativ vorgesehen ist; die Anwendung soll vorhandene NetSupport-Sicherheitsvorgaben nicht verändern.
