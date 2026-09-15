using System.IO;
using System.Text.Json;

namespace NetSupport.RemoteAdmin.Services;

public sealed class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string ConfigDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NetSupportRemoteAdmin");

    public string ConfigPath => Path.Combine(ConfigDirectory, "settings.json");

    public async Task<AppConfig> LoadAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(ConfigDirectory);

        if (!File.Exists(ConfigPath))
        {
            var defaults = ConfigNormalizer.Normalize(CreateDefault());
            await SaveAsync(defaults, cancellationToken);
            return defaults;
        }

        await using var stream = File.OpenRead(ConfigPath);
        var loaded = await JsonSerializer.DeserializeAsync<AppConfig>(stream, JsonOptions, cancellationToken)
                     ?? CreateDefault();

        return ConfigNormalizer.Normalize(loaded);
    }

    public async Task SaveAsync(AppConfig config, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(ConfigDirectory);
        ConfigNormalizer.Normalize(config);

        await using var stream = File.Create(ConfigPath);
        await JsonSerializer.SerializeAsync(stream, config, JsonOptions, cancellationToken);
    }

    private static AppConfig CreateDefault() => new()
    {
        NetSupportExecutable = new NetSupportInstallationService().FindBestExecutable()
    };
}
