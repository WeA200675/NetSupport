# Systemzustand des Admin-PCs

> **Erweitert → Einstellungen → Systemzustand** prüft lokale Voraussetzungen des Admin-PCs. Es werden dabei keine Domänenrechner gescannt und keine Remote-Systeme verändert.

## Geprüfte Bereiche

### Remotezugriffsrichtlinie

Der Check bestätigt die produktive Richtlinie:

```text
NetSupport-only
```

RDP ist weder Provider noch Diagnose-Fallback.

### Anwendungsdaten

Geprüft wird, ob

```text
%AppData%\NetSupportRemoteAdmin
```

angelegt und beschrieben werden kann. Eine temporäre Testdatei wird unmittelbar wieder gelöscht.

### NetSupport Manager

Die konfigurierte `PCICTLUI.EXE` wird mit derselben bounded Installationserkennung wie die Einstellungsseite geprüft. Soweit verfügbar werden Produkt-/Dateiversion, Hersteller und Erkennungsquelle angezeigt.

Ist der konfigurierte Pfad defekt, aber eine andere gültige lokale `PCICTLUI.EXE` auffindbar, erscheint ein reparierbarer Hinweis. Es gibt keinen alternativen Fernwartungs-Provider.

### NetSupport Control-Profil

Optional konfigurierte lokale Control-Profile werden read-only unter

```text
HKCU\Software\NetSupport Ltd\PCICTL\ConfigList
```

geprüft. `/F` ohne Profil, ungültige Profilnamen oder fehlende konfigurierte Profile werden als Fehler gemeldet, weil auch der produktive Provider in diesen Fällen bewusst nicht auf ein anderes Profil zurückfällt.

### NetSupport Client-Port

Der konfigurierte Diagnose-Port wird lokal validiert:

```text
TCP 5405
```

ist der Standardwert, kann aber in den Einstellungen geändert werden.

Der Systemzustand baut **keine** TCP-Verbindung zu Zielrechnern auf. Der Port wird erst über **Status prüfen** gegen die geladenen Ziele getestet. Ein fehlgeschlagener Porttest blockiert keinen NetSupport-Start.

### Active Directory

Die Zustandsprüfung unterscheidet zwei read-only Discovery-Wege:

1. **RSAT / `Get-ADComputer` vorhanden** – bevorzugter Weg.
2. **RSAT fehlt, LDAP verfügbar** – `LDAP://RootDSE` kann den Domänen-Namenskontext ermitteln; die Anwendung kann den read-only LDAP-Fallback verwenden.

Sind beide Wege nicht verfügbar, erscheint eine Warnung. Die Systemzustandsseite führt dabei keine vollständige Domänenrechnersuche aus.

Details: [`ACTIVE_DIRECTORY_DISCOVERY.md`](ACTIVE_DIRECTORY_DISCOVERY.md).

### CIM / WSMan

Lokal wird eine einfache `Win32_OperatingSystem`-CIM-Abfrage ausgeführt. Ein erfolgreicher lokaler Test garantiert nicht, dass ein entfernter Rechner per CIM erreichbar ist; Firewall und Berechtigungen können pro Ziel abweichen.

CIM/WSMan dient nur zusätzlichen Rechnerdetails und ist keine Voraussetzung für NetSupport.

### Windows-Autostart

Informativ wird der Benutzer-Autostart unter

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\NetSupportRemoteAdmin
```

angezeigt. Ein deaktivierter Autostart ist kein Fehler.

### Diagnoseprotokoll

Der Check zeigt, ob das optionale lokale Diagnoseprotokoll aktiviert ist:

```text
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

## Zeitlimits

PowerShell-basierte lokale Health-Checks besitzen ein Zeitlimit von ungefähr acht Sekunden. Die vollständige Domänenrechnersuche besitzt separat ein Limit von 30 Sekunden.

Dadurch sollen defekte PowerShell-, LDAP- oder CIM-Wege die Bedienoberfläche nicht unbegrenzt blockieren.

## Statusstufen

- **OK** – Voraussetzung ist vorhanden bzw. der konfigurierte lokale Wert ist gültig.
- **Info** – neutraler Zustand, z. B. Autostart aus oder kein festes NetSupport-Profil.
- **Hinweis** – optionale Voraussetzung fehlt oder eine reparierbare Abweichung wurde gefunden.
- **Fehler** – zentrale lokale Konfiguration ist ungültig oder würde einen produktiven NetSupport-Start blockieren.

## Sicherheitsgrenze

Der Systemzustand ist ein lokaler Diagnosecheck. Er:

- führt keine NetSupport-Remoteaktion aus
- scannt keine Zielrechner
- speichert keine Credentials
- ändert keine GPOs
- ändert keine Ziel-Firewall
- ändert keine AD-Objekte
- schreibt nicht in die NetSupport-`ConfigList`

Damit kann er auf einem Admin-PC gefahrlos zur Vorprüfung der lokalen Voraussetzungen verwendet werden.
