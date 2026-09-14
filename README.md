# NetSupport Remote Admin

A small, extensible Windows frontend for launching remote administration sessions without relying on every NetSupport UI setting being replicated through the domain.

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
3. Use the action card on the right for the common operations:
   - Control
   - View only
   - RDP
   - Remote command prompt
   - File transfer
   - Inventory
   - Chat
4. Double-clicking a target starts NetSupport control directly.
5. The generic provider/action selection remains available under **Advanced** for less common or future providers.

The application can be closed to the notification area and reopened from the tray icon.

## Current providers

### NetSupport Manager

The application looks for `PCICTLUI.EXE` in the usual 32-bit and 64-bit Program Files locations and supports:

- Control
- View
- Chat
- Inventory
- Remote command prompt
- File transfer

A custom executable path can be set in the generated `settings.json`.

### Windows Remote Desktop

Uses the Windows `mstsc.exe` client. Full-screen mode can be enabled in `settings.json`.

An embedded RDP host is intentionally treated as a separate future provider/window component so that ActiveX hosting does not leak into the main application UI.

## Active Directory discovery

Domain discovery is implemented behind `ITargetDiscoveryService`. The initial `DomainComputerDiscoveryService` invokes `Get-ADComputer`, therefore the admin workstation needs the Microsoft ActiveDirectory PowerShell module (RSAT).

Discovered computers are transient until they are explicitly saved. This prevents the local settings file from becoming a second copy of Active Directory.

Availability checks are performed in parallel with bounded concurrency and update transient Online/Offline state only.

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

## Extending remote providers

Remote backends implement `IRemoteProvider`:

```csharp
public interface IRemoteProvider
{
    string Id { get; }
    string DisplayName { get; }
    IReadOnlyCollection<RemoteAction> SupportedActions { get; }
    bool IsAvailable { get; }
    Task ConnectAsync(RemoteTarget target, RemoteAction action, CancellationToken cancellationToken = default);
}
```

Register a provider in `App.xaml.cs`; the advanced UI discovers the provider and supported actions dynamically.

## Extending target discovery

Target sources implement `ITargetDiscoveryService`. This allows the current Active Directory implementation to be replaced or complemented later by CSV, SCCM, Intune, an inventory API, or another directory service.

## Build

Requirements:

- Windows 10/11
- .NET 8 SDK
- optional: RSAT ActiveDirectory PowerShell module for domain discovery

```powershell
dotnet build NetSupport.sln
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
    |        +-- RdpProvider --------> mstsc.exe
    |        +-- future providers
    |
    +--> ITargetDiscoveryService
    |        |
    |        +-- DomainComputerDiscoveryService --> Get-ADComputer
    |        +-- future discovery sources
    |
    +--> HostAvailabilityService

ConfigService --> %AppData%\NetSupportRemoteAdmin\settings.json
```
