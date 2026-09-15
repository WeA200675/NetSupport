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

### Einstellungsfunktionen

Unter **Erweitert → Einstellungen → NetSupport Manager** stehen zur Verfügung:

- **Durchsuchen…** – Pfad manuell auswählen
- **Automatisch erkennen** – besten lokal gefundenen gültigen Pfad übernehmen
- **NetSupport prüfen…** – gefundene Kandidaten mit Quelle und Versionsinformationen anzeigen und bewusst auswählen
- lokales Control-Profil auswählen
- **Profile neu laden**
- optional **Control auf dieses Profil festlegen (/F)**

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
/N Profile name
/F
/C ClientName | "Address"
/V
/VC
/VW
/VS
/E
/A
/I
```

`/N` lädt eine benannte Control-Konfiguration. `/F` wird zusammen mit `/N` verwendet, um den Control auf das gewählte Profil zu beschränken.

Für eine TCP/IP-Adresse verwendet NetSupport die besondere Adressnotation:

```text
/c">address"
```

Beispiel:

```text
PCICTLUI.EXE /c">10.0.0.1" /vc /e
```

Mit Profil:

```text
PCICTLUI.EXE /n "Helpdesk" /c PC-001 /vc /e
```

Mit Profilbindung:

```text
PCICTLUI.EXE /f /n "Helpdesk" /c PC-001 /vc /e
```

Offizielle Quellen:

- `https://kb.netsupportsoftware.com/knowledge-base/netsupport-manager-control-command-line-options/`
- `https://kb.netsupportsoftware.com/knowledge-base/how-to-configure-remote-control-within-netsupport-dna/`
- `https://kb.netsupportsoftware.com/knowledge-base/how-to-use-pin-connect-via-the-command-line/`

---

## Control-Profile

Die Profilintegration liegt hinter:

```text
INetSupportProfileService
   +--> NetSupportProfileService
```

Vorhandene Profile werden ausschließlich aus folgendem Benutzerzweig gelesen:

```text
HKCU\Software\NetSupport Ltd\PCICTL\ConfigList
```

Die Anwendung verändert diesen NetSupport-Schlüssel nicht.

Vor dem Start gilt:

- Profilname maximal 128 Zeichen
- keine Anführungszeichen
- keine Steuerzeichen/Zeilenumbrüche
- `/F` nur mit gesetztem Profil
- konfiguriertes Profil muss lokal vorhanden sein

Fehlt ein konfiguriertes Profil, wird der Start blockiert. Es gibt bewusst keinen automatischen Fallback auf das Standardprofil, da dies eine erwartete eingeschränkte Control-Konfiguration umgehen könnte.

NetSupport-Profilpasswörter werden von der Anwendung nicht gespeichert oder an die Kommandozeile angehängt. Eine Passwortabfrage bleibt bei NetSupport.

Details: [`NETSUPPORT_CONTROL_PROFILES.md`](NETSUPPORT_CONTROL_PROFILES.md).

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

Deshalb wird nach strenger Ziel- und Profilvalidierung eine kontrollierte rohe Argumentzeichenfolge über

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

mit Profil beispielsweise:

```text
PCICTLUI.EXE /f /n "Helpdesk" /c PC-001 /vc /e
```

oder bei IP:

```text
PCICTLUI.EXE /c">10.20.30.40" /vc /e
```

Nach erfolgreichem `Process.Start` wird zusätzlich die gestartete Prozess-ID protokolliert.

Hostname/IP und Profilname gehören zur lokalen technischen Diagnose. Das anonymisierte Supportpaket ersetzt bekannte Zielkennungen durch `target-...`-Aliase und den konfigurierten Profilnamen durch `netsupport-profile`.

---

## Systemzustand

Der lokale Systemzustand unterscheidet bei der Installation:

1. **konfigurierter Pfad gültig** – OK inklusive Produkt-/Dateiversion
2. **konfigurierter Pfad ungültig, alternative Installation gefunden** – Hinweis mit gefundenem Pfad
3. **keine Installation auffindbar** – Fehler mit Handlungshinweis

Zusätzlich wird das optionale Control-Profil separat geprüft:

- kein Profil = Info / NetSupport-Standardverhalten
- Profil vorhanden = OK
- Profil fehlt, Name ungültig oder `/F` ohne Profil = Fehler

---

## Supportpaket

`configuration-summary.json` enthält zusätzlich nicht geheime NetSupport-Metadaten:

- Control-Pfad konfiguriert: ja/nein
- konfigurierte Control-Executable verwendbar: ja/nein
- alternative Installation gefunden: ja/nein
- Produktname
- Produktversion
- Dateiversion
- Hersteller
- Profil konfiguriert/verfügbar
- Profilbindung `/F` aktiv
- Anzahl lokal erkannter Profile

Bei aktiver Anonymisierung wird der konkrete Profilname nicht offengelegt.

---

## Fehlerfälle

### `PCICTLUI.EXE` fehlt

Die Anwendung startet keinen alternativen Remote-Provider. Der Systemzustand versucht zunächst, eine andere lokale NetSupport-Installation zu finden und zeigt ansonsten einen Fehler.

### Falsche EXE konfiguriert

Der Provider verweigert den Start, wenn der Dateiname nicht `PCICTLUI.EXE` ist – selbst wenn die Datei existiert.

### Ungültiger Zielwert

Ein Ziel mit unerlaubten Zeichen wird **vor** dem Prozessstart abgewiesen.

### Profil fehlt oder ist ungültig

Ein konfiguriertes Profil wird vor dem Start lokal geprüft. Fehlt es oder ist der Name ungültig, wird keine Remote-Aktion gestartet.

### NetSupport selbst lehnt die Verbindung ab

Der Prozessstart kann technisch erfolgreich sein, obwohl NetSupport den Client später nicht erreicht oder die Verbindung aufgrund seiner eigenen Konfiguration/Berechtigung ablehnt.

Der lokale Verlauf dokumentiert deshalb den **Startversuch**, nicht den vollständigen Erfolg oder die Dauer einer NetSupport-Sitzung. Für verbindliche Sitzungsnachvollziehbarkeit bleibt die vorhandene NetSupport-/Unternehmenskonfiguration maßgeblich.

---

## Nächste NetSupport-spezifische Ausbauschritte

- optional Installationsordner auf notwendige NetSupport-Begleitdateien prüfen
- besser unterscheiden zwischen `PCICTLUI.EXE`-Startfehler und späterem NetSupport-Verbindungsfehler
- optional bekannte NetSupport-Versionen/Abweichungen im Supportpaket gegeneinander vergleichbar machen
