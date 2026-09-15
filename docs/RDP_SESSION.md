# Eingebettete RDP-Session

> Technische und funktionale Dokumentation des Windows-Remote-Desktop-Viewers in **NetSupport Remote Admin**.

---

## Ziel

Die RDP-Integration soll sich wie eine klassische Remoteverbindung bedienen lassen, ohne dass für jede normale Sitzung ein separates `mstsc.exe`-Fenster verwaltet werden muss.

Dafür wird Microsofts **Remote Desktop ActiveX Control** in einem eigenen Session-Fenster der Anwendung gehostet. Für Funktionen, die bewusst über dokumentierte RDP-Dateieigenschaften abgebildet werden, steht zusätzlich der externe Windows-RDP-Client zur Verfügung.

NetSupport bleibt davon vollständig getrennt und kann parallel weiterhin für Control, View, Dateiübertragung, Inventar und weitere Funktionen verwendet werden.

---

## Technische Grundlage

Verwendetes Microsoft-Control:

```text
MsRdpClient12NotSafeForScripting
```

CLSID:

```text
3F859AA3-C2D4-4FAA-B0E4-FD0C9C4E5E3A
```

Das Control wird über `AxHost` eingebettet. Die Anwendung greift dynamisch auf Client-/Advanced-Settings zu und benötigt deshalb keine generierten `AxInterop.MSTSCLib`-Assemblies im Repository.

```text
RdpProvider
   |
   +--> IRdpSessionLauncher
   |       +--> EmbeddedRdpSessionLauncher
   |               +--> RdpSessionWindow
   |                       +--> RdpActiveXControl
   |                               +--> Microsoft MsTscAx.dll
   |
   +--> IRdpConnectionFileService
           +--> RdpConnectionFileService
                   +--> credential-freie .rdp-Datei
                   +--> mstsc.exe
```

Falls der eingebettete Client deaktiviert oder nicht verfügbar ist, wird `mstsc.exe` als Fallback verwendet.

---

## Bedienung

Das eingebettete Session-Fenster enthält aktuell:

- Zielrechner und Sessionstatus
- optionalen Benutzernamen und Domäne
- **An Fenster anpassen**
- **Zwischenablage**
- **Laufwerke**
- **Mikrofon**
- **Audio**: lokal / remote / aus
- **Admin-Sitzung**
- **Mehrere Monitore**
- **Alt+Tab remote**
- **Start remote**
- **Task-Manager**
- **Neu verbinden**
- **Vollbild / Fenstermodus**
- **Trennen**

Benutzername kann auch als `DOMÄNE\Benutzer` eingegeben werden. Änderungen an Redirection-/Audio-/Admin-/Multi-Monitor-Optionen werden beim nächsten Verbindungsaufbau beziehungsweise über **Neu verbinden** angewendet.

Die **gezielte Monitorwahl** wird im Hauptfenster unter **Erweitert** konfiguriert, weil sie pro Ziel gespeichert wird und aktuell einen externen `.rdp`-Verbindungsweg verwendet.

---

## Anmeldeinformationen und Sicherheit

Die Anwendung speichert **keine RDP-Passwörter**.

Vor einer eingebetteten Verbindung werden nur nicht geheime Werte und Präferenzen an das Microsoft-RDP-Control übergeben:

- Zielrechner
- optionaler Benutzername
- optionale Domäne
- Zwischenablageumleitung
- Laufwerksumleitung
- Mikrofonumleitung
- Audioausgabemodus
- Admin-Sitzung
- Multi-Monitor an/aus

Das Passwort wird vom normalen Windows-/RDP-Credential-Dialog angefordert. `AllowPromptingForCredentials` bleibt aktiviert; `AllowCredentialSaving` ist deaktiviert.

Beispiel eines gespeicherten Ziels:

