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

    public ConfigService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NetSupportRemoteAdmin"))
    {
    }

    internal ConfigService(string configDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configDirectory);
        ConfigDirectory = Path.GetFullPath(configDirectory);
    }

    public string ConfigDirectory { get; }
    public string ConfigPath => Path.Combine(ConfigDirectory, "settings.json");
    internal string BackupPath => Path.Combine(ConfigDirectory, "settings.json.bak");

    public async Task<AppConfig> LoadAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(ConfigDirectory);

        if (!File.Exists(ConfigPath))
        {
            if (File.Exists(BackupPath))
            {
                var recovered = await TryLoadAsync(BackupPath, cancellationToken);
                if (recovered is not null)
                {
                    await WriteNormalizedFilesAsync(recovered, cancellationToken);
                    return recovered;
                }
            }

            var defaults = ConfigNormalizer.Normalize(CreateDefault());
            await SaveAsync(defaults, cancellationToken);
            return defaults;
        }

        var primary = await TryLoadAsync(ConfigPath, cancellationToken);
        if (primary is not null)
            return primary;

        var backup = File.Exists(BackupPath)
            ? await TryLoadAsync(BackupPath, cancellationToken)
            : null;

        if (backup is null)
        {
            throw new InvalidDataException(
                $"Die Konfiguration '{ConfigPath}' ist beschädigt und es steht kein lesbares Backup zur Verfügung.");
        }

        // Repair the primary file from the last known-good, already normalized backup.
        await WriteNormalizedFilesAsync(backup, cancellationToken);
        return backup;
    }

    public async Task SaveAsync(AppConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        Directory.CreateDirectory(ConfigDirectory);
        await WriteNormalizedFilesAsync(config, cancellationToken);
    }

    private async Task<AppConfig?> TryLoadAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            var loaded = await JsonSerializer.DeserializeAsync<AppConfig>(stream, JsonOptions, cancellationToken);
            return loaded is null ? null : ConfigNormalizer.Normalize(loaded);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private async Task WriteNormalizedFilesAsync(AppConfig config, CancellationToken cancellationToken)
    {
        ConfigNormalizer.Normalize(config);
        var payload = JsonSerializer.SerializeToUtf8Bytes(config, JsonOptions);

        // Replace the primary first. Because the temporary file lives in the same directory,
        // File.Move(..., overwrite: true) uses a same-volume replacement instead of exposing
        // a partially written settings.json. The backup is written from the normalized payload,
        // so old/unknown legacy RDP fields are not preserved in settings.json.bak either.
        await WriteAtomicallyAsync(ConfigPath, payload, cancellationToken);
        await WriteAtomicallyAsync(BackupPath, payload, cancellationToken);
    }

    private static async Task WriteAtomicallyAsync(
        string destinationPath,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(destinationPath)
                        ?? throw new InvalidOperationException("Konfigurationsordner konnte nicht ermittelt werden.");
        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(
            directory,
            $".{Path.GetFileName(destinationPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                             tempPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 4096,
                             options: FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(payload, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(tempPath, destinationPath, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
                // Best effort cleanup; the destination was never written from a partial temp file.
            }
        }
    }

    private static AppConfig CreateDefault() => new()
    {
        NetSupportExecutable = new NetSupportInstallationService().FindBestExecutable()
    };
}
