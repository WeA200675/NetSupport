namespace NetSupport.RemoteAdmin.Providers;

public sealed class RemoteProviderRegistry(IEnumerable<IRemoteProvider> providers)
{
    private readonly IReadOnlyList<IRemoteProvider> _providers = providers.ToList();

    public IReadOnlyList<IRemoteProvider> All => _providers;

    public IRemoteProvider Get(string id) =>
        _providers.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Remote-Provider '{id}' wurde nicht gefunden.");
}
