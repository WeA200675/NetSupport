using System.Diagnostics;
using System.IO;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Services;

namespace NetSupport.RemoteAdmin.Providers;

public sealed class RdpProvider(AppConfig config) : IRemoteProvider
{
    public string Id => "rdp";
    public string DisplayName => "Windows Remote Desktop";
    public IReadOnlyCollection<RemoteAction> SupportedActions { get; } = new[] { RemoteAction.Control };
    public bool IsAvailable => File.Exists(Path.Combine(Environment.SystemDirectory, "mstsc.exe"));

    public Task ConnectAsync(RemoteTarget target, RemoteAction action, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (action != RemoteAction.Control)
            throw new NotSupportedException("RDP unterstützt in diesem Frontend nur die Aktion 'Control'.");

        if (string.IsNullOrWhiteSpace(target.Host))
            throw new ArgumentException("Für die Verbindung ist ein Rechnername oder eine IP-Adresse erforderlich.", nameof(target));

        var psi = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "mstsc.exe"),
            UseShellExecute = true
        };

        psi.ArgumentList.Add($"/v:{target.Host}");
        if (config.UseFullScreenRdp)
            psi.ArgumentList.Add("/f");

        _ = Process.Start(psi) ?? throw new InvalidOperationException("Remote Desktop konnte nicht gestartet werden.");
        return Task.CompletedTask;
    }
}
