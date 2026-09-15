# Automatisierte Tests

> Diese Datei beschreibt die automatisierten Tests von **NetSupport Remote Admin** und ihre Sicherheitsgrenzen.

---

## Ziel

Die produktive NetSupport-Anbindung erzeugt bewusst eine rohe Kommandozeile für `PCICTLUI.EXE`, weil NetSupport für IP-Ziele eine besondere Syntax verwendet.

Deshalb werden die sicherheitskritischen Regeln jetzt automatisiert geprüft, bevor GitHub Actions einen Windows-Testbuild veröffentlicht.

CI-Reihenfolge:

```text
Restore
Build
Test
Publish Windows x64
Upload Artifact
```

Ein fehlgeschlagener Test verhindert damit Publish und Artifact-Upload.

---

## Testprojekt

```text
tests/NetSupport.RemoteAdmin.Tests/
```

Framework:

```text
xUnit
.NET 8 / Windows
```

Das Testprojekt referenziert die produktive Anwendung. Für die isolierte interne Kommandozeilenlogik wird ausschließlich dem Test-Assembly `NetSupport.RemoteAdmin.Tests` interner Zugriff gewährt.

---

## Isolierte Kommandozeilenlogik

Die testbare Logik liegt in:

```text
Providers/NetSupportCommandLine.cs
```

`NetSupportProvider` verwendet dieselbe Klasse im produktiven Prozessstart.

Damit testen wir nicht eine nachgebaute Kopie der Regeln, sondern genau die Logik, die später die Argumentzeichenfolge für `PCICTLUI.EXE` erzeugt.

Die Tests starten **kein** NetSupport und bauen **keine** Remoteverbindung auf.

---

## Aktuell automatisiert geprüft

### Zielrechner / `/C`

Akzeptiert werden unter anderem:

```text
PC-001
PC-001.example.local
10.20.30.40
```

Für IPv4 wird die erwartete NetSupport-Syntax exakt geprüft:

```text
/c">10.20.30.40"
```

Abgewiesen werden unter anderem:

```text
leere Werte
-bad
PC 001
PC-001" /a
Zeilenumbrüche
PC-001&calc
```

Dadurch werden typische Kommandozeilen-Injection-Versuche vor `Process.Start` abgefangen.

### NetSupport-Aktionen

Für alle aktuell unterstützten Aktionen wird die exakte Argumentabbildung geprüft:

```text
Control        -> /vc /e
View           -> /v /e
Chat           -> /a /ea
Inventory      -> /i /ei
CommandPrompt  -> /m /em
FileTransfer   -> /x /ex
```

### Control-Profile

Geprüft werden:

- kein Profil + `/F` aus -> keine Profilargumente
- `/F` ohne `/N` -> Fehler
- `/N "Profil"`
- `/F /N "Profil"`
- Profilnamen mit Leerzeichen
- Anführungszeichen im Profilnamen -> Fehler
- Steuerzeichen im Profilnamen -> Fehler
- mehr als 128 Zeichen -> Fehler
- führende/trailing Leerzeichen werden normalisiert

Zusätzlich gibt es einen End-to-End-Test der **erzeugten Argumentzeichenfolge** aus Profil + IP + Control-Aktion.

### Schutz der ausführbaren Datei

Mit temporären lokalen Testdateien wird geprüft:

- vorhandene Datei namens `PCICTLUI.EXE` wird akzeptiert
- vorhandene Fremd-EXE wie `notepad.exe` wird abgewiesen
- nicht vorhandene `PCICTLUI.EXE` wird abgewiesen

Dabei wird keine Testdatei ausgeführt.

---

## Was diese Tests bewusst nicht beweisen

Automatisierte Unit-Tests können nicht bestätigen, dass eine konkrete NetSupport-Version auf einem echten Admin-PC eine Remote-Sitzung erfolgreich aufbaut.

Weiterhin praktisch zu testen sind daher insbesondere:

- lokale NetSupport-Installation und Lizenzierung
- tatsächliche `/C`-/`/VC`-/`/N`-/`/F`-Interpretation der eingesetzten NetSupport-Version
- Client-Erreichbarkeit
- NetSupport-Berechtigungen/Sicherheitsprofile
- Profilpasswort-Dialoge innerhalb NetSupport
- AD/RSAT in der realen Domäne
- CIM/WSMan auf echten Zielrechnern

Dafür bleibt [`TESTING.md`](TESTING.md) maßgeblich.

---

## Sicherheitsprinzip

Die Tests dürfen selbst keine Fernwartung starten und keine NetSupport-Registryprofile verändern.

Sie prüfen ausschließlich deterministische Eingabevalidierung, Argumenterzeugung und lokale Pfadregeln.

Damit sind die Tests eine zusätzliche Schutzschicht; sie ersetzen nicht die NetSupport-eigene Sitzungsprotokollierung oder die Freigabe des Testbuilds auf einem administrativen Pilot-PC.
