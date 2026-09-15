using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Services;

public sealed class AppConfig
{
    public string? NetSupportExecutable { get; set; }
    public bool StartMinimized { get; set; }
    public bool DiagnosticLoggingEnabled { get; set; } = true;
    public List<RemoteTarget> Targets { get; set; } = [];
    public List<SavedTargetView> SavedViews { get; set; } = [];
}