```json
{
  "name": "PC-001",
  "host": "PC-001",
  "rdpUserName": "max.mustermann",
  "rdpDomain": "CONTOSO",
  "rdpRedirectClipboard": true,
  "rdpRedirectDrives": false,
  "rdpRedirectMicrophone": false,
  "rdpAudioRedirectionMode": 0,
  "rdpAdminSession": false,
  "rdpUseMultiMonitor": false,
  "rdpSelectedMonitors": "0,1"
}
```

Auch erzeugte `.rdp`-Dateien enthalten **kein Passwort und keine gespeicherten Credentials**.

---

## Sessionstatus und Ereignisse

Die ActiveX-Ereignisse werden über `IMsTscAxEvents` abgegriffen und als .NET-Ereignisse bereitgestellt.

| RDP-Ereignis | Anzeige / Verwendung |
|---|---|
| `OnConnecting` | Verbindungsaufbau läuft |
| `OnConnected` | Transport hergestellt, Anmeldung läuft |
| `OnLoginComplete` | Sitzung erfolgreich angemeldet |
| `OnDisconnected` | Trennungsgrund und Extended Disconnect Reason |
| `OnFatalError` | RDP-Fehlercode |
| `OnRemoteDesktopSizeChange` | aktuelle Remote-Auflösung |
| `OnAutoReconnecting2` | Versuchszähler, Netzstatus und Trennungsgrund |
| `OnAutoReconnected` | erfolgreiche automatische Wiederverbindung |

Für Disconnects versucht die Anwendung zusätzlich über `GetErrorDescription` einen verständlichen Text zu erhalten.

---

## Automatische Wiederverbindung

Windows RDP besitzt eine eigene Auto-Reconnect-Funktion. Das Tool startet deshalb keine parallele Reconnect-Schleife, sondern zeigt die vom Microsoft-Control gemeldeten Versuche und die erfolgreiche Wiederverbindung an.

---

## Skalierung

**An Fenster anpassen** verwendet `SmartSizing`. Bei einer normalen Multi-Monitor-Sitzung wird SmartSizing nicht zusätzlich erzwungen, weil dann die native Monitoranordnung des RDP-Clients verwendet werden soll.

---

## Zwischenablage

**Zwischenablage** steuert `RedirectClipboard` und wird pro gespeichertem Ziel als `rdpRedirectClipboard` erhalten.

Im externen `.rdp`-Pfad wird dieselbe Präferenz über

```text
redirectclipboard:i:0|1
```

übernommen.

---

## Laufwerksumleitung

**Laufwerke** steuert im eingebetteten ActiveX-Control `RedirectDrives`.

Aus Sicherheitsgründen ist die Option standardmäßig **deaktiviert**. Wenn sie aktiviert ist, können lokale Laufwerke innerhalb der Remotesitzung verfügbar werden; die tatsächlich angebotenen Laufwerke und Sicherheitsdialoge bleiben unter Kontrolle des Windows-RDP-Clients.

Im externen Pfad wird die Einstellung über

```text
drivestoredirect:s:*
```

beziehungsweise bei deaktivierter Umleitung über einen leeren `drivestoredirect`-Wert ausgedrückt.

Persistenz pro Ziel:

```text
rdpRedirectDrives
```

---

## Mikrofonumleitung

**Mikrofon** steuert `AudioCaptureRedirectionMode` im eingebetteten Client. Die Option ist standardmäßig deaktiviert und wird beim nächsten Verbindungsaufbau wirksam.

Im externen `.rdp`-Pfad:

```text
audiocapturemode:i:0|1
```

Persistenz:

```text
rdpRedirectMicrophone
```

---

## Audioausgabe

Die Auswahl **Audio** kennt drei Modi:

| UI | Wert | Verhalten |
|---|---:|---|
| Auf diesem Computer | `0` | Remote-Audio wird lokal wiedergegeben |
| Auf dem Remotecomputer | `1` | Audio bleibt auf dem Zielrechner |
| Kein Audio | `2` | Audio wird nicht wiedergegeben |

