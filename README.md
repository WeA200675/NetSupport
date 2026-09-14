# NetSupport Remote Admin

A small, extensible Windows frontend for launching remote administration sessions without relying on every NetSupport UI setting being replicated through the domain.

## Documentation

The maintained project documentation lives in:

- [`docs/PROJECT_OVERVIEW.md`](docs/PROJECT_OVERVIEW.md) – current architecture, functions, configuration, build and roadmap
- [`docs/DEVELOPMENT_LOG.md`](docs/DEVELOPMENT_LOG.md) – chronological development decisions and progress

## Goals

- compact UI that can remain in the Windows tray
- select a computer once and launch common remote actions with one click
- launch NetSupport sessions directly by computer name or IP address
- discover domain computers without persisting every discovered machine
- keep application configuration independent from NetSupport profiles/GPO behavior
- allow additional remote backends without changing the UI
- provide Windows RDP as an alternative connection method

## User workflow

1. Enter a computer name/IP address or load computers from Active Directory.
2. Select the target in the computer list.
3. Use the action card on the right for Control, View, RDP, Remote CMD, File Transfer, Inventory or Chat.
4. Double-clicking a target starts NetSupport control directly.
5. The generic provider/action selection remains available under **Advanced** for less common or future providers.

The application can be closed to the notification area and reopened from the tray icon.

## Current providers

### NetSupport Manager

The application looks for `PCICTLUI.EXE` in the usual 32-bit and 64-bit Program Files locations and supports Control, View, Chat, Inventory, Remote Command Prompt and File Transfer.

### Windows Remote Desktop

RDP now supports an embedded session window based on Microsoft's nonscriptable Remote Desktop ActiveX control. The ActiveX host is isolated from the main UI behind `IRdpSessionLauncher`.

The embedded session currently provides:

- embedded RDP display
- connect / reconnect
- disconnect
- fullscreen session window
- fallback to `mstsc.exe`

Set `useEmbeddedRdp` to `false` to force the external Windows Remote Desktop client.

## Active Directory discovery

Domain discovery is implemented behind `ITargetDiscoveryService`. The initial `DomainComputerDiscoveryService` invokes `Get-ADComputer`, therefore the admin workstation needs the Microsoft ActiveDirectory PowerShell module (RSAT).

Discovered computers are transient until they are explicitly saved. Availability checks are performed in parallel with bounded concurrency and update transient Online/Offline state only.

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
      "description": "Office"
    }
  ]
}
```

## Extending the application

Remote backends implement `IRemoteProvider`. Embedded RDP session hosts are accessed through `IRdpSessionLauncher`. Target sources implement `ITargetDiscoveryService`.

This keeps the main UI independent from NetSupport, RDP ActiveX hosting, future VNC providers, Active Directory and other inventory sources.

## Build

Requirements:

- Windows 10/11
- .NET 8 SDK
- optional: RSAT ActiveDirectory PowerShell module for domain discovery
- NetSupport Manager Control for NetSupport actions

```powershell
dotnet restore NetSupport.sln
dotnet build NetSupport.sln --configuration Release
```

Run from Visual Studio or:

```powershell
dotnet run --project .\src\NetSupport.RemoteAdmin\NetSupport.RemoteAdmin.csproj
```

## Architecture

```text
MainWindow
    |
    +-- selected target action card
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
    +--> HostAvailabilityService

ConfigService --> %AppData%\NetSupportRemoteAdmin\settings.json
```
