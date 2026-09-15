# Domänenrichtlinie für Remotezugriff

> **Betriebsregel:** In dieser Domäne wird Fernwartung ausschließlich über **NetSupport Manager** durchgeführt.

---

## Hintergrund

Windows Remote Desktop (RDP) ist in der Domäne als Fernwartungsweg deaktiviert und darf für diesen Einsatzzweck nicht mehr verwendet werden.

Gründe für die Entscheidung sind insbesondere:

- einheitliche Nachvollziehbarkeit der Fernwartung
- unterschiedliche bzw. problematische RDP-Verhaltensweisen auf verschiedenen PC-Systemen
- zentrale Festlegung auf NetSupport Manager als vorgesehenes Support-/Remote-Control-Werkzeug

**NetSupport Remote Admin** bildet diese Betriebsregel technisch ab und versucht nicht, die Domänenvorgabe zu umgehen.

---

## Technische Umsetzung

Die Anwendung registriert im produktiven Startpfad ausschließlich:

```text
NetSupportProvider
```

als ausführbaren Remote-Provider.

```text
App
  |
  +--> RemoteProviderRegistry
          |
          +--> NetSupportProvider
                  |
                  +--> PCICTLUI.EXE
```

Es gibt im produktiven UI keine RDP-Schaltfläche, keine RDP-Monitoroptionen und keine RDP-Einstellungen.

Zusätzlich prüft `MainWindow` vor jedem Provider-Start nochmals, dass die Provider-ID

```text
netsupport
```

ist. Ein anderer Provider wird mit einem Richtlinienhinweis blockiert.

---

## Entfernte RDP-Bestandteile

Die frühere Entwicklungsphase enthielt testweise RDP-Unterstützung. Nach Klärung der Domänenvorgabe wurden die aktiven Bestandteile wieder aus dem Projekt entfernt, darunter:

- `RdpProvider`
- eingebettetes RDP-ActiveX-Control
- RDP-Sessionfenster
- `.rdp`-Dateierzeugung
- RDP-Monitorwahl
- RDP-Audio-/Geräteumleitung
- RDP-spezifische Remote-Aktionen

Damit enthält der normale Build keinen alternativen RDP-Remote-Control-Pfad mehr.

---

## Migration alter Konfigurationen

Ältere Entwicklungsstände konnten in `settings.json` noch RDP-Felder bzw. `preferredProviderId: "rdp"` enthalten.

Die aktuelle Version verhält sich so:

1. unbekannte/alte RDP-Felder werden beim Einlesen ignoriert,
2. ein nicht zugelassener `preferredProviderId` wird in der Laufzeit auf `netsupport` normalisiert,
3. beim nächsten Speichern wird die Konfiguration im aktuellen NetSupport-only-Datenmodell geschrieben.

Es ist daher keine manuelle Bearbeitung alter Konfigurationsdateien erforderlich.

---

## Zulässige Remote-Aktionen

Über NetSupport werden aktuell angeboten:

- **Steuern**
- **Nur ansehen**
- **Chat**
- **Inventar**
- **Remote CMD**
- **Dateiübertragung**

Die tatsächliche NetSupport-Berechtigung und Protokollierung bleibt weiterhin Sache der vorhandenen NetSupport-Installation und deren administrativer Konfiguration.

---

## Systemzustand

Die lokale Systemzustandsprüfung kontrolliert deshalb **keine RDP-Komponenten** mehr.

Relevant sind stattdessen:

- Remotezugriffsrichtlinie = NetSupport-only
- Pfad und Verfügbarkeit von `PCICTLUI.EXE`
- AppData-Schreibbarkeit
- Active Directory / RSAT
- CIM / WSMan für optionale Rechnerdetails
- Windows-Autostart
- Diagnoseprotokoll

Details: [`SYSTEM_HEALTH.md`](SYSTEM_HEALTH.md).

---

## Erweiterbarkeit

Die Provider-Schnittstelle bleibt absichtlich erhalten, damit die Anwendung architektonisch erweiterbar bleibt.

Eine spätere Erweiterung um einen anderen Remote-Provider darf jedoch nur erfolgen, wenn dieser in der Umgebung **explizit freigegeben** wurde. Die aktuelle Produktkonfiguration und Dokumentation gehen ausschließlich von NetSupport Manager aus.

---

## Sicherheitsprinzip

Die Anwendung soll bestehende Domänen- und Sicherheitsvorgaben unterstützen, nicht umgehen.

Deshalb gilt:

```text
Domänenvorgabe > Komfortfunktion
```

Wenn eine Remote-Technik in der Domäne nicht zugelassen ist, wird sie nicht als Ausweichweg in dieses Tool eingebaut.
