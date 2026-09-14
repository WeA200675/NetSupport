namespace NetSupport.RemoteAdmin.Models;

/// <summary>
/// Values from Microsoft's RemoteSessionActionType enumeration used by
/// IMsRdpClient8.SendRemoteAction.
/// </summary>
public enum RdpRemoteAction
{
    StartScreen = 3,
    AppSwitch = 4,
    ActionCenter = 5,
    TaskManager = 6
}
