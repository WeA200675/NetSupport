using System.Diagnostics;
using System.IO;
using System.Net;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Services;

namespace NetSupport.RemoteAdmin.Providers;

public sealed class NetSupportProvider(AppConfig config) : IRemoteProvider
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
            throw new InvalidOperationException("NetSupport Manager (PCICTLUI.EXE) wurde nicht gefunden. Bitte den Pfad in settings.json setzen.");

        if (string.IsNullOrWhiteSpace(target.Host))
            throw new ArgumentException("Für die Verbindung ist ein Rechnername oder eine IP-Adresse erforderlich.", nameof(target));

        if (!SupportedActions.Contains(action))
            throw new NotSupportedException($"Die Aktion '{action}' wird von NetSupport nicht unterstützt.");

        var psi = new ProcessStartInfo
        {
            FileName = config.NetSupportExecutable!,
            UseShellExecute = true
        };

        var connectTarget = IPAddress.TryParse(target.Host, out _) ? $">{target.Host}" : target.Host;
        psi.ArgumentList.Add($"/c\"{connectTarget}\"");

        foreach (var argument in GetActionArguments(action))
            psi.ArgumentList.Add(argument);

        _ = Process.Start(psi) ?? throw new InvalidOperationException("NetSupport konnte nicht gestartet werden.");
        return Task.CompletedTask;
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
}