Im ActiveX-Control wird `AudioRedirectionMode` gesetzt. Im externen Pfad wird derselbe Wert als

```text
audiomode:i:0|1|2
```

in die `.rdp`-Datei geschrieben.

Persistenz:

```text
rdpAudioRedirectionMode
```

Ungültige Werte werden defensiv auf `0` normalisiert.

---

## Multi-Monitor – alle Monitore

Die Option **Mehrere Monitore** im eingebetteten Viewer setzt vor `Connect()` die RDP-Eigenschaft `UseMultimon` des `MsRdpClient12NotSafeForScripting`-Controls.

Verhalten:

- die Einstellung wird pro gespeichertem Ziel als `rdpUseMultiMonitor` erhalten
- sie gilt ab dem nächsten Verbindungsaufbau
- der eingebettete Client verwendet die lokalen Monitore nach den Regeln des Windows-RDP-Clients
- `SmartSizing` wird in diesem Modus nicht parallel aktiviert
- falls die lokale ActiveX-Registrierung `UseMultimon` nicht bereitstellt, fällt die Sitzung defensiv auf Einzelmonitor zurück

Beim externen Fallback wird dieselbe Präferenz über die erzeugte `.rdp`-Datei mit `use multimon` weitergegeben. Eine konfigurierte Admin-Sitzung nutzt zusätzlich `/admin`.

---

## Gezielte Auswahl bestimmter Monitore

Windows kann die lokalen RDP-Monitor-IDs mit

```text
mstsc.exe /l
```

anzeigen.

Im Hauptfenster steht dafür unter **Erweitert → Gezielte RDP-Monitore** die Schaltfläche **IDs anzeigen** bereit.

Eine Auswahl wie

```text
0,1
```

wird als `rdpSelectedMonitors` gespeichert.

Wenn dieses Feld gesetzt ist, nutzt `RdpProvider` bewusst den externen Windows-RDP-Client mit einer generierten `.rdp`-Datei. Diese enthält unter anderem:

```text
full address:s:<ziel>
use multimon:i:1
selectedmonitors:s:0,1
redirectclipboard:i:1
audiomode:i:0
audiocapturemode:i:0
drivestoredirect:s:
```

Die Dateien liegen unter:

```text
%AppData%\NetSupportRemoteAdmin\rdp\
```

Die allgemeinen RDP-Redirection-Präferenzen werden dabei genauso übernommen wie beim normalen externen Fallback. Eine Admin-Sitzung wird weiterhin über `/admin` gestartet.

### Validierung

Die Anwendung prüft:

- nichtnegative Ganzzahlen
- Kommatrennung
- Entfernung von Leerzeichen
- Entfernung doppelter IDs

Die Windows-Regel, dass die ausgewählten Monitore für die konkrete lokale Anordnung zulässig beziehungsweise zusammenhängend sein müssen, wird bewusst dem Windows-RDP-Client überlassen.

### Warum für diese Phase extern?

Microsoft dokumentiert `selectedmonitors` als RDP-Eigenschaft. Aktuelle Microsoft-Dokumentation führt außerdem `SelectedMonitors` als benannte Eigenschaft von `IMsRdpExtendedSettings` auf.

Unsere ActiveX-Kapselung ist bislang absichtlich sehr leichtgewichtig und verwendet keine generierten MSTSCLib-Interop-Assemblies. Für die erste produktionsnahe Implementierung der gezielten Auswahl verwenden wir deshalb den transparenten `.rdp`-Dateipfad.

Später kann `IMsRdpExtendedSettings.SelectedMonitors` sauber typisiert angebunden werden, um dieselbe Auswahl auch im eingebetteten Viewer zu unterstützen.

Ausführlicher: [`RDP_SELECTED_MONITORS.md`](RDP_SELECTED_MONITORS.md).

---

## Administrative Sitzung

**Admin-Sitzung** verwendet im eingebetteten Viewer `ConnectToAdministerServer` und wird als `rdpAdminSession` gespeichert. Im externen Weg wird `/admin` verwendet.

