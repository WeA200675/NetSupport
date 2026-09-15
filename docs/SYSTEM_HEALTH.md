# Systemzustand des Admin-PCs

> Diese Datei beschreibt die lokale Zustandsprüfung von **NetSupport Remote Admin**.

---

## Ziel

Die Seite **Erweitert → Einstellungen → Systemzustand** zeigt, ob die wichtigsten lokalen Voraussetzungen auf dem Admin-PC verfügbar sind.

Die Prüfung scannt **keine Domänenrechner** und verändert keine Remote-Systeme. Sie prüft ausschließlich lokale Komponenten und Konfigurationen.

---

## Remotezugriffsrichtlinie

Der erste Check bestätigt die aktuelle Betriebsregel:

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

angelegt und beschrieben werden kann.

Dazu wird kurz eine temporäre Testdatei erzeugt und direkt wieder gelöscht.

Ein Fehler an dieser Stelle kann unter anderem Konfiguration, History, Diagnoseprotokoll und Supportpakete beeinträchtigen.

---

### NetSupport Manager

Geprüft wird der konfigurierte Pfad zu:

```text
PCICTLUI.EXE
```

Mögliche Zustände:

- **OK** – Datei existiert.
- **Fehler** – kein Pfad konfiguriert oder Datei nicht vorhanden.

Der Pfad kann unter **Erweitert → Einstellungen** geändert werden.

---

### Active Directory / RSAT

Über eine lokale, nicht interaktive Windows-PowerShell-Abfrage wird geprüft, ob das Modul

```text
ActiveDirectory
```

vorhanden ist.

Dieses Modul wird für **Domäne laden** benötigt.

Ein fehlendes Modul blockiert die NetSupport-Schnellaktionen nicht, sondern nur die AD-Discovery-Funktion.

---

### CIM / WSMan

Die Anwendung führt lokal eine einfache Abfrage aus:

```powershell
Get-CimInstance Win32_OperatingSystem
```

Damit wird geprüft, ob die lokale CIM-/PowerShell-Grundlage funktioniert.

Wichtig: Ein grüner lokaler Check garantiert **nicht**, dass ein entfernter Rechner per CIM erreichbar ist. Für Remote-CIM müssen zusätzlich Berechtigungen, Firewall, WSMan/WinRM und Zielkonfiguration stimmen.

CIM/WSMan wird nur für zusätzliche Rechnerdetails verwendet. NetSupport selbst hängt davon nicht ab.

---

### Windows-Autostart

Der Systemzustand zeigt informativ an, ob der Benutzer-Autostart aktiv ist.

Registry-Wert:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\NetSupportRemoteAdmin
```

Ein deaktivierter Autostart ist kein Fehler.

---

### Diagnoseprotokoll

Der Systemzustand zeigt an, ob das optionale Diagnoseprotokoll aktiviert ist.

Pfad:

```text
%AppData%\NetSupportRemoteAdmin\logs\application.log
```

Ein deaktiviertes Protokoll ist kein Fehler, erschwert aber die spätere Fehlersuche.

---

## Zeitlimits

PowerShell-basierte Checks besitzen ein lokales Zeitlimit von ungefähr acht Sekunden.

Dadurch kann eine defekte PowerShell-/CIM-Umgebung das Systemzustandsfenster nicht unbegrenzt blockieren.

---

## Statusstufen

Die Oberfläche unterscheidet:

- **OK** – Voraussetzung ist vorhanden und der Check war erfolgreich.
- **Info** – neutraler Zustand, z. B. Autostart aus.
- **Hinweis** – optionale oder nur für Teilfunktionen benötigte Voraussetzung fehlt.
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
                           +--> Dateisystem
                           +--> NetSupport-Pfad
                           +--> powershell.exe
                           +--> ActiveDirectory-Modul
                           +--> lokales CIM
                           +--> Autostart/Diagnosezustand
```

Modelle:

```text
Models/SystemHealthCheckResult.cs
```

Services:

```text
Services/ISystemHealthService.cs
Services/SystemHealthService.cs
```

---

## Sicherheits- und Betriebsprinzip

Der Systemzustand ist ein **lokaler Diagnosecheck**. Er führt keine Remote-Aktionen aus, speichert keine Credentials und ändert keine GPO-/HKLM-Einstellungen.

Damit kann die Seite auf einem neuen Admin-PC verwendet werden, um vor dem produktiven Einsatz fehlende Voraussetzungen zu erkennen.
