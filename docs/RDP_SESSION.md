# Eingebettete RDP-Session

> Technische und funktionale Dokumentation des eingebetteten Windows-Remote-Desktop-Viewers in **NetSupport Remote Admin**.

---

## Ziel

Die RDP-Integration soll sich wie eine klassische Remoteverbindung bedienen lassen, ohne dass für jede Sitzung ein separates `mstsc.exe`-Fenster verwaltet werden muss.

Dafür wird Microsofts **Remote Desktop ActiveX Control** in einem eigenen Session-Fenster der Anwendung gehostet. NetSupport bleibt davon vollständig getrennt und kann parallel weiterhin für Control, View, Dateiübertragung, Inventar und weitere Funktionen verwendet werden.

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

Das Control wird über `AxHost` eingebettet. Die Anwendung greift dynamisch auf die COM-Schnittstellen zu und benötigt deshalb keine generierten `AxInterop.MSTSCLib`-Assemblies im Repository.

Architektur:

```text
RdpProvider
   |
   +--> IRdpSessionLauncher
           |
           +--> EmbeddedRdpSessionLauncher
                   |
                   +--> RdpSessionWindow
                           |
                           +--> RdpActiveXControl
                                   |
                                   +--> Microsoft MsTscAx.dll
```

Falls der eingebettete Client deaktiviert oder nicht verfügbar ist, kann weiterhin `mstsc.exe` als Fallback gestartet werden.

---

## Bedienung

Eine RDP-Session wird über die Schnellaktion **RDP** am ausgewählten Rechner gestartet.

Das Session-Fenster enthält:

- Zielrechner und aktuellen Sessionstatus
- optionalen Benutzernamen
- optionale Windows-/AD-Domäne
- Schalter **An Fenster anpassen**
- **Neu verbinden**
- **Vollbild / Fenstermodus**
- **Trennen**

Der Benutzername kann auch in der Form

```text
DOMÄNE\Benutzer
```

eingegeben werden. Wenn das separate Domänenfeld leer ist, trennt die Anwendung diesen Wert automatisch in Domäne und Benutzername auf.

---

## Anmeldeinformationen und Sicherheit

Die Anwendung speichert **keine RDP-Passwörter**.

Vor einer Verbindung werden nur folgende nicht geheimen Werte an das Microsoft-RDP-Control übergeben:

- Zielrechner
- optionaler Benutzername
- optionale Domäne

Das Passwort wird vom normalen Windows-/RDP-Credential-Dialog angefordert. `AllowPromptingForCredentials` bleibt aktiviert.

Das Speichern von Credentials durch das eingebettete Control wird mit `AllowCredentialSaving = false` deaktiviert.

Für dauerhaft gespeicherte Zielrechner dürfen Benutzername und Domäne in `settings.json` gespeichert werden:

```json
{
  "name": "PC-001",
  "host": "PC-001",
  "rdpUserName": "max.mustermann",
  "rdpDomain": "CONTOSO"
}
```

Kennwörter gehören ausdrücklich **nicht** in diese Datei.

---

## Sessionstatus und Ereignisse

Die ActiveX-Ereignisse werden über `IMsTscAxEvents` abgegriffen und als .NET-Ereignisse aus `RdpActiveXControl` bereitgestellt.

| RDP-Ereignis | Anzeige / Verwendung |
|---|---|
| `OnConnecting` | Verbindungsaufbau läuft |
| `OnConnected` | Transport ist hergestellt, Anmeldung läuft |
| `OnLoginComplete` | Sitzung ist erfolgreich angemeldet |
| `OnDisconnected` | Trennungsgrund und Extended Disconnect Reason werden ausgewertet |
| `OnFatalError` | RDP-Fehlercode wird angezeigt |
| `OnRemoteDesktopSizeChange` | aktuelle Remote-Auflösung wird im Status angezeigt |

Für Disconnects versucht die Anwendung zusätzlich über `GetErrorDescription` eine verständliche Meldung vom Microsoft-Control zu erhalten. Falls das nicht möglich ist, werden die numerischen Reason-Codes angezeigt.

---

## Skalierung

Die Option **An Fenster anpassen** verwendet die RDP-Eigenschaft `SmartSizing`.

Damit wird der Remote-Desktop auf den verfügbaren Clientbereich skaliert. Die Einstellung kann auch während einer aktiven Sitzung geändert werden.

Die ursprüngliche Desktopgröße wird beim Verbindungsaufbau aus der verfügbaren Größe des Session-Fensters ermittelt. Das ActiveX-Control selbst füllt den `WindowsFormsHost` vollständig aus.

---

## Authentifizierung

Für die RDP-Verbindung wird CredSSP aktiviert, sofern das lokale Microsoft-Control die entsprechende Advanced-Settings-Schnittstelle bereitstellt.

Die Konfiguration ist bewusst defensiv implementiert: Fehlt eine optionale Advanced-Settings-Eigenschaft auf einem System, bleibt die Basisverbindung weiterhin verwendbar.

---

## Dateien im Projekt

```text
src/NetSupport.RemoteAdmin/
├── Controls/
│   └── RdpActiveXControl.cs
├── Models/
│   ├── RemoteTarget.cs
│   └── RdpSessionEvents.cs
├── Services/
│   ├── IRdpSessionLauncher.cs
│   └── EmbeddedRdpSessionLauncher.cs
└── Views/
    ├── RdpSessionWindow.xaml
    └── RdpSessionWindow.xaml.cs
```

---

## Aktueller Stand

### Phase 1

- eingebettetes RDP-Control
- eigenes Session-Fenster
- Verbinden / Neu verbinden
- Trennen
- Vollbild
- `mstsc.exe`-Fallback

### Phase 2

- echte Connecting-/Connected-/Login-/Disconnect-Ereignisse
- verständlichere Disconnect-Informationen
- Fatal-Error-Anzeige
- Remote-Auflösungsanzeige
- SmartSizing während der Sitzung
- Benutzername und Domäne
- Windows-Credential-Prompt
- keine Passwortspeicherung
- Speicherung von Benutzername/Domäne für bereits gespeicherte Zielrechner

---

## Nächste Ausbauschritte

Geplant sind insbesondere:

- Multi-Monitor-Unterstützung
- Clipboard-Einstellungen
- bessere Behandlung von Auto-Reconnect
- detailliertere RDP-Fehlertexte
- optionale administrative RDP-Sitzung
- Tastatur-/Sondertasten-Werkzeuge
- Session-Historie bzw. letzte Verbindung

---

## Wichtige Designregel

Die RDP-Implementierung bleibt hinter `IRdpSessionLauncher` und `RdpActiveXControl` gekapselt.

Das Hauptfenster soll weder COM-/ActiveX-Details noch Credential-Handling kennen. Dadurch kann der eingebettete Viewer später ersetzt oder erweitert werden, ohne die allgemeine Remote-Admin-Oberfläche neu zu bauen.
