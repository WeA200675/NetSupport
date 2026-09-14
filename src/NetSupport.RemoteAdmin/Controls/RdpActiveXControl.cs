using System.Windows.Forms;

namespace NetSupport.RemoteAdmin.Controls;

/// <summary>
/// Thin Windows Forms host around Microsoft's nonscriptable Remote Desktop ActiveX control.
/// The COM object is accessed dynamically so the project does not depend on generated
/// AxInterop/MSTSCLib assemblies.
/// </summary>
public sealed class RdpActiveXControl : AxHost
{
    // CLSID_MsRdpClient12NotSafeForScripting (Remote Desktop Client Control v13).
    private const string ClassId = "3F859AA3-C2D4-4FAA-B0E4-FD0C9C4E5E3A";

    public RdpActiveXControl() : base(ClassId)
    {
        Dock = DockStyle.Fill;
    }

    public bool IsConnected
    {
        get
        {
            try
            {
                dynamic client = GetClient();
                return client.Connected != 0;
            }
            catch
            {
                return false;
            }
        }
    }

    public void ConnectTo(string host, int desktopWidth, int desktopHeight)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Ein Zielrechner ist erforderlich.", nameof(host));

        dynamic client = GetClient();
        client.Server = host;
        client.DesktopWidth = Math.Max(640, desktopWidth);
        client.DesktopHeight = Math.Max(480, desktopHeight);
        client.Connect();
    }

    public void DisconnectSession()
    {
        try
        {
            dynamic client = GetClient();
            if (client.Connected != 0)
                client.Disconnect();
        }
        catch
        {
            // Closing the containing window must stay reliable even if the COM control
            // is already tearing down or was never fully initialized.
        }
    }

    private object GetClient()
    {
        if (!IsHandleCreated)
            CreateControl();

        return GetOcx() ?? throw new InvalidOperationException(
            "Das Microsoft Remote Desktop ActiveX-Control konnte nicht initialisiert werden.");
    }
}
