# Automatisierte Tests

> Diese Datei beschreibt die automatisierten Tests von **NetSupport Remote Admin** und ihre Sicherheitsgrenzen.

---

## Ziel

Sicherheits- und Persistenzregeln werden vor jedem veröffentlichten Windows-Testbuild automatisiert geprüft.

CI-Reihenfolge:

```text
Restore
Build
Test
Publish Windows x64
Upload Artifact
```

Ein fehlgeschlagener Build oder Test verhindert damit Publish und Artifact-Upload.

Testprojekt:

```text
tests/NetSupport.RemoteAdmin.Tests/
```

Framework:

```text
xUnit
.NET 8 / Windows
```

Die Tests starten **keine echte NetSupport-Remoteverbindung** und verändern keine NetSupport-Control-Profile oder Active-Directory-Objekte.

---

## Aktuell automatisiert geprüft

### NetSupport-Kommandozeile

Die produktive Logik liegt in:

```text
Providers/NetSupportCommandLine.cs
```

Geprüft werden unter anderem:

- Rechnername/FQDN und exakte IPv4-`/C`-Syntax
- Ablehnung unsicherer Zielwerte und typischer Kommandozeilen-Injection-Werte
- alle sechs NetSupport-Aktionsargumente
- `/N` und `/F /N`
- `/F` ohne Profil -> Fehler
- Profilnamen mit Quote, Steuerzeichen, Backslash oder Überlänge -> Fehler
- kombinierte Argumentzeichenfolge aus Profil, Ziel und Aktion
- ausschließlich vorhandene `PCICTLUI.EXE` als erlaubte Remote-Executable
- vorhandene Fremd-EXE und fehlende `PCICTLUI.EXE` -> Fehler

### Konfigurationsmigration und Persistenz

Geprüft werden:

- NetSupport-only-Normalisierung alter Providerwerte
- ungültige bevorzugte Aktionen -> `Control`
- Reparatur fehlender Listen
- Schema-Versionierung
- Ablehnung einer Konfiguration aus einer neueren Schema-Version
- alte RDP-Felder verschwinden beim erneuten Serialisieren
- gültiger benutzerdefinierter NetSupport-Client-Port bleibt erhalten
- ungültiger Port fällt auf TCP 5405 zurück
- atomisches Speichern ohne übrig gebliebene Temp-Dateien
- normalisierte Primär- und Backup-Datei
- Recovery aus `settings.json.bak`
- klare Fehlermeldung, wenn Primärdatei und Backup beide beschädigt sind

### NetSupport-Erreichbarkeit

`NetSupportReachabilityService` wird ohne echte Domänenrechner getestet:

- lokaler temporärer TCP-Listener -> erreichbar
- geschlossener lokaler TCP-Port -> nicht erreichbar
- gültiger Portbereich 1..65535
- ungültige Ports werden abgewiesen
- Standardwert TCP 5405 ist gültig

Die Diagnose ist bewusst nicht gleichbedeutend mit einer vollständigen NetSupport-Anmeldung und blockiert keinen echten Start von `PCICTLUI.EXE`.

### Ping-/NetSupport-Statusdarstellung

Geprüft werden Kombinationen wie:

```text
Ping erreichbar · NetSupport erreichbar
Ping erreichbar · NetSupport nicht erreichbar
Ping keine Antwort · NetSupport erreichbar
Ping keine Antwort · NetSupport nicht erreichbar
```

Damit darf ein fehlgeschlagener Ping nicht fälschlich als Beweis für einen ausgeschalteten Rechner dargestellt werden.

### Active-Directory-Ergebnisverarbeitung

Die JSON-Verarbeitung der Domänensuche wird isoliert geprüft:

- leerer Output -> leere Liste
- Einzelobjekt und Array
- `DNSHostName` wird bevorzugt
- fehlender `DNSHostName` fällt auf `Name` zurück
- Werte werden getrimmt
- doppelte Hosts werden unabhängig von Groß-/Kleinschreibung entfernt
- unbrauchbare Einträge ohne Host/Name werden übersprungen

Die Tests greifen nicht auf ein echtes Active Directory zu.

### Supportpaket / Datenschutz

Für ein tatsächlich erzeugtes temporäres Support-ZIP wird geprüft:

- `settings.json` wird nicht aufgenommen
- bekannte Rechnernamen/Hosts werden bei aktiver Anonymisierung entfernt
- Gruppen-/Ansichtsnamen werden abstrahiert
- NetSupport-Profilname wird anonymisiert
- keine Felder für Passwort, Credential oder Token werden eingeführt

### Lokaler Verlauf und CSV

Geprüft werden:

- maximal 100 Verlaufseinträge
- beschädigte History-Datei blockiert die Anwendung nicht
- CSV-Export neutralisiert Zellen, die mit `=`, `+`, `-` oder `@` als Tabellenkalkulationsformel interpretiert werden könnten
- CR/LF wird sauber in Text umgewandelt

### Single Instance

Geprüft werden:

- innerhalb derselben Windows-Sitzung kann nur eine Instanz den Mutex besitzen
- nach Dispose kann eine neue Instanz übernehmen

---

## Letzte bestätigte CI-Validierung

GitHub Actions **#643** auf Head:

```text
b1577f228e6e917fbb2ae7c16a5c08f19e235568
```

Ergebnis:

```text
Build succeeded
0 Warnungen
0 Fehler
84 Tests insgesamt
84 bestanden
0 fehlgeschlagen
```

Danach wurden self-contained Windows-x64-Publish und Artifact-Upload erfolgreich ausgeführt.

Artefakt:

```text
NetSupport.RemoteAdmin-win-x64
SHA-256: 7e5e337ea1c75076779a939edd99900011ce688b7ef5e915316dd6c627f78d2e
```

---

## Was diese Tests bewusst nicht beweisen

Automatisierte Tests können nicht bestätigen, dass eine konkrete NetSupport-Version auf einem echten Admin-PC eine Remote-Sitzung erfolgreich aufbaut.

Praktisch zu testen bleiben insbesondere:

- lokale NetSupport-Installation und Lizenzierung
- tatsächliche `/C`-/`/VC`-/`/N`-/`/F`-Interpretation der eingesetzten NetSupport-Version
- NetSupport-Client-Port/Firewall im realen Netz
- NetSupport-Berechtigungen und Sicherheitsprofile
- Profilpasswort-Dialoge innerhalb NetSupport
- RSAT bzw. LDAP gegen die reale Domäne
- CIM/WSMan auf echten Zielrechnern

Dafür bleibt [`TESTING.md`](TESTING.md) maßgeblich.

---

## Sicherheitsprinzip

Automatisierte Tests dürfen keine Fernwartung starten, keine Active-Directory-Objekte verändern und keine NetSupport-Registryprofile schreiben.

Sie sind eine zusätzliche Schutzschicht für deterministische Eingabevalidierung, Persistenz, Datenschutz und lokale Diagnose. Sie ersetzen nicht die NetSupport-eigene Sitzungsprotokollierung oder einen Pilot-Test auf einem vorgesehenen Admin-PC.
