# Testplan – NetSupport Remote Admin

> Praktische Prüfschritte für den Windows-x64-Testbuild.

---

## Grundregel

Für diese Domäne gilt:

```text
Remotezugriff ausschließlich über NetSupport Manager
```

RDP darf nicht als Fernwartungsweg angeboten oder gestartet werden.

Vor jedem produktiven Test deshalb zuerst prüfen:

- kein RDP-Button sichtbar
- keine RDP-Einstellungen vorhanden
- keine RDP-Monitoroptionen vorhanden
- Providerliste enthält nur NetSupport

Details: [`DOMAIN_REMOTE_POLICY.md`](DOMAIN_REMOTE_POLICY.md).

---

## Testbuild beziehen

Ein erfolgreicher GitHub-Actions-Lauf erzeugt das Artefakt:

```text
NetSupport.RemoteAdmin-win-x64
```

Vorgehen:

1. erfolgreichen Workflow-Lauf öffnen
2. unter **Artifacts** `NetSupport.RemoteAdmin-win-x64` herunterladen
3. ZIP in einen Testordner entpacken
4. `NetSupport.RemoteAdmin.exe` starten

Der Build ist self-contained; eine separat installierte .NET-8-Laufzeit ist auf dem Test-PC nicht erforderlich.

---

## 1. Programmstart und Tray

Prüfen:

- Hauptfenster öffnet sich
- Hinweis **NetSupport-only / RDP nicht angeboten** ist sichtbar
- Schließen des Fensters beendet die Anwendung nicht, sondern minimiert in den Infobereich
- Doppelklick auf das Tray-Symbol öffnet das Fenster wieder
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
- RSAT/ActiveDirectory-Modul wird korrekt erkannt
- lokales CIM/WSMan wird geprüft
- Autostartzustand wird angezeigt
- Diagnosezustand wird angezeigt
- **Neu prüfen** funktioniert

Die Seite darf keine RDP-Komponenten prüfen.

---

## 3. NetSupport-Pfad

Unter **Einstellungen**:

1. gültige `PCICTLUI.EXE` auswählen
2. speichern
3. prüfen, dass NetSupport ohne Programmneustart als Provider verfügbar ist
4. testweise ungültigen Pfad eingeben
5. Warnung prüfen

Typischer Installationspfad:

```text
C:\Program Files (x86)\NetSupport\NetSupport Manager\PCICTLUI.EXE
```

Der tatsächliche Pfad kann je Installation abweichen.

---

## 4. Direkte Zielverbindung

Mit einem bekannten Testrechner prüfen:

- Computername eingeben
- **Verbinden** startet die gewählte NetSupport-Aktion
- IP-Adresse eingeben
- **Steuern** startet NetSupport Control
- **Nur ansehen** startet View

Bei einem absichtlich ungültigen Ziel muss die Anwendung selbst stabil bleiben.

---

## 5. NetSupport-Schnellaktionen

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
- Schließen der jeweiligen Funktion verhält sich entsprechend der verwendeten NetSupport-CLI-Option
- Fehler werden verständlich im Hauptfenster bzw. Dialog angezeigt

---

## 6. Domänenrichtlinien-Sperre

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

## 7. Active Directory

Wenn RSAT verfügbar ist:

1. **Domäne laden**
2. prüfen, dass Domänenrechner erscheinen
3. gespeicherte Favoriten/Gruppen dürfen nicht verloren gehen
4. mehrfaches Laden darf keine unnötigen Duplikate erzeugen

Wenn RSAT fehlt, muss eine verständliche Warnung erscheinen und die manuelle NetSupport-Nutzung weiterhin funktionieren.

---

## 8. Statusprüfung

Mit mehreren Zielen:

- **Status prüfen**
- Online-/Offline-Anzeige prüfen
- Anwendung während der parallelen Prüfung weiterhin bedienen

Hinweis: Ping ist nur ein Erreichbarkeitshinweis. Ein fehlgeschlagener Ping beweist nicht zwingend, dass der PC ausgeschaltet ist.

---

## 9. Rechnerdetails

Für einen erreichbaren Test-PC:

- **Rechnerdetails laden**
- IP-Adresse prüfen
- angemeldeten Benutzer prüfen
- Windows-Version prüfen
- Hersteller/Modell prüfen
- Zeitstempel prüfen

Bei blockiertem CIM/WSMan muss die Anwendung weiter nutzbar bleiben und die NetSupport-Aktionen dürfen nicht blockiert werden.

---

## 10. Favoriten und Gruppen

Prüfen:

- Favorit setzen
- Gruppe vergeben
- **Speichern / Aktualisieren**
- Anwendung neu starten
- Werte bleiben erhalten
- **Nur Favoriten** funktioniert
- Gruppenfilter funktioniert
- Suchfeld findet auch Gruppennamen
- Favoriten stehen oben

Nicht gespeicherte Änderungen dürfen durch einen bloßen Verbindungsstart nicht automatisch dauerhaft werden.

---

## 11. Gespeicherte Ansichten

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

## 12. Verlauf

Mehrere NetSupport-Aktionen starten und prüfen:

- Einträge erscheinen unter **Zuletzt verwendet**
- Zeitpunkt ist korrekt
- Provider ist NetSupport
- Aktion ist korrekt
- fehlgeschlagene Starts werden als Fehler markiert
- Doppelklick übernimmt nur das Ziel und startet nicht automatisch eine neue Sitzung
- Verlauf ist auf 100 Einträge begrenzt

---

## 13. CSV-Export

**CSV exportieren** verwenden und in Excel/LibreOffice öffnen.

Prüfen:

- UTF-8-Umlaute korrekt
- Semikolontrennung korrekt
- Spalten vollständig
- Fehlertexte korrekt escaped

---

## 14. Autostart

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

## 15. Diagnoseprotokoll

Diagnose aktivieren und einige Aktionen ausführen.

Prüfen:

```text
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

Erwartete Inhalte:

- Start/Ende
- NetSupport-Aktionsstarts
- Zielhostname
- AD-/Statusvorgänge
- Fehler
- NetSupport-only-Richtlinienhinweis

Nicht enthalten sein dürfen Kennwörter oder Sitzungsinhalte.

---

## 16. Supportpaket

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
- Zielnamen sind bei Anonymisierung ersetzt
- Kennwörter/Credentials sind nicht enthalten

---

## Abnahmekriterium

Ein Build ist für den praktischen Pilotbetrieb geeignet, wenn:

- GitHub Actions vollständig grün ist
- RDP im Produkt weder sichtbar noch startbar ist
- NetSupport Control/View auf mindestens zwei typischen Ziel-PCs funktioniert
- AD-Liste und Filter funktionieren
- Fehler eines optionalen Dienstes wie CIM die NetSupport-Fernwartung nicht blockieren
- Diagnose/Supportpaket keine Credentials enthalten

Erst danach sollte der Build breiter auf Admin-Arbeitsplätzen verteilt werden.
