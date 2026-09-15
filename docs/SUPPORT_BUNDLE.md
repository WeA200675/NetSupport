# Supportpaket und Anonymisierung

> Diese Datei beschreibt das lokale Diagnose-/Supportpaket von **NetSupport Remote Admin**.

---

## Ziel

Bei einem reproduzierbaren Fehler soll eine technisch brauchbare ZIP-Datei erstellt werden können, ohne die komplette Benutzerkonfiguration oder Zugangsdaten weiterzugeben.

Das Supportpaket wird unter **Erweitert → Einstellungen → Diagnose und Support** über **Supportpaket erstellen…** erzeugt.

Die Anonymisierung ist standardmäßig aktiviert.

---

## Inhalt des ZIP-Pakets

Ein Supportpaket enthält aktuell:

```text
README.txt
system-info.json
configuration-summary.json
recent-history.json
recent-errors.txt
logs/
    application.log
    application.log.1   (falls vorhanden)
```

### `README.txt`

Enthält Erstellungszeitpunkt, Anonymisierungsstatus und die Betriebsregel:

```text
Remotezugriffsrichtlinie: NetSupport-only
```

### `system-info.json`

Enthält technische Laufzeitinformationen wie:

- App-Version
- Windows-/OS-Beschreibung
- OS- und Prozessarchitektur
- .NET-Laufzeit
- 32-/64-Bit-Status
- Sprache/Kultur
- lokale Rechner-/Benutzerkennung nur anonymisiert, wenn die Option aktiv ist

### `configuration-summary.json`

Die originale `settings.json` wird **nicht** kopiert.

Stattdessen wird eine bereinigte Zusammenfassung erzeugt, unter anderem mit:

- `remoteAccessPolicy = NetSupport-only`
- Start-/Diagnoseoptionen
- Anzahl gespeicherter Ziele und Ansichten
- Favorit/Gruppe/Standard-Provider und bevorzugte NetSupport-Aktion
- NetSupport-Control-Pfad konfiguriert: ja/nein
- konfigurierte `PCICTLUI.EXE` verwendbar: ja/nein
- alternative lokale NetSupport-Installation gefunden: ja/nein
- NetSupport-Produktname
- Produktversion
- Dateiversion
- Hersteller
- `netSupportProfileConfigured`
- `netSupportProfileName`
- `netSupportProfileAvailable`
- `netSupportProfileLocked`
- `netSupportDiscoveredProfileCount`

Der vollständige NetSupport-Installationspfad wird in der bereinigten Zusammenfassung nicht zusätzlich benötigt. Die eigentliche Anwendungskonfiguration bleibt weiterhin ausschließlich in `settings.json`.

### `recent-history.json`

Enthält den lokalen Startverlauf in bereinigter Form:

- Zeitpunkt
- Ziel
- Provider
- Aktion
- Erfolg/Fehler
- Fehlertext

### `recent-errors.txt`

Fasst die letzten bekannten Fehler aus Startverlauf und Diagnoseprotokoll zusammen.

### `logs/`

Vorhandene Diagnoseprotokolle werden beim Erstellen des Pakets neu gelesen und – bei aktivierter Anonymisierung – zusätzlich bereinigt.

---

## NetSupport-Installationsdaten

Die NetSupport-Metadaten stammen aus derselben lokalen Erkennung wie **NetSupport prüfen…** und der **Systemzustand**.

Mögliche Quellen sind:

```text
Konfigurierter Pfad
Program Files (x86)
Program Files
Windows-Uninstall-Registry
```

Es findet keine rekursive Laufwerkssuche statt.

Die Versionsinformationen helfen insbesondere, Unterschiede zwischen mehreren Admin-PCs zu erkennen, ohne dafür Credentials oder Remote-Daten zu sammeln.

---

## NetSupport-Control-Profilstatus

Wenn ein Control-Profil konfiguriert ist, enthält die Zusammenfassung den technischen Zustand dieser Auswahl:

```text
konfiguriert
lokal vorhanden
/F-Bindung aktiv
Anzahl lokal erkannter Profile
```

Die Anwendung nimmt **keine Profilpasswörter** in das Supportpaket auf. Profilpasswörter werden vom Tool grundsätzlich nicht gespeichert.

Bei aktivierter Anonymisierung wird der tatsächliche Profilname ersetzt:

```text
Helpdesk Intern -> netsupport-profile
```

Derselbe Alias wird beim Bereinigen der kopierten Diagnoseprotokolle verwendet. Dadurch soll ein Profilname nicht über eine protokollierte `PCICTLUI.EXE /n ...`-Zeile wieder sichtbar werden.

