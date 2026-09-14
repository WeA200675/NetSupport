# NetSupport Remote Admin

A small, extensible Windows frontend for launching remote administration sessions without relying on every NetSupport UI setting being replicated through the domain.

## Goals

- compact UI that can remain in the Windows tray
- launch NetSupport sessions directly by computer name or IP address
- keep application configuration independent from NetSupport profiles/GPO behavior
- allow additional remote backends without changing the UI
- provide Windows RDP as an alternative connection method

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

## Extending the application

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

Register a provider in `App.xaml.cs`; the main window discovers the supported actions dynamically.

This keeps UI code independent of NetSupport, RDP, VNC or future embedded viewers.

## Build

Requirements:

- Windows 10/11
- .NET 8 SDK

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
    v
RemoteProviderRegistry
    |
    +-- NetSupportProvider --> PCICTLUI.EXE
    |
    +-- RdpProvider --------> mstsc.exe
    |
    +-- future providers

ConfigService --> %AppData%\NetSupportRemoteAdmin\settings.json
```
