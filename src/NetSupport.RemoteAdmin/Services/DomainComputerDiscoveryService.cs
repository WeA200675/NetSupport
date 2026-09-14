using System.Diagnostics;
using System.Text.Json;
using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Services;

public sealed class DomainComputerDiscoveryService : ITargetDiscoveryService
{
    public string DisplayName => "Active Directory";

    public async Task<IReadOnlyList<RemoteTarget>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var script = "Get-ADComputer -Filter * -Properties Description | Select-Object Name,DNSHostName,Description | ConvertTo-Json -Compress";
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{script}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("PowerShell konnte nicht gestartet werden.");

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "Active-Directory-Abfrage fehlgeschlagen. Auf dem Admin-PC muss das PowerShell-Modul 'ActiveDirectory' (RSAT) verfügbar sein.\n\n" + error.Trim());
        }

        if (string.IsNullOrWhiteSpace(output))
            return Array.Empty<RemoteTarget>();

        using var document = JsonDocument.Parse(output);
        var elements = document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement.EnumerateArray().ToArray()
            : new[] { document.RootElement.Clone() };

        return elements
            .Select(item =>
            {
                var name = GetString(item, "Name");
                var dnsHostName = GetString(item, "DNSHostName");
                return new RemoteTarget
                {
                    Name = name,
                    Host = string.IsNullOrWhiteSpace(dnsHostName) ? name : dnsHostName,
                    Description = GetString(item, "Description")
                };
            })
            .Where(target => !string.IsNullOrWhiteSpace(target.Host))
            .OrderBy(target => target.Name)
            .ToList();
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
            return string.Empty;

        return property.GetString() ?? string.Empty;
    }
}
