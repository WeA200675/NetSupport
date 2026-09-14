# NetSupport Remote Admin

A small, extensible Windows frontend for launching remote administration sessions without relying on every NetSupport UI setting being replicated through the domain.

## Documentation

The maintained project documentation lives in:

- [`docs/PROJECT_OVERVIEW.md`](docs/PROJECT_OVERVIEW.md) – current architecture, functions, configuration, build and roadmap
- [`docs/DEVELOPMENT_LOG.md`](docs/DEVELOPMENT_LOG.md) – chronological development decisions and progress
- [`docs/RDP_SESSION.md`](docs/RDP_SESSION.md) – embedded RDP architecture, session events, scaling, security and session controls
- [`docs/TARGET_ORGANIZATION.md`](docs/TARGET_ORGANIZATION.md) – favorites, groups, preferred providers and list behavior
- [`docs/TESTING.md`](docs/TESTING.md) – how to download and validate the CI-generated Windows test build

## Goals

- compact UI that can remain in the Windows tray
- select a computer once and launch common remote actions with one click
- organize a larger computer fleet with favorites and user-defined groups
- choose a preferred remote provider per target
- launch NetSupport sessions directly by computer name or IP address
- discover domain computers without persisting every discovered machine
- show useful runtime computer information without creating a second inventory database
- keep application configuration independent from NetSupport profiles/GPO behavior
- allow additional remote backends and information sources without changing the UI
- provide Windows RDP as an alternative connection method

## User workflow

1. Enter a computer name/IP address or load computers from Active Directory.
2. Filter by text, group and/or favorites.
3. Select the target in the computer list.
4. Optionally load runtime details such as IP address, Windows version, logged-on user and model.
5. Set favorite/group/preferred provider and persist changes with **Speichern / Aktualisieren**.
6. Start the target's preferred control provider through **Standardverbindung starten** or double-click the target.
7. Direct NetSupport/RDP quick actions remain available independently from the preferred provider.

The application can be closed to the notification area and reopened from the tray icon.

## Target organization

Persisted targets support:

- `isFavorite` – favorite marker and favorite-only filtering
- `group` – free-form grouping such as Office, Workshop or Servers
- `preferredProviderId` – provider used by the standard/double-click connection

Favorites are sorted first. The text filter also searches group names, and a separate group selector can narrow the list further. If a saved preferred provider is unavailable, the application falls back to NetSupport when available and otherwise to another provider supporting `Control`.

See [`docs/TARGET_ORGANIZATION.md`](docs/TARGET_ORGANIZATION.md) for the full behavior.

## Current providers

### NetSupport Manager

The application looks for `PCICTLUI.EXE` in the usual 32-bit and 64-bit Program Files locations and supports Control, View, Chat, Inventory, Remote Command Prompt and File Transfer.

### Windows Remote Desktop

RDP supports an embedded session window based on Microsoft's nonscriptable Remote Desktop ActiveX control. The ActiveX host is isolated from the main UI behind `IRdpSessionLauncher`.

The embedded session currently provides:

- embedded RDP display
- connect / reconnect / disconnect
- fullscreen session window
- real Connecting / Connected / Login / Disconnect status
- Remote Desktop resolution status
- SmartSizing that can be toggled while connected
- multi-monitor sessions through `UseMultimon`
- optional user name and Windows/AD domain
- Windows credential prompting without storing passwords in this application
- persisted non-secret RDP preferences for saved targets
- detailed disconnect reason where the Microsoft control can provide one
- automatic reconnect status including attempt count and network availability
- optional clipboard redirection
- optional administrative RDP session
- remote Alt+Tab / app-switch action
- remote Start action
- remote Task Manager action where supported by the local RDP client/server combination
- fallback to `mstsc.exe`, including `/admin` and `/multimon` where configured

Set `useEmbeddedRdp` to `false` to force the external Windows Remote Desktop client.

## Active Directory discovery

