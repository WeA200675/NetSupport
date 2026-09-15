# Gezielte RDP-Monitorwahl

> Diese Datei beschreibt die Auswahl bestimmter lokaler Monitore für Windows Remote Desktop in **NetSupport Remote Admin** und den gemeinsamen externen `.rdp`-Verbindungsweg.

---

## Ziel

Der normale Multi-Monitor-Modus verwendet alle für Windows RDP geeigneten lokalen Monitore. Für Arbeitsplätze mit drei oder mehr Displays soll zusätzlich eine feste Teilmenge auswählbar sein, zum Beispiel nur Monitor `0` und `2`.

---

## Monitor-IDs ermitteln

Im Hauptfenster befindet sich unter **Erweitert → Gezielte RDP-Monitore** die Schaltfläche **IDs anzeigen**.

Sie startet:

```text
mstsc.exe /l
```

Windows zeigt daraufhin die lokal erkannten RDP-Monitore und deren IDs an.

Beispiel einer gewünschten Auswahl:

```text
0,1
```

Die erste angegebene ID wird von Windows RDP als primärer Monitor der Remotesitzung behandelt.

---

## Speichern

Die kommagetrennte Auswahl wird pro Zielrechner als

```json
"rdpSelectedMonitors": "0,1"
```

in

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

gespeichert.

Die Eingabe wird beim **Speichern / Aktualisieren** normalisiert und validiert:

- nur nichtnegative Ganzzahlen
- Trennung mit Komma
- Leerzeichen werden entfernt
- doppelte IDs werden entfernt
- leeres Feld deaktiviert die gezielte Auswahl

Beispiel:

```text
0, 1, 1
```

wird zu:

```text
0,1
```

---

## Verbindungsweg

### Keine Monitor-IDs eingetragen

Bei aktiviertem eingebettetem RDP wird weiterhin der interne ActiveX-Viewer verwendet.

Ist eingebettetes RDP global deaktiviert oder nicht verfügbar, nutzt der externe Fallback ebenfalls eine kleine von der Anwendung erzeugte `.rdp`-Datei. Dadurch können nicht geheime RDP-Präferenzen im eingebetteten und externen Weg konsistent gehalten werden.

### Monitor-IDs eingetragen

Die Anwendung erzeugt eine `.rdp`-Datei unter:

```text
%AppData%\NetSupportRemoteAdmin\rdp\
```

und startet sie mit dem Windows-RDP-Client.

Die Datei kann beispielsweise enthalten:

```text
full address:s:<ziel>
screen mode id:i:2
use multimon:i:1
selectedmonitors:s:0,1
redirectclipboard:i:1
audiomode:i:0
audiocapturemode:i:0
drivestoredirect:s:
```

Damit werden neben der Monitorwahl auch die aktuellen nicht geheimen Präferenzen für Zwischenablage, Audio, Mikrofon und Laufwerke übernommen. Eine Admin-Sitzung wird weiterhin über `/admin` angefordert.

Die erzeugte Datei enthält **keinen Benutzernamen, kein Passwort und keine gespeicherten Credentials**.

---

## Allgemeiner externer RDP-Fallback

`RdpConnectionFileService` wird inzwischen nicht nur für `selectedmonitors`, sondern für den gesamten externen `mstsc.exe`-Fallback verwendet.

Die generierte Datei transportiert aktuell:

- Zielrechner
- Fenster-/Vollbildmodus
- All-Monitor-Modus
- optionale ausgewählte Monitor-IDs
- Zwischenablageumleitung
- Audioausgabemodus
- Mikrofonumleitung
- Laufwerksumleitung

Dadurch müssen diese Einstellungen nicht auf verschiedene Kommandozeilen- und COM-Wege verteilt werden.

---

## Warum die gezielte Auswahl aktuell extern bleibt

Microsoft dokumentiert `selectedmonitors` als RDP-Eigenschaft für die Windows-Remotedesktopverbindung. Neuere Microsoft-Dokumentation führt außerdem `SelectedMonitors` als benannte Eigenschaft von `IMsRdpExtendedSettings` auf.

Das Projekt hostet das ActiveX-Control absichtlich ohne generierte `AxInterop.MSTSCLib`-/`MSTSCLib`-Assemblies und greift auf die üblichen Client-/Advanced-Settings dynamisch zu.

Für die gezielte Auswahl wurde deshalb der nachvollziehbare `.rdp`-Dateipfad gewählt. Dadurch muss die bisher stabile COM-Kapselung nicht nur für diese Funktion um ein zusätzliches typisiertes IUnknown-Interop-Interface erweitert werden.

Ein späterer Ausbau kann `IMsRdpExtendedSettings.SelectedMonitors` sauber typisiert anbinden und die gezielte Auswahl auch im eingebetteten Viewer ermöglichen.

---

## Microsoft-Regeln für selectedmonitors

Die Windows-RDP-Seite erwartet insbesondere:

- `use multimon:i:1`
- eine kommagetrennte Liste lokaler Monitor-IDs
- zusammenhängende ausgewählte Displays entsprechend der RDP-Monitoranordnung
- die erste ID als primären Remote-Monitor

Die Anwendung prüft bewusst nur die Syntax der IDs. Ob die gewählte Kombination für die aktuelle physische Monitoranordnung zulässig ist, entscheidet der Windows-RDP-Client.

Damit vermeiden wir eine zweite, möglicherweise abweichende Implementierung der Windows-Monitorregeln.

---

## Sicherheit

Es werden keine Passwörter oder RDP-Credentials in `.rdp`-Dateien geschrieben.

Der Dateiname basiert auf einem kurzen Hash des Zielnamens, nicht auf frei eingegebenen Dateipfaden. Der Zielwert selbst darf keine Zeilenumbrüche enthalten, damit zusätzliche RDP-Eigenschaften nicht über den Hostnamen eingeschleust werden können.

Die Laufwerksumleitung ist in der Anwendung standardmäßig deaktiviert. Wenn sie aktiviert wird, schreibt der externe Weg `drivestoredirect:s:*`; andernfalls bleibt der Wert leer.

---

## Architektur

```text
RdpProvider
   |
   +--> IRdpSessionLauncher
   |       +--> EmbeddedRdpSessionLauncher
   |
   +--> IRdpConnectionFileService
           +--> RdpConnectionFileService
                   +--> %AppData%\NetSupportRemoteAdmin\rdp\connection-<hash>.rdp
                   +--> mstsc.exe
```

Der Service kapselt:

- Normalisierung der Monitor-IDs
- Validierung
- Erzeugung credential-freier `.rdp`-Dateien
- gemeinsame externe RDP-Präferenzen
- Aufruf von `mstsc.exe /l`

---

## Rückkehr zum eingebetteten RDP

Das Feld **Gezielte RDP-Monitore** leeren und **Speichern / Aktualisieren** drücken.

Beim nächsten RDP-Start verwendet das Ziel wieder den normalen eingebetteten Viewer, sofern dieser global aktiviert und auf dem Admin-PC verfügbar ist.
