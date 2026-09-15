# Active-Directory-Rechnersuche

> Die Schaltfläche **Domäne laden** liest Computerobjekte aus Active Directory ausschließlich für die lokale Zielauswahl. Es werden keine AD-Objekte, Gruppenrichtlinien oder Client-Einstellungen verändert.

## Reihenfolge der Erkennung

Die Anwendung verwendet Windows PowerShell und versucht zwei read-only Wege:

1. **RSAT / `Get-ADComputer`**, wenn das Modul `ActiveDirectory` lokal verfügbar ist.
2. **LDAP-Fallback über `System.DirectoryServices.DirectorySearcher`**, wenn RSAT nicht installiert ist.

Damit ist RSAT nicht mehr zwingend erforderlich. Auf einem Domänen-PC mit funktionierendem LDAP-Zugriff kann **Domäne laden** auch ohne das ActiveDirectory-PowerShell-Modul funktionieren.

## Gelesene Felder

Pro Computerobjekt werden nur folgende Werte ausgewertet:

```text
Name
DNSHostName
Description
```

Wenn `DNSHostName` fehlt, wird `Name` als Verbindungsziel verwendet. Doppelte Hosts werden unabhängig von Groß-/Kleinschreibung zusammengeführt.

## LDAP-Fallback

Der Fallback ermittelt zuerst über:

```text
LDAP://RootDSE
```

 den `defaultNamingContext` der aktuellen Domäne. Anschließend wird darin mit einem paginierten `DirectorySearcher` nach Computerobjekten gesucht.

Filter:

```text
(&(objectCategory=computer)(objectClass=computer))
```

`PageSize` ist auf 1000 gesetzt, damit auch Domänen mit mehr als 1000 Computerobjekten vollständig seitenweise gelesen werden können.

## Zeitlimit

Die gesamte PowerShell-/AD-Abfrage ist auf **30 Sekunden** begrenzt.

Wenn dieses Limit erreicht wird, beendet die Anwendung den gestarteten PowerShell-Prozess bestmöglich und meldet einen verständlichen Timeout. Dadurch bleibt ein nicht erreichbarer Domain Controller bzw. ein blockierter LDAP-Pfad nicht unbegrenzt im UI hängen.

## Voraussetzungen

Mindestens einer der beiden Wege muss funktionieren:

- RSAT / ActiveDirectory-PowerShell-Modul, oder
- LDAP-Zugriff aus der aktuellen Windows-Sitzung auf die Domäne.

Typische Ursachen für einen Fehler sind:

- Rechner ist nicht mit der Domäne bzw. dem Unternehmensnetz/VPN verbunden
- Domain Controller ist nicht erreichbar
- DNS-Auflösung der Domäne funktioniert nicht
- LDAP wird durch Firewall/Netzsegmentierung blockiert
- aktuelles Benutzerkonto darf die benötigten AD-Computerattribute nicht lesen

Es werden von dieser Anwendung keine separaten AD-Zugangsdaten gespeichert oder übertragen. Die Abfrage läuft im Sicherheitskontext der aktuellen Windows-Sitzung.

## Systemzustand

**Erweitert → Einstellungen → Systemzustand** unterscheidet jetzt:

- RSAT vorhanden → bevorzugter `Get-ADComputer`-Pfad verfügbar
- RSAT fehlt, LDAP verfügbar → LDAP-Fallback verfügbar
- beide nicht verfügbar → Warnung

Die Systemzustandsseite führt dabei keine vollständige Domänenrechnersuche aus. Sie prüft nur lokal, welcher Discovery-Pfad grundsätzlich verfügbar ist.

## Grenzen

Die aktuelle Suche liest alle gefundenen Computerobjekte. Sie filtert bewusst noch nicht nach:

- deaktivierten Computeraccounts
- letztem Logon
- Betriebssystem
- Organisationseinheit (OU)

In älteren oder großen Active-Directory-Umgebungen können daher auch veraltete Computerobjekte in der Liste erscheinen. Für die eigentliche Fernwartung bleibt die NetSupport-Erreichbarkeitsprüfung separat maßgeblich.

## Sicherheitsgrenze

Die Discovery-Funktion führt ausschließlich Leseoperationen aus. Insbesondere werden **nicht** verändert:

- Computerobjekte
- Gruppenmitgliedschaften
- Gruppenrichtlinien
- NetSupport-Client-Konfigurationen
- Windows-Firewallregeln
- Registrywerte auf Zielrechnern
