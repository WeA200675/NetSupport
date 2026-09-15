using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Services;

public sealed class AppConfig
{
    /// <summary>
    /// Version of the persisted settings schema. Unversioned legacy files deserialize as 0
    /// and are upgraded by ConfigNormalizer before the application uses or saves them.
    /// </summary>
    public int SchemaVersion { get; set; }

    public string? NetSupportExecutable { get; set; }
    public string? NetSupportProfileName { get; set; }
    public bool NetSupportLockProfile { get; set; }

    /// <summary>
    /// TCP port used only for the lightweight NetSupport Client reachability check.
    /// NetSupport Manager uses 5405 by default, but the Client port is configurable.
    /// </summary>
    public int NetSupportClientPort { get; set; } = NetSupportReachabilityService.DefaultClientPort;

    public bool StartMinimized { get; set; }
    public bool DiagnosticLoggingEnabled { get; set; } = true;
    public List<RemoteTarget> Targets { get; set; } = [];
    public List<SavedTargetView> SavedViews { get; set; } = [];
}
