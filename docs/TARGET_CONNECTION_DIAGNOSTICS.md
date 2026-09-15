# Einzelrechner-Verbindungsdiagnose

> Read-only Diagnose für einen ausgewählten oder direkt eingegebenen Rechner, ohne eine NetSupport-Sitzung zu starten.

## Ziel

Die globale Funktion **Status prüfen** ist für viele Rechner gleichzeitig gedacht. Wenn bei einem einzelnen Rechner unklar ist, warum eine Verbindung nicht zustande kommt, steht zusätzlich **Verbindung diagnostizieren** zur Verfügung.

Die Diagnose prüft drei voneinander unabhängige Signale:

```text
DNS / IP-Auflösung
Ping / ICMP
NetSupport TCP <konfigurierter Client-Port>
```

Standardmäßig ist der NetSupport-Client-Port TCP 5405; ein abweichender Wert wird aus der normalen Anwendungskonfiguration übernommen.

## Bedienung

1. Rechner in der Liste auswählen oder Rechnername/IP oben eingeben.
2. **Verbindung diagnostizieren** wählen.
3. Die Anwendung prüft DNS, Ping und den NetSupport-Port parallel.
4. Ein Dialog zeigt Ergebnis und Laufzeit jeder Prüfung.
5. Optional **Diagnose kopieren** verwenden, um einen kurzen Textbericht in die Zwischenablage zu legen.

Der kopierte Bericht enthält Zielname/IP, Zeitpunkt, DNS-Ergebnis, Ping-Ergebnis, Portstatus und Laufzeiten. Er enthält keine Kennwörter oder Sitzungsinhalte.

## Ergebnisinterpretation

Mögliche Kombinationen sind ausdrücklich unabhängig voneinander. Beispiel:

```text
Ping keine Antwort
TCP 5405 erreichbar
```

Das ist ein gültiger Zustand, etwa wenn ICMP blockiert ist, NetSupport aber erreichbar bleibt.

Umgekehrt bedeutet:

```text
DNS funktioniert
TCP 5405 nicht erreichbar
```

nicht automatisch, dass der PC ausgeschaltet ist. Mögliche Ursachen sind unter anderem Firewall, Netzweg, falscher Client-Port oder ein nicht laufender NetSupport-Client.

Die Diagnose behauptet deshalb bewusst nicht `Offline` oder `ausgeschaltet`.

## Zeitlimits

Die Einzelprüfung ist bewusst kurz gehalten:

- Ping: ungefähr 1,2 Sekunden
- NetSupport TCP: ungefähr 1,5 Sekunden
- DNS: ungefähr 2,5 Sekunden
- äußerer UI-Schutz: ungefähr 6 Sekunden

Die drei Netzwerkprüfungen laufen parallel. Ein langsamer oder fehlerhafter DNS-/Netzpfad soll das Hauptfenster nicht dauerhaft blockieren.

## Sicherheitsgrenze

Die Funktion ist read-only:

- startet **keine** `PCICTLUI.EXE`
- öffnet keine Remote-Control-Sitzung
- verändert keine Einstellungen auf dem Zielrechner
- verändert keine Active-Directory-Objekte oder GPOs
- speichert keine Credentials
- blockiert einen späteren NetSupport-Start nicht, auch wenn Diagnosechecks fehlschlagen

Der TCP-Test bestätigt lediglich, dass der konfigurierte Port erreichbar ist. Er beweist keine erfolgreiche NetSupport-Anmeldung, Lizenzierung oder Berechtigung.

## Laufzeitstatus

Nach einer Einzelprüfung werden die vorhandenen flüchtigen Statuswerte des Ziels aktualisiert:

```text
Ping-Status
NetSupport-Erreichbarkeit
letzter Prüfzeitpunkt
```

Diese Werte bleiben Laufzeitdaten und werden nicht als dauerhafte Zielkonfiguration gespeichert.

## Architektur

```text
MainWindow
   |
   +--> ITargetConnectionDiagnosticService
           |
           +--> TargetConnectionDiagnosticService
                   +--> DNS
                   +--> HostAvailabilityService
                   +--> NetSupportReachabilityService

TargetConnectionDiagnosticsWindow
   +--> zeigt Resultat
   +--> kopiert optional einen Textbericht
```

Ergebnisobjekt:

```text
Models/TargetConnectionDiagnosticResult.cs
```

## Automatisierte Tests

Automatisiert geprüft werden unter anderem:

- Ping und NetSupport bleiben semantisch unabhängig
- kein `offline`-/`ausgeschaltet`-Overclaim im Ergebnistext
- DNS-Adressen und DNS-Fehler werden nachvollziehbar dargestellt
- Bericht enthält Ziel, Port und Laufzeiten
- leeres Ziel wird vor jeder Netzwerkprüfung abgewiesen
- ungültige TCP-Ports werden vor jeder Netzwerkprüfung abgewiesen

Automatisierte Tests greifen dafür nicht auf einen echten Domänenrechner zu.

## Praktischer Pilot-Test

Mit mindestens zwei typischen Zielrechnern prüfen:

1. normal erreichbarer PC: DNS, Ping und NetSupport-Port plausibel.
2. PC mit blockiertem ICMP oder Firewall-Test: Ergebnis darf nicht fälschlich `PC aus` behaupten.
3. absichtlich falschen NetSupport-Port konfigurieren: Port muss als nicht erreichbar erscheinen.
4. korrekten Port wiederherstellen und erneut prüfen.
5. **Diagnose kopieren** verwenden und Text auf unerwünschte Credentials/Sitzungsdaten kontrollieren.
6. anschließend eine echte NetSupport-Aktion separat starten und sicherstellen, dass die Diagnose diese nicht blockiert.
