using System.IO;
using NetSupport.RemoteAdmin.Providers;

namespace NetSupport.RemoteAdmin.Services;

internal static class ConfigNormalizer
{
    internal const int CurrentSchemaVersion = 1;
    internal const string ApprovedProviderId = "netsupport";

    internal static AppConfig Normalize(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config.SchemaVersion > CurrentSchemaVersion)
        {
            throw new InvalidDataException(
                $"Die Konfiguration verwendet Schema-Version {config.SchemaVersion}, diese Anwendung unterstützt jedoch maximal Version {CurrentSchemaVersion}. " +
                "Bitte eine gleich neue oder neuere Programmversion verwenden.");
        }

        // Schema 0 represents all unversioned/legacy settings files. Current migrations are
        // intentionally idempotent, so normalizing an already-current file is safe as well.
        config.Targets ??= [];
        config.SavedViews ??= [];

        config.NetSupportExecutable = NullIfWhiteSpace(config.NetSupportExecutable);
        config.NetSupportProfileName = NullIfWhiteSpace(config.NetSupportProfileName);

        foreach (var target in config.Targets)
        {
            if (target is null)
                continue;

            // Domain policy is NetSupport-only. Legacy values such as "rdp" or any
            // manually injected provider id are normalized before UI/provider logic sees them.
            target.PreferredProviderId = ApprovedProviderId;
            target.PreferredAction = NormalizePreferredAction(target.PreferredAction);

            target.Name = target.Name?.Trim() ?? string.Empty;
            target.Host = target.Host?.Trim() ?? string.Empty;
            target.Description = NullIfWhiteSpace(target.Description);
            target.Group = NullIfWhiteSpace(target.Group);
        }

        config.SchemaVersion = CurrentSchemaVersion;
        return config;
    }

    internal static string NormalizePreferredAction(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            Enum.TryParse<RemoteAction>(value.Trim(), ignoreCase: true, out var parsed) &&
            IsApprovedAction(parsed))
        {
            return parsed.ToString();
        }

        return RemoteAction.Control.ToString();
    }

    private static bool IsApprovedAction(RemoteAction action) => action is
        RemoteAction.Control or
        RemoteAction.View or
        RemoteAction.Chat or
        RemoteAction.Inventory or
        RemoteAction.CommandPrompt or
        RemoteAction.FileTransfer;

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
