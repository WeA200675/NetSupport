# Testplan – NetSupport Remote Admin

> Praktische Prüfschritte für den Windows-x64-Testbuild.

---

## Grundregel

Für diese Domäne gilt:

```text
Remotezugriff ausschließlich über NetSupport Manager
```

RDP darf nicht als Fernwartungsweg angeboten oder gestartet werden.

Vor jedem produktiven Test zuerst prüfen:

- kein RDP-Button sichtbar
- keine RDP-Einstellungen vorhanden
- keine RDP-Monitoroptionen vorhanden
- Providerliste enthält nur NetSupport

Details: [`DOMAIN_REMOTE_POLICY.md`](DOMAIN_REMOTE_POLICY.md).

---

## Testbuild beziehen

Ein erfolgreicher GitHub-Actions-Lauf erzeugt:

```text
NetSupport.RemoteAdmin-win-x64
```

Vorgehen:

1. erfolgreichen Workflow-Lauf öffnen
2. unter **Artifacts** `NetSupport.RemoteAdmin-win-x64` herunterladen
3. ZIP in einen Testordner entpacken
4. `NetSupport.RemoteAdmin.exe` starten

Der Build ist self-contained; eine separat installierte .NET-8-Laufzeit ist nicht erforderlich.

---

## 1. Programmstart und Tray

Prüfen:

- Hauptfenster öffnet sich
- Hinweis **NetSupport-only / RDP nicht angeboten** ist sichtbar
- Schließen minimiert in den Infobereich
- Doppelklick auf das Tray-Symbol öffnet das Fenster
- **Beenden** im Tray-Menü beendet die Anwendung vollständig

---

## 2. Systemzustand

Unter:

```text
Erweitert → Einstellungen → Systemzustand
```

prüfen:

- Remotezugriffsrichtlinie zeigt **NetSupport-only**
- AppData ist beschreibbar
- `PCICTLUI.EXE` wird gefunden oder verständlich als fehlend gemeldet
- bei gültiger Datei werden Produkt-/Dateiversion soweit verfügbar angezeigt
- bei absichtlich falschem konfiguriertem Pfad wird eine alternative lokale Installation als Hinweis erkannt, sofern vorhanden
- NetSupport-Control-Profil wird separat geprüft
- RSAT/ActiveDirectory-Modul wird korrekt erkannt
- lokales CIM/WSMan wird geprüft
- Autostartzustand wird angezeigt
- Diagnosezustand wird angezeigt
- **Neu prüfen** funktioniert

Die Seite darf keine RDP-Komponenten prüfen und keine Remoteverbindung aufbauen.

---

## 3. NetSupport-Installation und Pfad

Unter **Einstellungen → NetSupport Manager**:

### Manuelle Auswahl

1. gültige `PCICTLUI.EXE` über **Durchsuchen…** auswählen
2. prüfen, dass unter dem Feld **Gefunden** und eine Version erscheint
3. speichern
4. prüfen, dass NetSupport ohne Programmneustart als Provider verfügbar ist

### Automatische Erkennung

1. vorhandenen Pfad notieren
2. Feld testweise leeren oder auf einen nicht vorhandenen Pfad setzen
3. **Automatisch erkennen** drücken
4. wenn NetSupport lokal installiert ist, muss eine gültige `PCICTLUI.EXE` übernommen werden
5. prüfen, dass keine Laufwerkssuche bzw. kein langer Scan sichtbar stattfindet

### Prüffenster

1. **NetSupport prüfen…** öffnen
2. Liste der Kandidaten prüfen
3. bei vorhandener Installation sollen Quelle und Version sichtbar sein
4. gültigen Kandidaten auswählen
5. **Pfad übernehmen** verwenden
6. prüfen, dass das Einstellungsfeld aktualisiert wird

Typischer Pfad:

```text
C:\Program Files (x86)\NetSupport\NetSupport Manager\PCICTLUI.EXE
```

Der tatsächliche Pfad kann je Installation abweichen.

---

## 4. Schutz vor falscher EXE

Dieser Test prüft die Provider-Härtung.

1. Anwendung beenden
2. in einer Testkopie der lokalen `settings.json` den Wert `netSupportExecutable` absichtlich auf eine andere vorhandene EXE setzen
3. Anwendung starten
4. NetSupport-Aktion auslösen
5. Start muss mit verständlicher Fehlermeldung abgewiesen werden
6. das fremde Programm darf **nicht** gestartet werden
7. anschließend den korrekten NetSupport-Pfad über **Automatisch erkennen** oder **NetSupport prüfen…** wiederherstellen

Der Provider darf ausschließlich `PCICTLUI.EXE` starten.

---

## 4a. NetSupport-Control-Profile

In NetSupport Manager für den aktuellen Windows-Benutzer ein Testprofil anlegen, zum Beispiel:

```text
Helpdesk Test
```

NetSupport verwaltet diese Profile unter:

```text
HKCU\Software\NetSupport Ltd\PCICTL\ConfigList
```

### Profil erkennen und laden

