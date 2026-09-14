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

```text
RdpProvider
   |
   +--> IRdpSessionLauncher
           +--> EmbeddedRdpSessionLauncher
                   +--> RdpSessionWindow
                           +--> RdpActiveXControl
                                   +--> Microsoft MsTscAx.dll
```

Falls der eingebettete Client deaktiviert oder nicht verfügbar ist, wird `mstsc.exe` als Fallback gestartet.

---

## Bedienung

Das Session-Fenster enthält aktuell:

- Zielrechner und Sessionstatus
- optionalen Benutzernamen und Domäne
- **An Fenster anpassen**
- **Zwischenablage**
- **Admin-Sitzung**
- **Mehrere Monitore**
- **Alt+Tab remote**
- **Start remote**
- **Task-Manager**
- **Neu verbinden**
- **Vollbild / Fenstermodus**
- **Trennen**

Benutzername kann auch als `DOMÄNE\Benutzer` eingegeben werden. Änderungen an Zwischenablage, Admin-Sitzung und Multi-Monitor werden beim nächsten Verbinden beziehungsweise über **Neu verbinden** angewendet.

---

## Anmeldeinformationen und Sicherheit

Die Anwendung speichert **keine RDP-Passwörter**.

Vor einer Verbindung werden nur nicht geheime Werte und Präferenzen an das Microsoft-RDP-Control übergeben:

- Zielrechner
- optionaler Benutzername
- optionale Domäne
- Zwischenablageumleitung
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
  "rdpAdminSession": false,
  "rdpUseMultiMonitor": false
}
```

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

**An Fenster anpassen** verwendet `SmartSizing`. Bei einer Multi-Monitor-Sitzung wird SmartSizing nicht zusätzlich erzwungen, weil dann die native Monitoranordnung des RDP-Clients verwendet werden soll.

---

## Multi-Monitor

Die Option **Mehrere Monitore** setzt vor `Connect()` die RDP-Eigenschaft `UseMultimon` des `MsRdpClient12NotSafeForScripting`-Controls.

Verhalten:

- die Einstellung wird pro gespeichertem Ziel als `rdpUseMultiMonitor` erhalten
- sie gilt ab dem nächsten Verbindungsaufbau
- der eingebettete Client verwendet die lokalen Monitore nach den Regeln des Windows-RDP-Clients
- `SmartSizing` wird in diesem Modus nicht parallel aktiviert
- falls die lokale ActiveX-Registrierung `UseMultimon` nicht bereitstellt, fällt die Sitzung defensiv auf Einzelmonitor zurück

Beim externen Fallback wird dieselbe Präferenz als

```text
mstsc.exe /multimon
```

weitergegeben. Eine gleichzeitig konfigurierte Admin-Sitzung nutzt zusätzlich `/admin`.

### Auswahl bestimmter Monitore

Microsoft dokumentiert für das ActiveX-Control `UseMultimon`, also den Multi-Monitor-Modus als Ein/Aus-Eigenschaft. Eine dokumentierte ActiveX-Eigenschaft für eine Liste bestimmter lokaler Monitor-IDs wird in der verwendeten Schnittstelle nicht bereitgestellt.

Für normale RDP-Eigenschaften ist dagegen dokumentiert:

```text
use multimon:i:1
selectedmonitors:s:0,1
```

Die Monitor-IDs können beim Windows-RDP-Client mit

```text
mstsc.exe /l
```

ermittelt werden. Bei `selectedmonitors` müssen die ausgewählten Displays den RDP-Regeln entsprechen; unter anderem müssen sie zusammenhängend sein, und der zuerst angegebene Monitor wird zum primären Remote-Display.

Deshalb gilt als Designregel:

- eingebettetes RDP: nur dokumentiertes `UseMultimon`
- gezielte Monitor-ID-Auswahl: später über einen dokumentierten externen `.rdp`-/`mstsc`-Pfad
- keine undokumentierte dynamische COM-Eigenschaft nur deshalb setzen, um die Funktion scheinbar im eingebetteten Viewer anzubieten

---

## Zwischenablage

Die Option **Zwischenablage** steuert `RedirectClipboard`. Änderungen werden beim nächsten Verbindungsaufbau wirksam und für gespeicherte Rechner als `rdpRedirectClipboard` erhalten.

---

## Administrative Sitzung

**Admin-Sitzung** verwendet `ConnectToAdministerServer` und wird als `rdpAdminSession` gespeichert. Im `mstsc.exe`-Fallback wird `/admin` verwendet.

---

## Remote-Aktionen

Über `IMsRdpClient8.SendRemoteAction` stehen aktuell zur Verfügung:

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
- `/multimon`-Fallback für `mstsc.exe`

---

## Nächste Ausbauschritte

- gezielte Monitor-ID-Auswahl über einen externen `.rdp`-/`mstsc`-Pfad
- weitere Tastatur-/Sondertasten-Werkzeuge
- detailliertere RDP-Fehlertexte
- weitere Redirects wie Laufwerke oder Audio

Der allgemeine Startverlauf liegt nicht in dieser RDP-Schicht, sondern separat hinter `ISessionHistoryService`, weil er gleichermaßen NetSupport und RDP protokolliert.

---

## Wichtige Designregel

Die RDP-Implementierung bleibt hinter `IRdpSessionLauncher` und `RdpActiveXControl` gekapselt. Das Hauptfenster kennt keine COM-/ActiveX-Details und keine Passwörter.

Dokumentierte Microsoft-Schnittstellen werden undokumentierten COM-Workarounds vorgezogen.
