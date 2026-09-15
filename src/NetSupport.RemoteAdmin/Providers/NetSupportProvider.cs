using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Services;

namespace NetSupport.RemoteAdmin.Providers;

public sealed partial class NetSupportProvider(
    AppConfig config,
    IDiagnosticLogService diagnosticLog) : IRemoteProvider
{
    public string Id => "netsupport";
    public string DisplayName => "NetSupport Manager";

    public IReadOnlyCollection<RemoteAction> SupportedActions { get; } = new[]
    {
        RemoteAction.Control,
        RemoteAction.View,
        RemoteAction.Chat,
        RemoteAction.Inventory,
        RemoteAction.CommandPrompt,
        RemoteAction.FileTransfer
    };

    public bool IsAvailable => !string.IsNullOrWhiteSpace(config.NetSupportExecutable)
                               && File.Exists(config.NetSupportExecutable);

    public Task ConnectAsync(RemoteTarget target, RemoteAction action, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsAvailable)
        {
            throw new InvalidOperationException(
                "NetSupport Manager (PCICTLUI.EXE) wurde nicht gefunden. Bitte den Pfad unter Erweitert → Einstellungen prüfen.");
        }

        if (!SupportedActions.Contains(action))
            throw new NotSupportedException($"Die Aktion '{action}' wird von NetSupport nicht unterstützt.");

        var connectArgument = BuildConnectArgument(target.Host);
        var actionArguments = string.Join(' ', GetActionArguments(action));
        var arguments = $"{connectArgument} {actionArguments}".Trim();

        // NetSupport documents an unusual compact /c\">address\" syntax for IP connections.
        // ProcessStartInfo.ArgumentList can re-escape embedded quotes, so pass the validated
        // command line directly to PCICTLUI.EXE instead of asking .NET to reconstruct it.
        var psi = new ProcessStartInfo
        {
            FileName = config.NetSupportExecutable!,
            Arguments = arguments,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(config.NetSupportExecutable!)
                               ?? Environment.CurrentDirectory
        };

        diagnosticLog.Info($"NetSupport CLI: {Path.GetFileName(psi.FileName)} {arguments}");
        _ = Process.Start(psi) ?? throw new InvalidOperationException("NetSupport konnte nicht gestartet werden.");
        return Task.CompletedTask;
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

        // The UI is intended for domain computer/DNS names, not arbitrary command-line text.
        // Restrict the value before placing it into NetSupport's raw command line.
        if (!DomainHostNameRegex().IsMatch(trimmed))
        {
            throw new ArgumentException(
                "Der Rechnername ist ungültig. Erlaubt sind Buchstaben, Ziffern, Punkt, Bindestrich und Unterstrich.",
                nameof(host));
        }

        return $"/c {trimmed}";
    }

    private static IEnumerable<string> GetActionArguments(RemoteAction action) => action switch
    {
        RemoteAction.Control => ["/vc", "/e"],
        RemoteAction.View => ["/v", "/e"],
        RemoteAction.Chat => ["/a", "/ea"],
        RemoteAction.Inventory => ["/i", "/ei"],
        RemoteAction.CommandPrompt => ["/m", "/em"],
        RemoteAction.FileTransfer => ["/x", "/ex"],
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
    };

    [GeneratedRegex(@"^[A-Za-z0-9](?:[A-Za-z0-9._-]{0,251}[A-Za-z0-9_])?$", RegexOptions.CultureInvariant)]
    private static partial Regex DomainHostNameRegex();
}
