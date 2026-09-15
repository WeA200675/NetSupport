using System.Diagnostics;
using System.Text.Json;
using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Services;

public sealed class DomainComputerDiscoveryService : ITargetDiscoveryService
{
    public string DisplayName => "Active Directory (RSAT/LDAP)";

    private const string DiscoveryScript = """
        $ErrorActionPreference = 'Stop'

        $items = if ($null -ne (Get-Command Get-ADComputer -ErrorAction SilentlyContinue)) {
            Get-ADComputer -Filter * -Properties Description |
                Select-Object Name, DNSHostName, Description
        }
        else {
            $rootDse = [ADSI]'LDAP://RootDSE'
            $defaultNamingContext = [string]$rootDse.defaultNamingContext
            if ([string]::IsNullOrWhiteSpace($defaultNamingContext)) {
                throw 'Die aktuelle Windows-Sitzung konnte keinen Active-Directory-Namenskontext ermitteln.'
            }

            $searchRoot = [ADSI]('LDAP://' + $defaultNamingContext)
            $searcher = New-Object System.DirectoryServices.DirectorySearcher($searchRoot)
            $searcher.Filter = '(&(objectCategory=computer)(objectClass=computer))'
            $searcher.PageSize = 1000
            [void]$searcher.PropertiesToLoad.Add('name')
            [void]$searcher.PropertiesToLoad.Add('dnshostname')
            [void]$searcher.PropertiesToLoad.Add('description')

            $results = $null
            try {
                $results = $searcher.FindAll()
                foreach ($entry in $results) {
                    [pscustomobject]@{
                        Name = if ($entry.Properties['name'].Count -gt 0) { [string]$entry.Properties['name'][0] } else { '' }
                        DNSHostName = if ($entry.Properties['dnshostname'].Count -gt 0) { [string]$entry.Properties['dnshostname'][0] } else { '' }
                        Description = if ($entry.Properties['description'].Count -gt 0) { [string]$entry.Properties['description'][0] } else { '' }
                    }
                }
            }
            finally {
                if ($null -ne $results) { $results.Dispose() }
                $searcher.Dispose()
            }
        }

        $items | ConvertTo-Json -Compress
        """;

    public async Task<IReadOnlyList<RemoteTarget>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var powershell = Path.Combine(
            Environment.SystemDirectory,
            "WindowsPowerShell",
            "v1.0",
            "powershell.exe");
        if (!File.Exists(powershell))
            throw new InvalidOperationException("Windows PowerShell wurde nicht gefunden.");

        var startInfo = new ProcessStartInfo
        {
            FileName = powershell,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(DiscoveryScript);

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
            throw new InvalidOperationException("PowerShell konnte nicht gestartet werden.");

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "Active-Directory-Abfrage fehlgeschlagen. Weder die bevorzugte RSAT-Abfrage noch der LDAP-Fallback konnte die Rechnerliste lesen.\n\n" +
                error.Trim());
        }

        return ParseTargets(output);
    }

    internal static IReadOnlyList<RemoteTarget> ParseTargets(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return Array.Empty<RemoteTarget>();

        using var document = JsonDocument.Parse(output);
        var elements = document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement.EnumerateArray().ToArray()
            : new[] { document.RootElement.Clone() };

        return elements
            .Where(item => item.ValueKind == JsonValueKind.Object)
            .Select(item =>
            {
                var name = GetString(item, "Name").Trim();
                var dnsHostName = GetString(item, "DNSHostName").Trim();
                return new RemoteTarget
                {
                    Name = name,
                    Host = string.IsNullOrWhiteSpace(dnsHostName) ? name : dnsHostName,
                    Description = NullIfWhiteSpace(GetString(item, "Description"))
                };
            })
            .Where(target => !string.IsNullOrWhiteSpace(target.Host))
            .GroupBy(target => target.Host, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(target => string.IsNullOrWhiteSpace(target.Name) ? target.Host : target.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
            return string.Empty;

        return property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : property.ToString();
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
