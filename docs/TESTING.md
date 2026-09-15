# Testplan – NetSupport Remote Admin

> Praktische Prüfschritte für den self-contained Windows-x64-Testbuild.

## Grundregel

```text
Remotezugriff ausschließlich über NetSupport Manager
```

Vor einem Pilotbetrieb prüfen, dass keine RDP-Funktion sichtbar oder startbar ist und die Providerliste ausschließlich NetSupport enthält.

## Testbuild

Ein erfolgreicher GitHub-Actions-Lauf erzeugt:

```text
NetSupport.RemoteAdmin-win-x64
```

ZIP entpacken und `NetSupport.RemoteAdmin.exe` auf einem vorgesehenen Admin-PC starten. Eine separat installierte .NET-8-Laufzeit ist für das self-contained Artefakt nicht erforderlich.

## 1. Programmstart / Single Instance / Tray

- Anwendung startet normal.
- Hinweis auf NetSupport-only ist sichtbar.
- Schließen minimiert in den Infobereich.
- Tray-Doppelklick öffnet das Fenster.
- zweiter Programmstart in derselben Windows-Sitzung öffnet keine zweite produktive Instanz.
- **Beenden** im Tray-Menü beendet die Anwendung vollständig.

## 2. NetSupport-Installation

Unter **Erweitert → Einstellungen → NetSupport Manager**:

- gültige `PCICTLUI.EXE` manuell auswählen
- **Automatisch erkennen** testen
- **NetSupport prüfen…** öffnen und Quelle/Version/Hersteller prüfen
- absichtlich eine Fremd-EXE konfigurieren und sicherstellen, dass der Remote-Start blockiert wird
- danach gültigen Pfad wiederherstellen

Es darf ausschließlich `PCICTLUI.EXE` als Remote-Backend gestartet werden.

## 3. Control-Profil

Mit einem lokalen Testprofil:

- Profil neu laden und auswählen
- ohne `/F` starten und Diagnose auf `/n "Profil"` prüfen
- `/F` aktivieren und Diagnose auf `/f /n "Profil"` prüfen
- Profil testweise löschen/umbenennen: Remote-Start muss blockiert werden
- `/F` ohne Profil darf nicht gespeichert werden
- Profilpasswort, falls vorhanden, muss weiterhin ausschließlich von NetSupport verarbeitet werden

## 4. NetSupport Client-Port

Standard ist:

```text
TCP 5405
```

Prüfen:

- Portfeld zeigt 5405 bei Standardkonfiguration.
- gültiger alternativer Port 1..65535 kann gespeichert werden.
- `0`, negative Werte und Werte >65535 werden abgewiesen.
- **Systemzustand** zeigt den konfigurierten Port nur als lokalen Diagnosewert.

Wenn eure NetSupport-Clients einen abweichenden Port verwenden, diesen vor der Statusprüfung entsprechend setzen.

## 5. Direkte NetSupport-Aktionen

Mit einem freigegebenen Test-PC einzeln prüfen:

- **Steuern**
- **Nur ansehen**
- **Remote CMD**
- **Dateien**
- **Inventar**
- **Chat**

Erwartung: `PCICTLUI.EXE` startet mit der passenden Funktion; Diagnose enthält bei aktiviertem Logging die erzeugte CLI und nach erfolgreichem Prozessstart eine PID.

## 6. Bevorzugte Aktion

Für einen gespeicherten Rechner:

- bevorzugte Aktion wählen und **Speichern / Aktualisieren**
- Anwendung bzw. Auswahl neu laden
- Standardaktionsbutton zeigt die gespeicherte Aktion
- Doppelklick verwendet die **gespeicherte** Aktion
- eine nur im Editor geänderte, noch nicht gespeicherte Aktion darf den Doppelklick nicht dauerhaft beeinflussen

## 7. Active Directory mit RSAT

Auf einem Admin-PC mit `ActiveDirectory`-PowerShell-Modul:

- **Domäne laden**
- Computer erscheinen
- `DNSHostName` wird als Ziel verwendet, soweit vorhanden
- erneutes Laden erzeugt keine unnötigen Host-Duplikate
- gespeicherte Favoriten/Gruppen bleiben erhalten
- Systemzustand meldet RSAT als bevorzugten Discovery-Weg

## 8. Active Directory ohne RSAT / LDAP-Fallback

Auf einem Domänen-PC ohne ActiveDirectory-PowerShell-Modul, aber mit LDAP-Zugriff:

- **Domäne laden**
- Rechnerliste wird über LDAP geladen
- Systemzustand meldet den read-only LDAP-Fallback als verfügbar
- keine AD-Objekte oder Gruppenrichtlinien werden verändert

Zusätzlich einen nicht erreichbaren Domänen-/LDAP-Pfad testen: die Discovery muss spätestens nach ungefähr 30 Sekunden abbrechen und die manuelle NetSupport-Nutzung muss weiterhin möglich sein.

