namespace NetSupport.RemoteAdmin.Services;

public interface IAutoStartService
{
    bool IsEnabled { get; }
    void SetEnabled(bool enabled);
}
