# Supportpaket und Anonymisierung

> Das Supportpaket liefert eine technisch brauchbare ZIP-Datei für Fehlersuche, ohne die originale Benutzerkonfiguration oder Zugangsdaten zu kopieren.

## Inhalt

```text
README.txt
system-info.json
configuration-summary.json
recent-history.json
recent-errors.txt
logs/
```

Die Originaldatei `settings.json` wird **nicht** in das ZIP kopiert.

## `configuration-summary.json`

Die bereinigte Konfigurationsübersicht enthält unter anderem:

- `remoteAccessPolicy = NetSupport-only`
- Start-/Diagnoseoptionen
- Anzahl gespeicherter Ziele und Ansichten
- Favoriten, Gruppen und bevorzugte NetSupport-Aktionen in bereinigter Form
- `netSupportClientPort`
- NetSupport-Control-Pfad konfiguriert/verwendbar
- alternative lokale NetSupport-Installation gefunden
- Produkt-/Dateiversion und Hersteller, soweit lokal auslesbar
- NetSupport-Profil konfiguriert/verfügbar
- `/F`-Profilbindung
- Anzahl erkannter lokaler Control-Profile

`netSupportClientPort` ist nur der konfigurierte Diagnosewert. Das Supportpaket führt **keinen** Portscan aus und enthält keine Liste erreichbarer Zielrechner.

## NetSupport-Profil

Profilpasswörter werden vom Tool nicht gespeichert und daher auch nicht in das Supportpaket aufgenommen.

Bei aktiver Anonymisierung wird ein konfigurierter Profilname ersetzt:

```text
Helpdesk Intern -> netsupport-profile
```

Derselbe Alias wird beim Bereinigen der kopierten Diagnoseprotokolle verwendet.

## Anonymisierung

Standardmäßig ist **Supportpaket anonymisieren (empfohlen)** aktiviert.

Bekannte Werte werden innerhalb des Pakets ersetzt, zum Beispiel:

```text
PC-BUERO-17  -> target-001
Rechnername  -> local-machine
Benutzername -> local-user
Windows-Domain -> local-domain
Benutzerprofil -> user-profile
Konfigurationspfad -> config-directory
NetSupport-Profil -> netsupport-profile
```

Gruppen und gespeicherte Ansichten werden ebenfalls abstrahiert.

Automatisierte Tests erzeugen ein echtes temporäres ZIP und prüfen unter anderem, dass bekannte Ziel-/Gruppen-/Profilnamen bei aktivierter Anonymisierung nicht als Klartext enthalten sind und `settings.json` kein ZIP-Eintrag ist.

## Bewusst nicht enthalten

Unabhängig von der Anonymisierungsoption werden nicht bewusst aufgenommen:

- Kennwörter
- NetSupport-Profilpasswörter
- gespeicherte Windows-Credentials
- Original-`settings.json`
- Bildschirm-/Sitzungsinhalte
- Zwischenablageinhalte
- Inhalte übertragener Dateien

## Wenn Anonymisierung deaktiviert wird

Dann können Hostnamen, Anzeigenamen, Gruppen, Profilname sowie lokale Rechner-/Benutzerkennungen enthalten sein. Credentials werden trotzdem nicht bewusst gesammelt.

Ein Paket sollte vor externer Weitergabe weiterhin kurz geprüft werden, da frei formulierte Fehlermeldungen von Fremdkomponenten unbekannte interne Informationen enthalten können.

## Temporäre Daten

Für die ZIP-Erzeugung wird ein temporärer Ordner unter dem Windows-Temp-Verzeichnis verwendet. Die Anwendung versucht diesen nach Erfolg oder Fehler rekursiv zu löschen. Die finale ZIP-Datei wird nur an den vom Benutzer ausgewählten Speicherort geschrieben.

## Manueller Prüfschritt

1. Diagnoseprotokoll aktivieren.
2. Einige NetSupport-Aktionen ausführen, darunter optional einen Fehlerfall.
3. Supportpaket mit aktivierter Anonymisierung erzeugen.
4. ZIP entpacken.
5. Prüfen, dass `settings.json` fehlt.
6. Nach bekannten Rechner-, Benutzer-, Domain-, Gruppen- und Profilnamen suchen.
7. `configuration-summary.json` auf `remoteAccessPolicy`, `netSupportClientPort`, Version/Erkennung und Profilstatus prüfen.
8. `recent-errors.txt` und Logs auf sinnvolle, anonymisierte Diagnoseinformationen prüfen.

## Sicherheitsgrenze

Das Supportpaket ist eine Datenschutz- und Diagnosehilfe, keine formale DLP-/Compliance-Lösung. Es verändert keine Zielsysteme, AD-Objekte, Firewallregeln oder NetSupport-Client-Konfigurationen.