Domain discovery is implemented behind `ITargetDiscoveryService`. The initial `DomainComputerDiscoveryService` invokes `Get-ADComputer`, therefore the admin workstation needs the Microsoft ActiveDirectory PowerShell module (RSAT).

Discovered computers are transient until they are explicitly saved. Availability checks are performed in parallel with bounded concurrency and update transient Online/Offline state only.

## Runtime computer details

Computer information is isolated behind `ITargetDetailsService`. The initial `PowerShellTargetDetailsService` resolves IP addresses locally and uses `Get-CimInstance` for remote Windows information.

The selected-computer card can show:

- IP address(es)
- logged-on Windows user
- Windows edition/version
- manufacturer/model
- last status/details check time

The CIM query uses the current Windows identity and normal WSMan policy. If remote CIM is blocked by firewall or policy, the rest of the application continues to work and the UI reports that management data is only partially available. Runtime details are not persisted because they can become stale.

## Configuration

Configuration is stored per user at:

```text
%AppData%\NetSupportRemoteAdmin\settings.json
```

Example:

```json
{
  "netSupportExecutable": "C:\\Program Files (x86)\\NetSupport\\NetSupport Manager\\PCICTLUI.EXE",
  "startMinimized": false,
  "useEmbeddedRdp": true,
  "useFullScreenRdp": false,
  "targets": [
    {
      "name": "PC-001",
      "host": "PC-001",
      "description": "Office",
      "isFavorite": true,
      "group": "Office",
      "preferredProviderId": "netsupport",
      "rdpUserName": "max.mustermann",
      "rdpDomain": "CONTOSO",
      "rdpRedirectClipboard": true,
      "rdpAdminSession": false,
      "rdpUseMultiMonitor": false
    }
  ]
}
```

RDP passwords are intentionally never stored in `settings.json`.

## Extending the application

Remote backends implement `IRemoteProvider`. Embedded RDP session hosts are accessed through `IRdpSessionLauncher`. Target sources implement `ITargetDiscoveryService`. Runtime computer information is provided through `ITargetDetailsService`.

This keeps the main UI independent from NetSupport, RDP ActiveX hosting, future VNC providers, Active Directory and other inventory sources.

## Build

Requirements:

- Windows 10/11
- .NET 8 SDK
- optional: RSAT ActiveDirectory PowerShell module for domain discovery
- optional: remote CIM/WSMan access for computer details
- NetSupport Manager Control for NetSupport actions

```powershell
dotnet restore NetSupport.sln
dotnet build NetSupport.sln --configuration Release
```

Run from Visual Studio or:

```powershell
dotnet run --project .\src\NetSupport.RemoteAdmin\NetSupport.RemoteAdmin.csproj
```

## CI test build

A successful GitHub Actions run also publishes a self-contained Windows x64 artifact named:

```text
NetSupport.RemoteAdmin-win-x64
```

It can be downloaded from the successful **Build** workflow run and tested without installing the .NET 8 runtime separately. See [`docs/TESTING.md`](docs/TESTING.md) for the validation checklist.

## Architecture

```text
MainWindow
    |
    +-- selected target action/details/organization card
    |
    +--> RemoteProviderRegistry
    |        |
    |        +-- NetSupportProvider --> PCICTLUI.EXE
    |        +-- RdpProvider
    |               |
    |               +--> IRdpSessionLauncher
    |               |       +--> EmbeddedRdpSessionLauncher
    |               |               +--> RdpSessionWindow
    |               |                       +--> RdpActiveXControl
    |               |
    |               +--> mstsc.exe fallback
    |
    +--> ITargetDiscoveryService
    |        +--> DomainComputerDiscoveryService --> Get-ADComputer
    |
    +--> ITargetDetailsService
    |        +--> PowerShellTargetDetailsService --> DNS + Get-CimInstance
    |
    +--> HostAvailabilityService

ConfigService --> %AppData%\NetSupportRemoteAdmin\settings.json
```
