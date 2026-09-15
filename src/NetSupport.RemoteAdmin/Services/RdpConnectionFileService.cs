using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Services;

/// <summary>
/// Creates minimal credential-free .rdp files for the external Windows client.
/// The file carries the same non-secret redirection and monitor preferences that
/// the embedded ActiveX client applies directly.
/// </summary>
public sealed class RdpConnectionFileService : IRdpConnectionFileService
{
    private readonly string _directory;

    public RdpConnectionFileService(string configDirectory)
    {
        _directory = Path.Combine(configDirectory, "rdp");
    }

    public string? NormalizeMonitorIds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var result = new List<int>();
        var seen = new HashSet<int>();

        foreach (var part in value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id < 0)
                throw new ArgumentException("RDP-Monitor-IDs müssen nichtnegative Ganzzahlen sein, z. B. 0,1.");

            if (seen.Add(id))
                result.Add(id);
        }

        if (result.Count == 0)
            return null;

        return string.Join(',', result);
    }

    public string CreateConnectionFile(RemoteTarget target, bool fullScreen)
    {
        ArgumentNullException.ThrowIfNull(target);

        var host = RequireSingleLine(target.Host, "Zielrechner");
        var monitorIds = NormalizeMonitorIds(target.RdpSelectedMonitors);
        var useMultiMonitor = monitorIds is not null || target.RdpUseMultiMonitor;
        var audioMode = NormalizeAudioMode(target.RdpAudioRedirectionMode);

        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, $"connection-{StableId(host)}.rdp");

        var lines = new List<string>
        {
            $"full address:s:{host}",
            $"screen mode id:i:{(fullScreen ? 2 : 1)}",
            $"use multimon:i:{(useMultiMonitor ? 1 : 0)}",
            $"redirectclipboard:i:{(target.RdpRedirectClipboard ? 1 : 0)}",
            $"audiomode:i:{audioMode}",
            $"audiocapturemode:i:{(target.RdpRedirectMicrophone ? 1 : 0)}",
            target.RdpRedirectDrives ? "drivestoredirect:s:*" : "drivestoredirect:s:"
        };

        if (monitorIds is not null)
            lines.Add($"selectedmonitors:s:{monitorIds}");

        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return path;
    }

    public void ShowLocalMonitorIds()
    {
        var mstscPath = GetMstscPath();
        var psi = new ProcessStartInfo
        {
            FileName = mstscPath,
            UseShellExecute = true
        };
        psi.ArgumentList.Add("/l");

        _ = Process.Start(psi)
            ?? throw new InvalidOperationException("Die lokale RDP-Monitorliste konnte nicht geöffnet werden.");
    }

    private static string GetMstscPath()
    {
        var mstscPath = Path.Combine(Environment.SystemDirectory, "mstsc.exe");
        if (!File.Exists(mstscPath))
            throw new InvalidOperationException("Windows Remote Desktop (mstsc.exe) wurde nicht gefunden.");

        return mstscPath;
    }

    private static string RequireSingleLine(string value, string displayName)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException($"{displayName} darf nicht leer sein.");
        if (trimmed.Contains('\r') || trimmed.Contains('\n'))
            throw new ArgumentException($"{displayName} enthält ungültige Zeilenumbrüche.");

        return trimmed;
    }

    private static int NormalizeAudioMode(int value) => value is >= 0 and <= 2 ? value : 0;

    private static string StableId(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value.ToUpperInvariant()));
        return Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
    }
}
