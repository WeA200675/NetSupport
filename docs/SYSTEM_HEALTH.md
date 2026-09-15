# Systemzustand des Admin-PCs

> Diese Datei beschreibt die lokale Zustandsprüfung von **NetSupport Remote Admin**.

---

## Ziel

Die Seite **Erweitert → Einstellungen → Systemzustand** zeigt, ob die wichtigsten lokalen Voraussetzungen auf dem Admin-PC verfügbar sind.

Die Prüfung scannt **keine Domänenrechner** und verändert keine Remote-Systeme. Sie prüft ausschließlich lokale Komponenten und Konfigurationen.

---

## Remotezugriffsrichtlinie

Der erste Check bestätigt:

```text
NetSupport-only
```

RDP wird nicht als Remote-Provider registriert und ist kein Bestandteil der lokalen Zustandsprüfung.

Details: [`DOMAIN_REMOTE_POLICY.md`](DOMAIN_REMOTE_POLICY.md).

---

## Geprüfte Komponenten

### Anwendungsdaten

Geprüft wird, ob

```text
%AppData%\NetSupportRemoteAdmin
```

angelegt und beschrieben werden kann. Dazu wird kurz eine temporäre Testdatei erzeugt und direkt wieder gelöscht.

---

### NetSupport Manager

Der Check verwendet dieselbe Installationserkennung wie die Einstellungsseite.

Quellen:

- aktuell konfigurierter Pfad
- Standardpfad unter Program Files (x86)
- Standardpfad unter Program Files
- Windows-Uninstall-Registry in HKLM/HKCU und 32-/64-Bit-Sicht

Es gibt drei typische Ergebnisse:

#### OK – konfigurierter Pfad gültig

Die konfigurierte `PCICTLUI.EXE` existiert. Zusätzlich werden soweit verfügbar angezeigt:

- Produktname
- Produktversion
- Dateiversion
- Hersteller
- Erkennungsquelle

#### Hinweis – konfigurierter Pfad defekt, andere Installation gefunden

Beispiel: Ein alter Admin-PC-Pfad zeigt auf eine nicht mehr vorhandene Installation, aber eine gültige `PCICTLUI.EXE` wurde an anderer Stelle erkannt.

Dann kann unter **Einstellungen → Automatisch erkennen** oder **NetSupport prüfen…** der gefundene Pfad übernommen werden.

#### Fehler – keine Installation auffindbar

Wenn weder der konfigurierte Pfad noch bekannte lokale Quellen eine `PCICTLUI.EXE` liefern, wird ein Fehler angezeigt.

NetSupport bleibt dann bewusst der einzige Remote-Provider; es gibt keinen alternativen Fernwartungs-Fallback.

---

### Active Directory / RSAT

Über eine lokale, nicht interaktive Windows-PowerShell-Abfrage wird geprüft, ob das Modul

```text
ActiveDirectory
```

vorhanden ist.

Dieses Modul wird für **Domäne laden** benötigt. Ein fehlendes Modul blockiert die NetSupport-Schnellaktionen nicht.

---

### CIM / WSMan

Die Anwendung führt lokal eine einfache Abfrage aus:

```powershell
Get-CimInstance Win32_OperatingSystem
```

Ein grüner lokaler Check garantiert nicht, dass ein entfernter Rechner per CIM erreichbar ist. Für Remote-CIM müssen zusätzlich Berechtigungen, Firewall und Zielkonfiguration stimmen.

CIM/WSMan wird nur für zusätzliche Rechnerdetails verwendet. NetSupport selbst hängt davon nicht ab.

---

### Windows-Autostart

Der Systemzustand zeigt informativ an, ob der Benutzer-Autostart aktiv ist.

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\NetSupportRemoteAdmin
```

Ein deaktivierter Autostart ist kein Fehler.

---

### Diagnoseprotokoll

Der Systemzustand zeigt an, ob das optionale Diagnoseprotokoll aktiviert ist.

```text
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

Ein deaktiviertes Protokoll ist kein Fehler, erschwert aber die spätere Fehlersuche.

---

## Zeitlimits

PowerShell-basierte Checks besitzen ein lokales Zeitlimit von ungefähr acht Sekunden. Dadurch kann eine defekte PowerShell-/CIM-Umgebung das Systemzustandsfenster nicht unbegrenzt blockieren.

Die NetSupport-Installationserkennung selbst ist lokal und durchsucht keine Laufwerke rekursiv.

---

## Statusstufen

- **OK** – Voraussetzung ist vorhanden und der Check war erfolgreich.
- **Info** – neutraler Zustand, z. B. Autostart aus.
- **Hinweis** – optionale Voraussetzung fehlt oder eine reparierbare Abweichung wurde gefunden.
- **Fehler** – zentrale lokale Voraussetzung fehlt oder ein notwendiger Pfad ist nicht nutzbar.

---

## Architektur

```text
SettingsWindow
   |
   +--> SystemHealthWindow
           |
           +--> ISystemHealthService
                   |
                   +--> SystemHealthService
                           +--> INetSupportInstallationService
                           +--> Dateisystem
                           +--> powershell.exe
                           +--> ActiveDirectory-Modul
                           +--> lokales CIM
                           +--> Autostart/Diagnosezustand
```

NetSupport-Installationserkennung:

```text
INetSupportInstallationService
   +--> NetSupportInstallationService
```

Modelle:

```text
Models/SystemHealthCheckResult.cs
Models/NetSupportInstallationCandidate.cs
```

---

## Sicherheits- und Betriebsprinzip

Der Systemzustand ist ein **lokaler Diagnosecheck**. Er führt keine Remote-Aktionen aus, speichert keine Credentials und ändert keine GPO-/HKLM-Einstellungen.

Damit kann die Seite auf einem neuen Admin-PC verwendet werden, um vor dem produktiven Einsatz fehlende oder veraltete Voraussetzungen zu erkennen.
