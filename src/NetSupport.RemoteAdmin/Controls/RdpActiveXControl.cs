using System.Runtime.InteropServices;
using System.Windows.Forms;
using NetSupport.RemoteAdmin.Models;

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

    // DIID_IMsTscAxEvents.
    private static readonly Guid EventInterfaceId = new("336D5562-EFA8-482E-8CB3-C5C0FC7A7DB6");

    private readonly Action _onConnecting;
    private readonly Action _onConnected;
    private readonly Action _onLoginComplete;
    private readonly Action<int> _onDisconnected;
    private readonly Action<int> _onFatalError;
    private readonly Action<int, int> _onRemoteDesktopSizeChanged;
    private bool _eventsAttached;

    public RdpActiveXControl() : base(ClassId)
    {
        Dock = DockStyle.Fill;

        _onConnecting = () => Connecting?.Invoke(this, EventArgs.Empty);
        _onConnected = () => Connected?.Invoke(this, EventArgs.Empty);
        _onLoginComplete = () => LoginCompleted?.Invoke(this, EventArgs.Empty);
        _onDisconnected = OnDisconnected;
        _onFatalError = errorCode => FatalError?.Invoke(this, new RdpFatalErrorEventArgs(errorCode));
        _onRemoteDesktopSizeChanged = (width, height) =>
            RemoteDesktopSizeChanged?.Invoke(this, new RdpDesktopSizeChangedEventArgs(width, height));
    }

    public event EventHandler? Connecting;
    public event EventHandler? Connected;
    public event EventHandler? LoginCompleted;
    public event EventHandler<RdpDisconnectedEventArgs>? Disconnected;
    public event EventHandler<RdpFatalErrorEventArgs>? FatalError;
    public event EventHandler<RdpDesktopSizeChangedEventArgs>? RemoteDesktopSizeChanged;

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

    public void ConnectTo(
        string host,
        int desktopWidth,
        int desktopHeight,
        string? userName = null,
        string? domain = null)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Ein Zielrechner ist erforderlich.", nameof(host));

        dynamic client = GetClient();

        client.Server = host;
        client.DesktopWidth = Math.Max(640, desktopWidth);
        client.DesktopHeight = Math.Max(480, desktopHeight);
        client.ColorDepth = 32;
        client.AllowPromptingForCredentials = true;
        client.AllowCredentialSaving = false;

        if (!string.IsNullOrWhiteSpace(userName))
            client.UserName = userName.Trim();

        if (!string.IsNullOrWhiteSpace(domain))
            client.Domain = domain.Trim();

        ConfigureAdvancedSettings(client);
        client.Connect();
    }

    public void SetSmartSizing(bool enabled)
    {
        try
        {
            dynamic client = GetClient();
            dynamic settings = client.AdvancedSettings9;
            settings.SmartSizing = enabled;
        }
        catch
        {
            // Older/partially registered controls can fail to expose the newest settings
            // interface. The session remains usable without SmartSizing.
        }
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

    protected override void CreateSink()
    {
        base.CreateSink();

        try
        {
            var client = GetClient();
            ComEventsHelper.Combine(client, EventInterfaceId, 1, _onConnecting);
            ComEventsHelper.Combine(client, EventInterfaceId, 2, _onConnected);
            ComEventsHelper.Combine(client, EventInterfaceId, 3, _onLoginComplete);
            ComEventsHelper.Combine(client, EventInterfaceId, 4, _onDisconnected);
            ComEventsHelper.Combine(client, EventInterfaceId, 10, _onFatalError);
            ComEventsHelper.Combine(client, EventInterfaceId, 12, _onRemoteDesktopSizeChanged);
            _eventsAttached = true;
        }
        catch
        {
            _eventsAttached = false;
        }
    }

    protected override void DetachSink()
    {
        if (_eventsAttached)
        {
            try
            {
                var client = GetClient();
                ComEventsHelper.Remove(client, EventInterfaceId, 1, _onConnecting);
                ComEventsHelper.Remove(client, EventInterfaceId, 2, _onConnected);
                ComEventsHelper.Remove(client, EventInterfaceId, 3, _onLoginComplete);
                ComEventsHelper.Remove(client, EventInterfaceId, 4, _onDisconnected);
                ComEventsHelper.Remove(client, EventInterfaceId, 10, _onFatalError);
                ComEventsHelper.Remove(client, EventInterfaceId, 12, _onRemoteDesktopSizeChanged);
            }
            catch
            {
                // The underlying COM object can already be released while the AxHost is
                // disposing. There is nothing useful to recover at this point.
            }

            _eventsAttached = false;
        }

        base.DetachSink();
    }

    private static void ConfigureAdvancedSettings(dynamic client)
    {
        try
        {
            dynamic settings = client.AdvancedSettings9;
            settings.SmartSizing = true;
            settings.EnableCredSspSupport = true;
        }
        catch
        {
            // Keep the base RDP connection available even if an optional advanced setting
            // is unavailable on the local Remote Desktop ActiveX registration.
        }
    }

    private void OnDisconnected(int reason)
    {
        var extendedReason = 0;
        string? description = null;

        try
        {
            dynamic client = GetClient();
            extendedReason = Convert.ToInt32(client.ExtendedDisconnectReason);
            description = Convert.ToString(client.GetErrorDescription(
                Convert.ToUInt32(reason),
                Convert.ToUInt32(extendedReason)));
        }
        catch
        {
            // A numeric reason is still useful if the COM control cannot provide text.
        }

        Disconnected?.Invoke(
            this,
            new RdpDisconnectedEventArgs(reason, extendedReason, description));
    }

    private object GetClient()
    {
        if (!IsHandleCreated)
            CreateControl();

        return GetOcx() ?? throw new InvalidOperationException(
            "Das Microsoft Remote Desktop ActiveX-Control konnte nicht initialisiert werden.");
    }
}
