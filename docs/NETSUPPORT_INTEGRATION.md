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

## Installation und Erkennung

Verwendet wird das NetSupport-Manager-Control:

```text
PCICTLUI.EXE
```

Die lokale Installationserkennung liegt hinter:

```text
INetSupportInstallationService
   |
   +--> NetSupportInstallationService
```

Geprüft werden bewusst nur lokale, nachvollziehbare Quellen:

1. aktuell konfigurierter Pfad
2. `%ProgramFiles(x86)%\NetSupport\NetSupport Manager\PCICTLUI.EXE`
3. `%ProgramFiles%\NetSupport\NetSupport Manager\PCICTLUI.EXE`
4. Windows-Uninstall-Registry in HKLM/HKCU sowie 32-/64-Bit-Sicht, wenn dort **NetSupport Manager** registriert ist

Es findet **keine rekursive Laufwerkssuche** statt.

### Neue Einstellungsfunktionen

Unter **Erweitert → Einstellungen → NetSupport Manager** stehen zur Verfügung:

- **Durchsuchen…** – Pfad manuell auswählen
- **Automatisch erkennen** – besten lokal gefundenen gültigen Pfad übernehmen
- **NetSupport prüfen…** – gefundene Kandidaten mit Quelle und Versionsinformationen anzeigen und bewusst auswählen

Das Prüffenster führt keine Remoteverbindung und keinen Netzwerkscan aus.

### Ausgelesene Dateiinformationen

Wenn `PCICTLUI.EXE` vorhanden ist, werden best-effort gelesen:

- Produktname
- Produktversion
- Dateiversion
- Hersteller
- letzter Änderungszeitpunkt
- Erkennungsquelle

Der Systemzustand verwendet dieselbe Erkennung.

---

## Default-Konfiguration

Bei einer neuen Konfiguration verwendet `ConfigService` ebenfalls `NetSupportInstallationService`.

Dadurch kann ein neuer Admin-PC bereits beim ersten Start einen vorhandenen lokalen NetSupport-Control-Pfad übernehmen, auch wenn die Installation über eine Registry-Quelle statt nur über einen Standardordner gefunden wird.

Eine bestehende manuell gespeicherte Konfiguration wird nicht ungefragt überschrieben; dafür stehen die Erkennungsfunktionen im Einstellungsfenster bereit.

---

## Schutz des ausführbaren Programms

`NetSupportProvider` akzeptiert nicht mehr irgendeine in `settings.json` eingetragene EXE.

Vor jedem Start wird geprüft:

- Pfad ist syntaktisch auflösbar
- Datei existiert
- Dateiname ist exakt `PCICTLUI.EXE` (Groß-/Kleinschreibung unerheblich)

Damit kann eine manipulierte Konfigurationsdatei nicht dazu verwendet werden, über den Remote-Provider ein beliebiges anderes Programm mit unseren Argumenten zu starten.

Andere Programme werden mit einer verständlichen Fehlermeldung abgewiesen.

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
/c">address"
```

Beispiel:

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

```text
10.20.30.40
```

wird zu:

```text
/c">10.20.30.40"
```

`IPAddress.TryParse` übernimmt die Unterscheidung zwischen IP und Rechnername.

### Rechnername / FQDN

Beispiele:

```text
PC-001
PC-001.example.local
```

Für Namen akzeptiert die Anwendung nur einen begrenzten DNS-/NetBIOS-artigen Zeichensatz:

```text
A-Z a-z 0-9 . _ -
```

Anführungszeichen, Zeilenumbrüche und beliebiger zusätzlicher Kommandozeilentext werden vor `Process.Start` abgewiesen.

---

## Warum `Arguments` statt `ArgumentList`?

Die NetSupport-IP-Syntax enthält absichtlich eingebettete Anführungszeichen:

```text
/c">10.0.0.1"
```

`ProcessStartInfo.ArgumentList` escaped Argumente selbstständig für Windows. Dadurch könnte die für NetSupport relevante kompakte Form verändert werden.

Deshalb wird nach strenger Zielvalidierung eine kontrollierte rohe Argumentzeichenfolge über

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

Wenn das Diagnoseprotokoll aktiviert ist, werden unter anderem protokolliert:

```text
PCICTLUI.EXE /c PC-001 /vc /e
```

oder bei IP:

```text
PCICTLUI.EXE /c">10.20.30.40" /vc /e
```

Nach erfolgreichem `Process.Start` wird zusätzlich die gestartete Prozess-ID protokolliert.

Hostname/IP gehören zur lokalen technischen Diagnose. Das anonymisierte Supportpaket ersetzt bekannte Zielkennungen dagegen durch `target-...`-Aliase.

---

## Systemzustand

Der lokale Systemzustand unterscheidet jetzt:

1. **konfigurierter Pfad gültig** – OK inklusive Produkt-/Dateiversion
2. **konfigurierter Pfad ungültig, alternative Installation gefunden** – Hinweis mit gefundenem Pfad
3. **keine Installation auffindbar** – Fehler mit Handlungshinweis

Dadurch lässt sich ein Admin-PC auch dann reparieren, wenn `settings.json` noch auf eine alte Installation zeigt.

---

## Supportpaket

`configuration-summary.json` enthält zusätzlich nicht geheime NetSupport-Metadaten:

- Control-Pfad konfiguriert: ja/nein
- konfigurierter Control-Pfad vorhanden: ja/nein
- alternative Installation gefunden: ja/nein
- Produktname
- Produktversion
- Dateiversion
- Hersteller

Der vollständige NetSupport-Installationspfad wird in der bereinigten Support-Zusammenfassung nicht benötigt und nicht zusätzlich aufgenommen.

---

## Fehlerfälle

### `PCICTLUI.EXE` fehlt

Die Anwendung startet keinen alternativen Remote-Provider. Der Systemzustand versucht zunächst, eine andere lokale NetSupport-Installation zu finden und zeigt ansonsten einen Fehler.

### Falsche EXE konfiguriert

Der Provider verweigert den Start, wenn der Dateiname nicht `PCICTLUI.EXE` ist – selbst wenn die Datei existiert.

### Ungültiger Zielwert

Ein Ziel mit unerlaubten Zeichen wird **vor** dem Prozessstart abgewiesen.

### NetSupport selbst lehnt die Verbindung ab

Der Prozessstart kann technisch erfolgreich sein, obwohl NetSupport den Client später nicht erreicht oder die Verbindung aufgrund seiner eigenen Konfiguration/Berechtigung ablehnt.

Der lokale Verlauf dokumentiert deshalb den **Startversuch**, nicht den vollständigen Erfolg oder die Dauer einer NetSupport-Sitzung. Für verbindliche Sitzungsnachvollziehbarkeit bleibt die vorhandene NetSupport-/Unternehmenskonfiguration maßgeblich.

---

## Nächste NetSupport-spezifische Ausbauschritte

- optional Installationsordner auf notwendige NetSupport-Begleitdateien prüfen
- besser unterscheiden zwischen `PCICTLUI.EXE`-Startfehler und späterem NetSupport-Verbindungsfehler
- bei Bedarf freigegebene NetSupport-Konfigurationsprofile (`/N`, `/F`) explizit integrieren
- optional bekannte NetSupport-Versionen/Abweichungen im Supportpaket gegeneinander vergleichbar machen

Konfigurationsprofile werden nur ergänzt, wenn klar ist, welches Profil administrativ vorgesehen ist; die Anwendung soll vorhandene NetSupport-Sicherheitsvorgaben nicht verändern.
