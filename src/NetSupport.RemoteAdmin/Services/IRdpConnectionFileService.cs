using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Services;

public interface IRdpConnectionFileService
{
    string? NormalizeMonitorIds(string? value);

    string CreateConnectionFile(RemoteTarget target, bool fullScreen);

    void ShowLocalMonitorIds();
}
