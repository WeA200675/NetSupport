namespace NetSupport.RemoteAdmin.Services;

public interface ISupportBundleService
{
    Task CreateAsync(
        string destinationPath,
        bool anonymizeIdentifiers = true,
        CancellationToken cancellationToken = default);
}
