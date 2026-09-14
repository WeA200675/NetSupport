using System.Diagnostics;
using System.Reflection;
using Microsoft.Win32;

namespace NetSupport.RemoteAdmin.Services;

/// <summary>
/// Manages per-user startup through HKCU only. No elevation or machine-wide policy changes are required.
/// </summary>
public sealed class WindowsAutoStartService : IAutoStartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "NetSupportRemoteAdmin";

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
        }
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
                        ?? throw new InvalidOperationException("Windows-Autostart konnte nicht geöffnet werden.");

        if (!enabled)
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        key.SetValue(ValueName, BuildLaunchCommand(), RegistryValueKind.String);
    }

    private static string BuildLaunchCommand()
    {
        var processPath = Environment.ProcessPath
                          ?? Process.GetCurrentProcess().MainModule?.FileName
                          ?? throw new InvalidOperationException("Der aktuelle Programmpfad konnte nicht ermittelt werden.");

        var entryLocation = Assembly.GetEntryAssembly()?.Location;
        if (string.Equals(Path.GetFileName(processPath), "dotnet.exe", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(entryLocation))
        {
            return $"\"{processPath}\" \"{entryLocation}\"";
        }

        return $"\"{processPath}\"";
    }
}
