# NetSupport Control-Profile

> Diese Datei beschreibt die optionale Verwendung benannter NetSupport-Manager-Control-Konfigurationen in **NetSupport Remote Admin**.

---

## Ziel

NetSupport Manager kann mehrere Control-Konfigurationen verwalten. Damit lassen sich Bedien- und Funktionsvorgaben zentral in einem benannten lokalen Profil bündeln, statt einzelne Einstellungen in der Remote-Admin-Oberfläche nachzubauen.

Die Anwendung kann ein vorhandenes Control-Profil beim Start von `PCICTLUI.EXE` explizit laden.

NetSupport dokumentiert dafür:

```text
/N <Profil>
```

und optional:

```text
/F
```

`/F` wird nur zusammen mit `/N` verwendet und beschränkt den Control auf das gewählte Profil.

Offizielle NetSupport-Quelle:

- `https://kb.netsupportsoftware.com/knowledge-base/netsupport-manager-control-command-line-options/`

NetSupport empfiehlt bei Profilen, die Standardkonfiguration unverändert zu lassen, um sich nicht aus dem Control auszusperren.

---

## Lokale Profilquelle

Die Anwendung liest vorhandene Profilnamen ausschließlich aus dem Benutzerzweig:

```text
HKCU\Software\NetSupport Ltd\PCICTL\ConfigList
```

Jeder Unterschlüssel wird als vorhandene Control-Konfiguration behandelt.

Beispiel:

```text
ConfigList
├── Standard
├── Helpdesk
└── NurAnsehen
```

Die Anwendung schreibt **nicht** in diesen NetSupport-Schlüssel. Profile werden weiterhin in NetSupport Manager selbst erstellt und gepflegt.

---

## Einstellungen

Unter:

```text
Erweitert → Einstellungen → NetSupport Manager → Control-Konfiguration
```

stehen zur Verfügung:

- Auswahl bzw. Eingabe eines Profilnamens
- **Profile neu laden**
- **Control auf dieses Profil festlegen (/F)**

Ein leeres Profilfeld bedeutet:

```text
kein /N und kein /F
```

NetSupport startet dann mit seinem normalen Standardverhalten.

---

## Kommandozeile

Ohne Profil:

```text
PCICTLUI.EXE /c PC-001 /vc /e
```

Mit Profil:

```text
PCICTLUI.EXE /n "Helpdesk" /c PC-001 /vc /e
```

Mit fester Profilbindung:

```text
PCICTLUI.EXE /f /n "Helpdesk" /c PC-001 /vc /e
```

Die Ziel- und Aktionsparameter bleiben unverändert.

---

## Sicherheitsverhalten

### Profilname

Vor dem Einsetzen in die rohe NetSupport-Kommandozeile wird der Profilname validiert.

Nicht erlaubt sind unter anderem:

- leere Namen
- Namen über 128 Zeichen
- Anführungszeichen
- Steuerzeichen und Zeilenumbrüche

Leerzeichen und normale Unicode-Zeichen können verwendet werden.

### Profil muss lokal vorhanden sein

Wenn `netSupportProfileName` gesetzt ist, prüft der Provider vor jedem Start, ob das Profil unter dem HKCU-ConfigList-Schlüssel tatsächlich vorhanden ist.

Fehlt das Profil, wird die Remote-Aktion blockiert.

Die Anwendung fällt bewusst **nicht** stillschweigend auf ein anderes oder das Standardprofil zurück. Damit kann eine erwartete eingeschränkte Control-Konfiguration nicht unbemerkt umgangen werden.

### `/F` nur mit `/N`

`netSupportLockProfile = true` ohne Profilname ist ungültig.

Die Einstellungsseite verhindert diese Kombination beim Speichern; der Provider prüft sie zusätzlich vor dem Start.

---

## Konfiguration

Beispiel in `settings.json`:

```json
{
  "netSupportExecutable": "C:\\Program Files (x86)\\NetSupport\\NetSupport Manager\\PCICTLUI.EXE",
  "netSupportProfileName": "Helpdesk",
  "netSupportLockProfile": true
}
```

Ohne festes Profil:

```json
{
  "netSupportProfileName": null,
  "netSupportLockProfile": false
}
```

---

## Systemzustand

Der lokale Systemzustand zeigt einen eigenen Eintrag **NetSupport Control-Profil**.

Mögliche Zustände:

- **Info** – kein festes Profil konfiguriert; NetSupport-Standardverhalten wird verwendet
- **OK** – konfiguriertes Profil ist lokal vorhanden
- **Fehler** – Profilname ungültig, Profil fehlt oder `/F` ist ohne Profil aktiviert

Die Prüfung ist rein lokal und baut keine Verbindung zu einem Zielrechner auf.

---

## Supportpaket

`configuration-summary.json` enthält:

```text
netSupportProfileConfigured
netSupportProfileName
netSupportProfileAvailable
netSupportProfileLocked
netSupportDiscoveredProfileCount
```

Bei aktiver Anonymisierung wird der tatsächliche Profilname durch

```text
netsupport-profile
```

ersetzt. Derselbe Alias wird auf kopierte Diagnoseprotokolle angewendet.

---

## Abgrenzung zu GPO und Registry-Verteilung

Diese Funktion verteilt keine NetSupport-Konfiguration auf andere PCs und schreibt keine GPOs.

Sie löst ein anderes Problem: Der lokale Admin-Control kann beim Start deterministisch auf eine bereits vorhandene NetSupport-Control-Konfiguration festgelegt werden.

Dadurch muss die eigene Remote-Admin-Oberfläche nicht jede einzelne NetSupport-UI-Option nachimplementieren.

Wenn Profile zentral ausgerollt werden sollen, sollte dafür weiterhin ein administrativ freigegebener NetSupport-/Windows-Verteilungsweg verwendet werden.