---

## Remote-Aktionen

Über `IMsRdpClient8.SendRemoteAction` stehen im eingebetteten Viewer aktuell zur Verfügung:

| Schaltfläche | RDP-Aktion |
|---|---|
| **Alt+Tab remote** | Remote-App-Switch |
| **Start remote** | Remote-Start-Aktion |
| **Task-Manager** | Remote-Task-Manager, sofern unterstützt |

Nicht unterstützte Aktionen werden als Statusmeldung angezeigt und beenden die Session nicht.

---

## Authentifizierung

CredSSP wird aktiviert, sofern die lokale Advanced-Settings-Schnittstelle verfügbar ist. Optionale ActiveX-Eigenschaften sind defensiv gekapselt, damit die Basisverbindung auf abweichenden Windows-Versionen weiterhin funktioniert.

---

## Externer RDP-Pfad

Wenn eingebettetes RDP deaktiviert/nicht verfügbar ist oder gezielte Monitor-IDs konfiguriert sind, erstellt `RdpConnectionFileService` eine kleine `.rdp`-Datei und startet `mstsc.exe` damit.

Damit werden folgende nicht geheime Präferenzen auch extern konsistent übertragen:

- Bildschirmmodus
- Multi-Monitor / ausgewählte Monitore
- Zwischenablage
- Laufwerke
- Mikrofon
- Audioausgabe

Die Datei enthält keinen RDP-Benutzernamen, kein Passwort und keine gespeicherten Credentials. Eine Admin-Sitzung wird über `/admin` ergänzt.

---

## Aktueller Stand

### Phase 1

- eingebettetes RDP-Control
- Session-Fenster
- Verbinden / Neu verbinden / Trennen
- Vollbild
- `mstsc.exe`-Fallback

### Phase 2

- Session-Ereignisse und Disconnect-Informationen
- SmartSizing
- Benutzername/Domäne
- Windows-Credential-Prompt
- keine Passwortspeicherung

### Phase 3

- Zwischenablage
- Admin-Sitzung
- Remote-Aktionen
- persistente nicht geheime RDP-Präferenzen

### Phase 4

- Auto-Reconnect-Status
- Versuchszähler und Netzverfügbarkeit
- Multi-Monitor über `UseMultimon`
- externer Multi-Monitor-Fallback

### Phase 5

- Anzeige lokaler Monitor-IDs über `mstsc /l`
- `rdpSelectedMonitors` pro Ziel
- dokumentierte `selectedmonitors`-RDP-Datei
- externe RDP-Verbindung bei expliziter Monitor-ID-Auswahl
- Syntaxvalidierung und sichere Dateierzeugung ohne Credentials

### Phase 6

- Laufwerksumleitung, standardmäßig aus
- Mikrofonumleitung, standardmäßig aus
- Audioausgabe lokal / remote / aus
- Persistenz der nicht geheimen Präferenzen für gespeicherte Ziele
- identische Redirection-/Audioeinstellungen im externen `.rdp`-Fallback

---

## Nächste Ausbauschritte

- optional typisierte Anbindung von `IMsRdpExtendedSettings.SelectedMonitors` für den eingebetteten Viewer
- weitere Tastatur-/Sondertasten-Werkzeuge
- detailliertere RDP-Fehlertexte
- optional differenziertere Laufwerksauswahl statt `alle/keine`

Der allgemeine Startverlauf liegt nicht in dieser RDP-Schicht, sondern separat hinter `ISessionHistoryService`, weil er gleichermaßen NetSupport und RDP protokolliert.

---

## Wichtige Designregel

Die RDP-Implementierung bleibt hinter `IRdpSessionLauncher`, `RdpActiveXControl` und `IRdpConnectionFileService` gekapselt. Das Hauptfenster kennt keine COM-Details und keine Passwörter.

Dokumentierte Microsoft-Schnittstellen werden undokumentierten COM-Workarounds vorgezogen.
