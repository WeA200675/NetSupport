using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Services;

/// <summary>
/// Collects lightweight target information without adding a WMI/CIM NuGet dependency.
/// DNS resolution is local. OS, model and interactive user information are queried through
/// Get-CimInstance using the current Windows identity and the target's normal WSMan policy.
/// Failure of the management query is non-fatal so DNS/status information remains useful.
/// </summary>
public sealed class PowerShellTargetDetailsService : ITargetDetailsService
{
    public string DisplayName => "Windows CIM";

    public async Task<RemoteTargetDetails> GetDetailsAsync(
        RemoteTarget target,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(target.Host))
            throw new ArgumentException("Ein Zielrechner ist erforderlich.", nameof(target));

        var ipAddresses = await ResolveAddressesAsync(target.Host, cancellationToken);

        string? operatingSystem = null;
        string? operatingSystemVersion = null;
        string? loggedOnUser = null;
        string? manufacturer = null;
        string? model = null;
        string? managementError = null;

        try
        {
            var result = await QueryCimAsync(target.Host, cancellationToken);
            operatingSystem = result.OperatingSystem;
            operatingSystemVersion = result.OperatingSystemVersion;
            loggedOnUser = result.LoggedOnUser;
            manufacturer = result.Manufacturer;
            model = result.Model;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            managementError = ex.Message;
        }

        return new RemoteTargetDetails
        {
            IpAddresses = ipAddresses,
            OperatingSystem = operatingSystem,
            OperatingSystemVersion = operatingSystemVersion,
            LoggedOnUser = loggedOnUser,
            Manufacturer = manufacturer,
            Model = model,
            CheckedAt = DateTimeOffset.Now,
            ManagementError = managementError
        };
    }

    private static async Task<string?> ResolveAddressesAsync(
        string host,
        CancellationToken cancellationToken)
    {
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host).WaitAsync(cancellationToken);
            var ordered = addresses
                .OrderBy(address => address.AddressFamily == AddressFamily.InterNetwork ? 0 : 1)
                .Select(address => address.ToString())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(4)
                .ToArray();

            return ordered.Length == 0 ? null : string.Join(", ", ordered);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<CimTargetResult> QueryCimAsync(
        string host,
        CancellationToken cancellationToken)
    {
        var powerShellPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell",
            "v1.0",
            "powershell.exe");

        if (!File.Exists(powerShellPath))
            powerShellPath = "powershell.exe";

        const string script = "$ErrorActionPreference='Stop';" +
                              "$computer=$env:NSRA_TARGET;" +
                              "$cs=Get-CimInstance -ClassName Win32_ComputerSystem -ComputerName $computer -OperationTimeoutSec 6 -ErrorAction Stop;" +
                              "$os=Get-CimInstance -ClassName Win32_OperatingSystem -ComputerName $computer -OperationTimeoutSec 6 -ErrorAction Stop;" +
                              "[pscustomobject]@{" +
                              "LoggedOnUser=$cs.UserName;" +
                              "Manufacturer=$cs.Manufacturer;" +
                              "Model=$cs.Model;" +
                              "OperatingSystem=$os.Caption;" +
                              "OperatingSystemVersion=$os.Version" +
                              "}|ConvertTo-Json -Compress";

        var psi = new ProcessStartInfo
        {
            FileName = powerShellPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        psi.ArgumentList.Add("-NoLogo");
        psi.ArgumentList.Add("-NoProfile");
        psi.ArgumentList.Add("-NonInteractive");
        psi.ArgumentList.Add("-ExecutionPolicy");
        psi.ArgumentList.Add("Bypass");
        psi.ArgumentList.Add("-Command");
        psi.ArgumentList.Add(script);
        psi.Environment["NSRA_TARGET"] = host;

        using var process = Process.Start(psi)
                            ?? throw new InvalidOperationException("PowerShell konnte nicht gestartet werden.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Best effort only; cancellation should still propagate.
                }
            }

            throw;
        }

        var stdout = (await stdoutTask).Trim();
        var stderr = (await stderrTask).Trim();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(stderr)
                    ? $"CIM-Abfrage wurde mit Exitcode {process.ExitCode} beendet."
                    : FirstLine(stderr));
        }

        if (string.IsNullOrWhiteSpace(stdout))
            throw new InvalidOperationException("Die CIM-Abfrage hat keine Daten geliefert.");

        using var json = JsonDocument.Parse(stdout);
        var root = json.RootElement;

        return new CimTargetResult(
            GetString(root, "OperatingSystem"),
            GetString(root, "OperatingSystemVersion"),
            GetString(root, "LoggedOnUser"),
            GetString(root, "Manufacturer"),
            GetString(root, "Model"));
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;

        var text = value.GetString();
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static string FirstLine(string value)
    {
        using var reader = new StringReader(value);
        return reader.ReadLine()?.Trim() ?? value;
    }

    private sealed record CimTargetResult(
        string? OperatingSystem,
        string? OperatingSystemVersion,
        string? LoggedOnUser,
        string? Manufacturer,
        string? Model);
}