1. **Erweitert → Einstellungen → NetSupport Manager** öffnen
2. **Profile neu laden** drücken
3. `Helpdesk Test` muss in der Liste erscheinen
4. Profil auswählen
5. `/F` zunächst deaktiviert lassen
6. speichern
7. **Systemzustand** öffnen
8. **NetSupport Control-Profil** muss OK anzeigen
9. Diagnoseprotokoll aktivieren
10. eine NetSupport-Aktion starten
11. Diagnose muss `/n "Helpdesk Test"` in der erzeugten `PCICTLUI.EXE`-Kommandozeile enthalten

### Feste Profilbindung

1. **Control auf dieses Profil festlegen (/F)** aktivieren
2. speichern
3. NetSupport-Aktion starten
4. Diagnose muss `/f /n "Helpdesk Test"` enthalten
5. prüfen, dass NetSupport den Control entsprechend seiner `/F`-/`/N`-Semantik auf das gewählte Profil beschränkt

### Fehlendes Profil

1. Anwendung schließen oder Einstellungsfenster schließen
2. Testprofil in NetSupport Manager umbenennen oder löschen
3. Anwendung erneut verwenden
4. **Systemzustand** muss das konfigurierte Profil als fehlend melden
5. eine Remote-Aktion darf **nicht** stillschweigend mit einem anderen Profil starten
6. verständliche Fehlermeldung muss erscheinen

### Ungültige Kombinationen

Prüfen:

- `/F` aktivieren und Profilfeld leeren → Speichern muss blockiert werden
- Profilname mit `"` oder Zeilenumbruch über manipulierte Konfiguration → Provider muss Start blockieren
- unbekannten, aber syntaktisch gültigen Profilnamen speichern → Warnung; Remote-Start bleibt blockiert, bis das Profil lokal vorhanden ist

### Profilpasswort

Falls das NetSupport-Profil selbst passwortgeschützt ist:

- Passwortabfrage muss weiterhin von NetSupport kommen
- das Remote-Admin-Tool darf kein Profilpasswort speichern
- im Diagnoseprotokoll und Supportpaket darf kein Profilpasswort erscheinen

### Supportpaket

Bei aktivierter Anonymisierung prüfen:

- `netSupportProfileConfigured` ist enthalten
- `netSupportProfileAvailable` ist enthalten
- `netSupportProfileLocked` ist enthalten
- der echte Profilname wird als `netsupport-profile` ausgegeben
- auch Diagnosezeilen im ZIP dürfen den echten Profilnamen nicht mehr enthalten

Details: [`NETSUPPORT_CONTROL_PROFILES.md`](NETSUPPORT_CONTROL_PROFILES.md).

---

## 5. Direkte Zielverbindung

Mit einem bekannten Testrechner prüfen:

- Computername eingeben
- **Verbinden** startet die gewählte NetSupport-Aktion
- IP-Adresse eingeben
- **Steuern** startet NetSupport Control
- **Nur ansehen** startet View

Bei einem absichtlich ungültigen Ziel muss die Anwendung selbst stabil bleiben.

---

## 6. NetSupport-Schnellaktionen

Für einen freigegebenen Test-PC einzeln prüfen:

- **Steuern**
- **Nur ansehen**
- **Remote CMD**
- **Dateien**
- **Inventar**
- **Chat**

Erwartung:

- `PCICTLUI.EXE` startet
- gewünschte NetSupport-Funktion wird verwendet
- Fehler werden verständlich angezeigt
- Diagnose enthält bei aktiviertem Logging die erzeugte CLI und nach erfolgreichem Prozessstart eine PID

---

## 7. Domänenrichtlinien-Sperre

Mit einer alten `settings.json`, die beispielsweise enthält:

```json
{
  "useEmbeddedRdp": true,
  "useFullScreenRdp": true,
  "targets": [
    {
      "name": "ALT-PC",
      "host": "ALT-PC",
      "preferredProviderId": "rdp",
      "rdpSelectedMonitors": "0,1"
    }
  ]
}
```

prüfen:

1. Anwendung starten
2. keine RDP-Funktion darf sichtbar sein
3. Ziel auswählen
4. Standardverbindung muss NetSupport verwenden
5. Ziel oder Einstellungen speichern
6. `settings.json` erneut öffnen
7. alte, unbekannte RDP-Felder sollen im neu geschriebenen Datenmodell nicht mehr enthalten sein
8. `preferredProviderId` soll `netsupport` sein

---

## 8. Active Directory

Wenn RSAT verfügbar ist:

1. **Domäne laden**
2. prüfen, dass Domänenrechner erscheinen
3. gespeicherte Favoriten/Gruppen dürfen nicht verloren gehen
4. mehrfaches Laden darf keine unnötigen Duplikate erzeugen

Wenn RSAT fehlt, muss eine verständliche Warnung erscheinen und die manuelle NetSupport-Nutzung weiterhin funktionieren.

---

## 9. Statusprüfung

Mit mehreren Zielen:

- **Status prüfen**
- Online-/Offline-Anzeige prüfen
- Anwendung während der parallelen Prüfung weiterhin bedienen

