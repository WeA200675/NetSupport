using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Providers;

public enum RemoteAction
{
    Control,
    View,
    Chat,
    Inventory,
    CommandPrompt,
    FileTransfer
}

public interface IRemoteProvider
{
    string Id { get; }
    string DisplayName { get; }
    IReadOnlyCollection<RemoteAction> SupportedActions { get; }
    bool IsAvailable { get; }
    Task ConnectAsync(RemoteTarget target, RemoteAction action, CancellationToken cancellationToken = default);
}
