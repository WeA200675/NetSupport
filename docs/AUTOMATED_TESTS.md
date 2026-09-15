# Automatisierte Tests

> Diese Datei beschreibt die automatisierten Tests von **NetSupport Remote Admin** und ihre Sicherheitsgrenzen.

## CI-Gate

```text
Restore
Build
Test
Publish Windows x64
Upload Artifact
```

Ein fehlgeschlagener Build oder Test verhindert Publish und Artifact-Upload.

Testprojekt:

```text
tests/NetSupport.RemoteAdmin.Tests/
```

Framework: xUnit auf .NET 8 / Windows.

Die Tests starten keine echte NetSupport-Remoteverbindung und verändern keine NetSupport-Control-Profile oder Active-Directory-Objekte.

## Abgedeckte Bereiche

### NetSupport-Kommandozeile

Geprüft werden unter anderem:

- Rechnername/FQDN und exakte IPv4-`/C`-Syntax
- Ablehnung unsicherer Ziel-/Injection-Werte
- alle sechs NetSupport-Aktionsargumente
- `/N` und `/F /N`
- `/F` ohne Profil -> Fehler
- ungültige Profilnamen -> Fehler
- kombinierte Argumentzeichenfolge aus Profil, Ziel und Aktion
- ausschließlich vorhandene `PCICTLUI.EXE` als erlaubte Remote-Executable

### Konfiguration und Recovery

Geprüft werden:

- NetSupport-only-Normalisierung alter Providerwerte
- ungültige bevorzugte Aktionen -> `Control`
- Schema-Versionierung und Schutz vor neuerem Schema
- Entfernung alter RDP-Felder beim erneuten Serialisieren
- NetSupport-Client-Port und Fallback auf TCP 5405
- atomisches Speichern
- normalisierte Primär-/Backup-Datei
- Recovery aus `settings.json.bak`
- klarer Fehler, wenn Primärdatei und Backup beschädigt sind

### NetSupport-Erreichbarkeit

Ohne echte Domänenrechner:

- lokaler temporärer TCP-Listener -> erreichbar
- geschlossener TCP-Port -> nicht erreichbar
- gültiger Portbereich 1..65535
- ungültige Ports werden abgewiesen

Der Porttest ist reine Diagnose und blockiert keinen Start von `PCICTLUI.EXE`.

### Ping-/NetSupport-Status

Geprüft werden Kombinationen wie:

```text
Ping erreichbar · NetSupport erreichbar
Ping erreichbar · NetSupport nicht erreichbar
Ping keine Antwort · NetSupport erreichbar
Ping keine Antwort · NetSupport nicht erreichbar
```

Damit wird ein ICMP-Fehler nicht als sicherer Beweis für einen ausgeschalteten Rechner dargestellt.

### Active Directory

Die Ergebnisverarbeitung der RSAT-/LDAP-Discovery wird isoliert geprüft:

- leerer Output
- Einzelobjekt und Array
- `DNSHostName` wird bevorzugt
- Fallback auf `Name`
- Trim/Normalisierung
- case-insensitive Deduplizierung
- ungültige Einträge ohne Host werden übersprungen

Die Tests greifen nicht auf ein echtes Active Directory zu.

### Supportpaket / Datenschutz

Ein tatsächlich erzeugtes temporäres ZIP wird geprüft:

- `settings.json` wird nicht aufgenommen
- bekannte Ziel-/Gruppen-/Profilwerte werden anonymisiert
- keine Passwort-/Credential-/Token-Felder werden eingeführt
- `configuration-summary.json` enthält den konfigurierten `netSupportClientPort`

### Verlauf und CSV

Geprüft werden:

- maximal 100 Verlaufseinträge
- beschädigte History-Datei blockiert die Anwendung nicht
- CSV-Formula-Injection für `=`, `+`, `-`, `@`
- saubere CR/LF-Normalisierung

### Single Instance

Geprüft werden Mutex-Besitz und erneute Übernahme nach Dispose.

## Letzte bestätigte CI-Validierung

GitHub Actions **#661** auf Head:

```text
98605a8d59838be1ae9c877187f783b7e2dbf39e
```

Ergebnis:

```text
Build succeeded
0 Warnungen
0 Fehler
85 Tests insgesamt
85 bestanden
0 fehlgeschlagen
```

Danach wurden self-contained Windows-x64-Publish und Artifact-Upload erfolgreich ausgeführt.

Artefakt:

```text
NetSupport.RemoteAdmin-win-x64
SHA-256: 8676d6db1243bb18887f5bd515a95e5cd58ccdc1e5b8bc5bc5ea9df0362ee08f
```

## Was diese Tests bewusst nicht beweisen

Praktisch zu testen bleiben insbesondere:

- lokale NetSupport-Installation und Lizenzierung
- tatsächliche `/C`-/`/VC`-/`/N`-/`/F`-Interpretation der eingesetzten NetSupport-Version
- NetSupport-Client-Port/Firewall im realen Netz
- NetSupport-Berechtigungen und Sicherheitsprofile
- Profilpasswort-Dialoge innerhalb NetSupport
- RSAT bzw. LDAP gegen die reale Domäne
- CIM/WSMan auf echten Zielrechnern

Details: [`TESTING.md`](TESTING.md).

## Sicherheitsprinzip

Automatisierte Tests dürfen keine Fernwartung starten, keine Active-Directory-Objekte verändern und keine NetSupport-Registryprofile schreiben. Sie ergänzen, aber ersetzen nicht den Pilot-Test auf einem vorgesehenen Admin-PC.
