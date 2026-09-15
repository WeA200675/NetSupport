using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Services;

namespace NetSupport.RemoteAdmin.Providers;

internal static partial class NetSupportCommandLine
{
    internal static string BuildArguments(
        string host,
        RemoteAction action,
        string? profileName,
        bool lockProfile)
    {
        var profileArguments = BuildProfileArguments(profileName, lockProfile);
        var connectArgument = BuildConnectArgument(host);
        var actionArguments = GetActionArguments(action);

        return string.Join(
            ' ',
            new[] { profileArguments, connectArgument, actionArguments }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    internal static string BuildConnectArgument(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Für die Verbindung ist ein Rechnername oder eine IP-Adresse erforderlich.", nameof(host));

        var trimmed = host.Trim();
        if (trimmed.Length > 253 || trimmed.Contains('"') || trimmed.Contains('\r') || trimmed.Contains('\n'))
            throw new ArgumentException("Der Rechnername enthält ungültige Zeichen.", nameof(host));

        if (IPAddress.TryParse(trimmed, out var address))
            return $"/c\">{address}\"";

        if (!DomainHostNameRegex().IsMatch(trimmed))
        {
            throw new ArgumentException(
                "Der Rechnername ist ungültig. Erlaubt sind Buchstaben, Ziffern, Punkt, Bindestrich und Unterstrich.",
                nameof(host));
        }

        return $"/c {trimmed}";
    }

    internal static string BuildProfileArguments(string? profileName, bool lockProfile)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            if (lockProfile)
            {
                throw new InvalidOperationException(
                    "NetSupport-Profilbindung (/F) ist aktiviert, aber es wurde kein Control-Profil ausgewählt.");
            }

            return string.Empty;
        }

        var profile = NetSupportProfileService.NormalizeProfileName(profileName);
        return lockProfile
            ? $"/f /n \"{profile}\""
            : $"/n \"{profile}\"";
    }

    internal static string GetActionArguments(RemoteAction action) => action switch
    {
        RemoteAction.Control => "/vc /e",
        RemoteAction.View => "/v /e",
        RemoteAction.Chat => "/a /ea",
        RemoteAction.Inventory => "/i /ei",
        RemoteAction.CommandPrompt => "/m /em",
        RemoteAction.FileTransfer => "/x /ex",
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
    };

    internal static string ValidateExecutablePath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new InvalidOperationException(
                "NetSupport Manager (PCICTLUI.EXE) ist nicht konfiguriert. Bitte den Pfad unter Erweitert → Einstellungen prüfen.");
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(configuredPath.Trim().Trim('"')));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Der konfigurierte NetSupport-Pfad ist ungültig.", ex);
        }

        if (!string.Equals(Path.GetFileName(fullPath), "PCICTLUI.EXE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Der konfigurierte NetSupport-Pfad muss auf PCICTLUI.EXE zeigen. Andere Programme werden nicht über den Remote-Provider gestartet.");
        }

        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException(
                $"NetSupport Manager wurde am konfigurierten Pfad nicht gefunden: {fullPath}");
        }

        return fullPath;
    }

    [GeneratedRegex(@"^[A-Za-z0-9](?:[A-Za-z0-9._-]{0,251}[A-Za-z0-9_])?$", RegexOptions.CultureInvariant)]
    private static partial Regex DomainHostNameRegex();
}
