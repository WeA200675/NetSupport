using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Services;

public sealed class NetSupportInstallationService : INetSupportInstallationService
{
    public IReadOnlyList<NetSupportInstallationCandidate> Discover(string? configuredPath = null)
    {
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        AddCandidate(paths, configuredPath, "Konfiguriert");
        AddCandidate(
            paths,
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "NetSupport",
                "NetSupport Manager",
                "PCICTLUI.EXE"),
            "Program Files (x86)");
        AddCandidate(
            paths,
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "NetSupport",
                "NetSupport Manager",
                "PCICTLUI.EXE"),
            "Program Files");

        foreach (var registryCandidate in ReadRegistryCandidates())
            AddCandidate(paths, registryCandidate.Path, registryCandidate.Source);

        return paths
            .Select(pair => Inspect(pair.Key, pair.Value))
            .OrderByDescending(candidate => candidate.IsUsable)
            .ThenByDescending(candidate => candidate.Exists)
            .ThenBy(candidate => candidate.Source, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public NetSupportInstallationCandidate Inspect(string path, string source = "Manuell")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var normalized = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
        var exists = File.Exists(normalized);
        var isControlExecutable = string.Equals(
            Path.GetFileName(normalized),
            "PCICTLUI.EXE",
            StringComparison.OrdinalIgnoreCase);

        if (!exists)
        {
            return new NetSupportInstallationCandidate
            {
                Path = normalized,
                Source = source,
                Exists = false,
                IsControlExecutable = isControlExecutable
            };
        }

        try
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(normalized);
            return new NetSupportInstallationCandidate
            {
                Path = normalized,
                Source = source,
                Exists = true,
                IsControlExecutable = isControlExecutable,
                ProductName = NullIfWhiteSpace(versionInfo.ProductName),
                ProductVersion = NullIfWhiteSpace(versionInfo.ProductVersion),
                FileVersion = NullIfWhiteSpace(versionInfo.FileVersion),
                CompanyName = NullIfWhiteSpace(versionInfo.CompanyName),
                LastWriteTime = File.GetLastWriteTimeUtc(normalized)
            };
        }
        catch
        {
            return new NetSupportInstallationCandidate
            {
                Path = normalized,
                Source = source,
                Exists = true,
                IsControlExecutable = isControlExecutable,
                LastWriteTime = SafeGetLastWriteTime(normalized)
            };
        }
    }

    public string? FindBestExecutable(string? configuredPath = null) =>
        Discover(configuredPath).FirstOrDefault(candidate => candidate.IsUsable)?.Path;

    private static IReadOnlyList<(string Path, string Source)> ReadRegistryCandidates()
    {
        var result = new List<(string Path, string Source)>();
        var roots = new[]
        {
            (RegistryHive.LocalMachine, RegistryView.Registry64),
            (RegistryHive.LocalMachine, RegistryView.Registry32),
            (RegistryHive.CurrentUser, RegistryView.Registry64),
            (RegistryHive.CurrentUser, RegistryView.Registry32)
        };

        foreach (var (hive, view) in roots)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var uninstallKey = baseKey.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    writable: false);
                if (uninstallKey is null)
                    continue;

                foreach (var subKeyName in uninstallKey.GetSubKeyNames())
                {
                    using var appKey = uninstallKey.OpenSubKey(subKeyName, writable: false);
                    var displayName = appKey?.GetValue("DisplayName") as string;
                    if (string.IsNullOrWhiteSpace(displayName) ||
                        !displayName.Contains("NetSupport Manager", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var installLocation = appKey.GetValue("InstallLocation") as string;
                    if (!string.IsNullOrWhiteSpace(installLocation))
                    {
                        result.Add((
                            Path.Combine(installLocation.Trim().Trim('"'), "PCICTLUI.EXE"),
                            $"Registry {hive}/{view}"));
                    }

                    var displayIcon = appKey.GetValue("DisplayIcon") as string;
                    var iconPath = NormalizeDisplayIcon(displayIcon);
                    if (!string.IsNullOrWhiteSpace(iconPath) &&
                        string.Equals(Path.GetFileName(iconPath), "PCICTLUI.EXE", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add((iconPath, $"Registry {hive}/{view} (DisplayIcon)"));
                    }
                }
            }
            catch
            {
                // Registry discovery is best effort. Known file-system paths remain available.
            }
        }

        return result;
    }

    private static string? NormalizeDisplayIcon(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = Environment.ExpandEnvironmentVariables(value.Trim().Trim('"'));
        var comma = trimmed.LastIndexOf(',');
        if (comma > 2 && int.TryParse(trimmed[(comma + 1)..], out _))
            trimmed = trimmed[..comma].Trim().Trim('"');

        return trimmed;
    }

    private static void AddCandidate(IDictionary<string, string> candidates, string? path, string source)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        var normalized = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
        if (!candidates.ContainsKey(normalized))
            candidates[normalized] = source;
    }

    private static DateTimeOffset? SafeGetLastWriteTime(string path)
    {
        try
        {
            return File.GetLastWriteTimeUtc(path);
        }
        catch
        {
            return null;
        }
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