Hinweis: Ping ist nur ein Erreichbarkeitshinweis. Ein fehlgeschlagener Ping beweist nicht zwingend, dass der PC ausgeschaltet ist.

---

## 10. Rechnerdetails

Für einen erreichbaren Test-PC:

- **Rechnerdetails laden**
- IP-Adresse prüfen
- angemeldeten Benutzer prüfen
- Windows-Version prüfen
- Hersteller/Modell prüfen
- Zeitstempel prüfen

Bei blockiertem CIM/WSMan muss die Anwendung weiter nutzbar bleiben und die NetSupport-Aktionen dürfen nicht blockiert werden.

---

## 11. Favoriten und Gruppen

Prüfen:

- Favorit setzen
- Gruppe vergeben
- **Speichern / Aktualisieren**
- Anwendung neu starten
- Werte bleiben erhalten
- **Nur Favoriten** funktioniert
- Gruppenfilter funktioniert
- Suchfeld findet Gruppennamen
- Favoriten stehen oben

Nicht gespeicherte Änderungen dürfen durch einen bloßen Verbindungsstart nicht dauerhaft werden.

---

## 12. Gespeicherte Ansichten

Beispiele anlegen:

```text
Meine Favoriten
Server
Werkstatt
```

Prüfen:

- Suchtext wird gespeichert
- Gruppe wird gespeichert
- Favoritenfilter wird gespeichert
- Auswahl einer Ansicht stellt die Filter wieder her
- Löschen einer Ansicht funktioniert

---

## 13. Verlauf

Mehrere NetSupport-Aktionen starten und prüfen:

- Einträge erscheinen unter **Zuletzt verwendet**
- Zeitpunkt ist korrekt
- Provider ist NetSupport
- Aktion ist korrekt
- fehlgeschlagene Starts werden als Fehler markiert
- Doppelklick übernimmt nur das Ziel und startet nicht automatisch eine neue Sitzung
- Verlauf ist auf 100 Einträge begrenzt

---

## 14. CSV-Export

**CSV exportieren** verwenden und in Excel/LibreOffice öffnen.

Prüfen:

- UTF-8-Umlaute korrekt
- Semikolontrennung korrekt
- Spalten vollständig
- Fehlertexte korrekt escaped

---

## 15. Autostart

**Mit Windows starten** aktivieren.

Prüfen:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

mit Wert:

```text
NetSupportRemoteAdmin
```

Danach deaktivieren und prüfen, dass der Eintrag entfernt wird.

Es dürfen keine HKLM- oder GPO-Änderungen erfolgen.

---

## 16. Diagnoseprotokoll

Diagnose aktivieren und einige Aktionen ausführen.

Prüfen:

```text
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

Erwartete Inhalte:

- Start/Ende
- NetSupport-Aktionsstarts
- erzeugte `PCICTLUI.EXE`-Kommandozeile
- PID eines erfolgreich gestarteten NetSupport-Prozesses
- Zielhostname
- AD-/Statusvorgänge
- Fehler
- NetSupport-only-Richtlinienhinweis

Nicht enthalten sein dürfen Kennwörter oder Sitzungsinhalte.

---

## 17. Supportpaket

Mit aktivierter Anonymisierung ZIP erzeugen.

Prüfen:

```text
README.txt
system-info.json
configuration-summary.json
recent-history.json
recent-errors.txt
logs/
```

Zusätzlich prüfen:

- `settings.json` ist nicht enthalten
- `configuration-summary.json` enthält `remoteAccessPolicy = NetSupport-only`
- NetSupport-Produkt-/Dateiversion ist enthalten, sofern auslesbar
- der Erkennungsstatus der NetSupport-Installation ist enthalten
- NetSupport-Profilstatus und `/F`-Status sind enthalten
- echter Profilname ist bei Anonymisierung ersetzt
- Zielnamen sind bei Anonymisierung ersetzt
- Kennwörter/Credentials sind nicht enthalten

---

## Abnahmekriterium

Ein Build ist für den praktischen Pilotbetrieb geeignet, wenn:

- GitHub Actions vollständig grün ist
- RDP im Produkt weder sichtbar noch startbar ist
- NetSupport-Installation auf den vorgesehenen Admin-PCs korrekt erkannt oder bewusst auswählbar ist
- ein konfiguriertes Control-Profil auf dem Admin-PC erkannt und mit `/N` gestartet wird
- `/F` nur zusammen mit einem vorhandenen Profil verwendet werden kann
- ein fehlendes konfiguriertes Profil nicht stillschweigend umgangen wird
- NetSupport Control/View auf mindestens zwei typischen Ziel-PCs funktioniert
- eine manipulierte Fremd-EXE nicht über den NetSupport-Provider gestartet werden kann
- AD-Liste und Filter funktionieren
- Fehler eines optionalen Dienstes wie CIM die NetSupport-Fernwartung nicht blockieren
- Diagnose/Supportpaket keine Credentials enthalten

Erst danach sollte der Build breiter auf Admin-Arbeitsplätzen verteilt werden.