Details: [`NETSUPPORT_CONTROL_PROFILES.md`](NETSUPPORT_CONTROL_PROFILES.md).

---

## Daten, die niemals aufgenommen werden

Unabhängig von der Anonymisierungsoption werden bewusst nicht in das Supportpaket geschrieben:

- Kennwörter
- NetSupport-Profilpasswörter
- gespeicherte Windows-Credentials
- Original-`settings.json`
- Bildschirm-/Sitzungsinhalte
- Zwischenablageinhalte
- Inhalte übertragener Dateien

---

## Anonymisierung

Standardmäßig ist **Supportpaket anonymisieren (empfohlen)** aktiviert.

Bekannte Zielrechner werden dann in stabile Aliase innerhalb des Pakets umgewandelt, zum Beispiel:

```text
PC-BUERO-17  -> target-001
PC-SERVER-02 -> target-002
```

Zusätzlich werden bekannte lokale Identitäten, Pfade und der konfigurierte NetSupport-Profilname ersetzt, unter anderem:

```text
Rechnername         -> local-machine
Benutzername        -> local-user
Windows-Domain      -> local-domain
Benutzerprofil      -> user-profile
Konfigurationspfad  -> config-directory
NetSupport-Profil   -> netsupport-profile
```

Gruppen und gespeicherte Ansichten werden in der Konfigurationsübersicht bei aktiver Anonymisierung ebenfalls abstrahiert.

---

## Wenn Anonymisierung deaktiviert wird

Bei deaktivierter Anonymisierung können Hostnamen, Anzeigenamen, Gruppen, NetSupport-Profilname und lokale Rechner-/Benutzerkennungen im Paket enthalten sein.

Auch dann werden Kennwörter/Credentials nicht bewusst aufgenommen.

Vor einer externen Weitergabe sollte das ZIP trotzdem kurz geprüft werden.

---

## Temporäre Dateien

Zum Erzeugen des ZIP-Pakets wird ein temporärer Ordner unter dem Windows-Temp-Verzeichnis erstellt.

Nach erfolgreicher oder fehlgeschlagener Erstellung versucht die Anwendung diesen Ordner rekursiv zu löschen.

Die finale ZIP-Datei wird ausschließlich an dem vom Benutzer ausgewählten Speicherort geschrieben.

---

## Architektur

```text
SettingsWindow
   |
   +--> ISupportBundleService
           |
           +--> SupportBundleService
                   +--> AppConfig
                   +--> ConfigService
                   +--> INetSupportInstallationService
                   +--> INetSupportProfileService
                   +--> ISessionHistoryService
                   +--> IDiagnosticLogService
                   +--> System-/Runtime-Informationen
                   +--> ZIP-Erzeugung
```

Der Service erzeugt bewusst eine eigene Supportdarstellung statt bestehende Konfigurationsdateien blind zu archivieren.

---

## Prüfschritte

Für einen manuellen Test:

1. Diagnoseprotokoll aktivieren.
2. Optional ein NetSupport-Control-Profil auswählen und eine Aktion starten.
3. Einige erfolgreiche und eine absichtlich fehlerhafte NetSupport-Aktion starten.
4. **Einstellungen → Supportpaket erstellen…** öffnen.
5. Anonymisierung aktiviert lassen.
6. ZIP entpacken.
7. Prüfen, dass Hostnamen durch `target-...` ersetzt wurden.
8. Bei konfiguriertem Profil prüfen, dass dessen echter Name durch `netsupport-profile` ersetzt wurde.
9. Prüfen, dass auch die kopierten Logzeilen den echten Profilnamen nicht enthalten.
10. Prüfen, dass `settings.json` nicht enthalten ist.
11. Nach bekannten Benutzernamen, Domainnamen und Kennwörtern suchen.
12. Prüfen, dass `configuration-summary.json` `remoteAccessPolicy: NetSupport-only` enthält.
13. NetSupport-Produkt-/Dateiversion, Erkennungsstatus und Profilstatus prüfen.
14. `recent-errors.txt` auf sinnvolle Fehlermeldungen prüfen.

---

## Sicherheitsgrenze

Das Supportpaket reduziert das Risiko einer unbeabsichtigten Weitergabe interner Namen und Konfigurationen. Es ist jedoch keine formale Data-Loss-Prevention- oder Compliance-Lösung.

Frei formulierte Fehlermeldungen von Fremdkomponenten können theoretisch unbekannte Informationen enthalten. Deshalb sollte ein Paket vor externer Weitergabe weiterhin kurz geprüft werden.