Details: [`ACTIVE_DIRECTORY_DISCOVERY.md`](ACTIVE_DIRECTORY_DISCOVERY.md).

## 9. Ping- und NetSupport-Erreichbarkeit

Mit mehreren Zielen **Status prüfen** ausführen.

Erwartete Kombinationen können sein:

```text
Ping erreichbar · NetSupport erreichbar
Ping erreichbar · NetSupport nicht erreichbar
Ping keine Antwort · NetSupport erreichbar
Ping keine Antwort · NetSupport nicht erreichbar
```

Wichtig:

- fehlender Ping ist kein Beweis für „PC aus“
- geschlossener NetSupport-Port kann auch Firewall/Netzweg/falschen Port bedeuten
- ein fehlgeschlagener Diagnosecheck darf den eigentlichen NetSupport-Start nicht blockieren

## 10. Rechnerdetails

Für einen erreichbaren Test-PC **Rechnerdetails laden**:

- IP-Adresse
- angemeldeter Benutzer
- Windows-Version
- Hersteller/Modell
- Zeitstempel

Blockiertes CIM/WSMan darf NetSupport-Aktionen nicht blockieren.

## 11. Favoriten, Gruppen und Ansichten

- Favorit setzen
- Gruppe vergeben
- speichern und neu laden
- **Nur Favoriten** testen
- Gruppenfilter und Textsuche testen
- gespeicherte Ansichten anlegen, anwenden und löschen

## 12. Verlauf und CSV

Mehrere erfolgreiche und fehlgeschlagene Starts erzeugen.

Prüfen:

- korrekte Aktion/Ziel/Zeit/Erfolg
- maximal 100 Einträge
- beschädigte Testkopie der History darf den App-Start nicht blockieren
- CSV in Excel/LibreOffice öffnen
- UTF-8-Umlaute und Semikolontrennung stimmen
- Fehlertexte mit CR/LF werden zu normalem Text
- Testwerte, die mit `=`, `+`, `-` oder `@` beginnen, dürfen nicht als Formel ausgeführt werden

## 13. Konfigurationsmigration und Recovery

Mit einer alten Testkopie von `settings.json`, die RDP-Felder/Provider enthält:

- App starten
- RDP darf nicht sichtbar oder startbar sein
- gespeicherter Provider wird auf `netsupport` normalisiert
- ungültige bevorzugte Aktion fällt auf `Control` zurück
- nach dem Speichern verschwinden unbekannte alte RDP-Felder

Recovery prüfen:

- gültige `settings.json.bak` vorhanden
- Primärdatei testweise beschädigen
- App muss aus Backup wiederherstellen
- sind Primärdatei und Backup beide beschädigt, muss ein klarer Fehler erscheinen statt stiller Default-Rücksetzung

## 14. Systemzustand

Prüfen:

- NetSupport-only
- AppData beschreibbar
- `PCICTLUI.EXE` und Version
- Control-Profil und `/F`
- Client-Port als lokaler Wert
- RSAT oder LDAP-Fallback
- lokales CIM/WSMan
- Autostart
- Diagnosezustand

Die Seite darf keine Domänenziele scannen und keine Remoteverbindung starten.

## 15. Supportpaket

Mit aktivierter Anonymisierung erzeugen und entpacken.

Prüfen:

- `settings.json` fehlt
- `remoteAccessPolicy = NetSupport-only`
- `netSupportClientPort` entspricht der Konfiguration
- Produkt-/Dateiversion und Profilstatus sind enthalten, soweit verfügbar
- echte Ziel-/Gruppen-/Profilnamen sind ersetzt
- Kennwörter/Credentials sind nicht enthalten
- Supportpaketerstellung startet keinen Zielscan

## 16. Autostart

**Mit Windows starten** aktivieren/deaktivieren und prüfen:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\NetSupportRemoteAdmin
```

Es dürfen keine HKLM- oder GPO-Änderungen erfolgen.

## 17. Diagnoseprotokoll

```text
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

Prüfen auf sinnvolle Start-/Fehlerzeilen, NetSupport-CLI, PID, Zielhost sowie AD-/Statusvorgänge. Kennwörter und Sitzungsinhalte dürfen nicht protokolliert werden.

## Abnahmekriterium

Ein Build ist für einen breiteren Pilotbetrieb geeignet, wenn:

- GitHub Actions vollständig grün ist
- NetSupport-only technisch eingehalten wird
- Installation/Profil auf den vorgesehenen Admin-PCs funktionieren
- Control/View und benötigte weitere Aktionen auf typischen Test-PCs funktionieren
- TCP-Portdiagnose zum realen NetSupport-Setup passt
- RSAT oder LDAP-Discovery in der realen Domäne funktioniert
- optionale CIM-Fehler NetSupport nicht blockieren
- Migration/Recovery praktisch geprüft sind
- Diagnose und Supportpaket keine Credentials enthalten

Erst danach sollte der Build breiter verteilt werden.
