namespace NetSupport.RemoteAdmin.Models;

public sealed class RdpDisconnectedEventArgs(
    int reason,
    int extendedReason,
    string? description) : EventArgs
{
    public int Reason { get; } = reason;
    public int ExtendedReason { get; } = extendedReason;
    public string? Description { get; } = description;
}

public sealed class RdpFatalErrorEventArgs(int errorCode) : EventArgs
{
    public int ErrorCode { get; } = errorCode;
}

public sealed class RdpDesktopSizeChangedEventArgs(int width, int height) : EventArgs
{
    public int Width { get; } = width;
    public int Height { get; } = height;
}
