using System.IO;
using System.Text;
using System.Text.Json;
using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Services;

/// <summary>
/// Keeps a small local history of remote-session launches separate from settings.json.
/// The file is capped to avoid unbounded growth and is written atomically where possible.
/// </summary>
public sealed class JsonSessionHistoryService : ISessionHistoryService
{
    private const int MaxEntries = 100;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _configDirectory;

    public JsonSessionHistoryService(string configDirectory)
    {
        _configDirectory = configDirectory;
    }

    public string HistoryPath => Path.Combine(_configDirectory, "session-history.json");

    public async Task<IReadOnlyList<SessionHistoryEntry>> GetRecentAsync(
        int count = 20,
        CancellationToken cancellationToken = default)
    {
        if (count <= 0)
            return [];

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var entries = await LoadUnsafeAsync(cancellationToken);
            return entries
                .OrderByDescending(entry => entry.StartedAt)
                .Take(count)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RecordAsync(
        SessionHistoryEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var entries = await LoadUnsafeAsync(cancellationToken);
            entries.Insert(0, entry);

            if (entries.Count > MaxEntries)
                entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);

            await SaveUnsafeAsync(entries, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ExportCsvAsync(
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var entries = (await LoadUnsafeAsync(cancellationToken))
                .OrderByDescending(entry => entry.StartedAt)
                .ToList();

            var builder = new StringBuilder();
            builder.AppendLine("Zeitpunkt;Rechner;Name;Provider;Provider-ID;Aktion;Erfolg;Fehler");

            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                builder.Append(Csv(entry.StartedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"))).Append(';')
                    .Append(Csv(entry.Host)).Append(';')
                    .Append(Csv(entry.TargetName)).Append(';')
                    .Append(Csv(entry.ProviderName)).Append(';')
                    .Append(Csv(entry.ProviderId)).Append(';')
                    .Append(Csv(entry.Action)).Append(';')
                    .Append(Csv(entry.Succeeded ? "Ja" : "Nein")).Append(';')
                    .Append(Csv(entry.Error))
                    .AppendLine();
            }

            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            // UTF-8 with BOM makes the semicolon-delimited German CSV open reliably in Excel.
            await File.WriteAllTextAsync(
                destinationPath,
                builder.ToString(),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
                cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(HistoryPath))
                File.Delete(HistoryPath);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<SessionHistoryEntry>> LoadUnsafeAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_configDirectory);

        if (!File.Exists(HistoryPath))
            return [];

        try
        {
            await using var stream = File.OpenRead(HistoryPath);
            return await JsonSerializer.DeserializeAsync<List<SessionHistoryEntry>>(
                       stream,
                       JsonOptions,
                       cancellationToken)
                   ?? [];
        }
        catch (JsonException)
        {
            // A damaged history file must never prevent the admin tool from starting.
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    private async Task SaveUnsafeAsync(
        List<SessionHistoryEntry> entries,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_configDirectory);

        var tempPath = HistoryPath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, entries, JsonOptions, cancellationToken);
        }

        File.Move(tempPath, HistoryPath, overwrite: true);
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var normalized = value.Replace("\r", " ").Replace("\n", " ");
        if (!normalized.Contains(';') && !normalized.Contains('"'))
            return normalized;

        return $"\"{normalized.Replace("\"", "\"\"")}\"";
    }
}
