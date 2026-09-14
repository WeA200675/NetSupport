using System.Windows;
using NetSupport.RemoteAdmin.Controls;
using NetSupport.RemoteAdmin.Models;

namespace NetSupport.RemoteAdmin.Views;

public partial class RdpSessionWindow : Window
{
    private readonly RemoteTarget _target;
    private readonly RdpActiveXControl _rdpControl = new();
    private WindowState _previousWindowState = WindowState.Normal;
    private WindowStyle _previousWindowStyle = WindowStyle.SingleBorderWindow;
    private bool _disconnectRequestedByUser;

    public RdpSessionWindow(RemoteTarget target)
    {
        InitializeComponent();

        _target = target;
        TargetTextBlock.Text = string.IsNullOrWhiteSpace(target.Name)
            ? target.Host
            : $"{target.Name}  ·  {target.Host}";

        UserNameTextBox.Text = target.RdpUserName ?? string.Empty;
        DomainTextBox.Text = target.RdpDomain ?? string.Empty;
        ClipboardCheckBox.IsChecked = target.RdpRedirectClipboard;
        AdminSessionCheckBox.IsChecked = target.RdpAdminSession;
        MultiMonitorCheckBox.IsChecked = target.RdpUseMultiMonitor;

        _rdpControl.Connecting += (_, _) => SetStatus($"Verbinde mit {_target.Host} …");
        _rdpControl.Connected += (_, _) => SetStatus($"Transport zu {_target.Host} hergestellt – Anmeldung läuft …");
        _rdpControl.LoginCompleted += (_, _) => SetStatus($"Verbunden mit {_target.Host}");
        _rdpControl.Disconnected += (_, e) => OnDisconnected(e);
        _rdpControl.FatalError += (_, e) => SetStatus($"RDP-Fehler {e.ErrorCode}");
        _rdpControl.RemoteDesktopSizeChanged += (_, e) =>
            SetStatus($"Verbunden mit {_target.Host} · Remote {e.Width}×{e.Height}");
        _rdpControl.AutoReconnecting += (_, e) => OnAutoReconnecting(e);
        _rdpControl.AutoReconnected += (_, _) => SetStatus($"Automatisch wieder verbunden mit {_target.Host}");

        RdpHost.Child = _rdpControl;
        Loaded += (_, _) => Connect();
        Closing += (_, _) => _rdpControl.DisconnectSession();
    }

    private void Connect()
    {
        try
        {
            _disconnectRequestedByUser = false;
            StatusTextBlock.Text = $"Verbinde mit {_target.Host} …";

            var width = Math.Max(800, (int)Math.Round(RdpHost.ActualWidth));
            var height = Math.Max(600, (int)Math.Round(RdpHost.ActualHeight));
            var (userName, domain) = ResolveIdentity();
            var redirectClipboard = ClipboardCheckBox.IsChecked == true;
            var adminSession = AdminSessionCheckBox.IsChecked == true;
            var useMultiMonitor = MultiMonitorCheckBox.IsChecked == true;

            _target.RdpUserName = string.IsNullOrWhiteSpace(userName) ? null : userName;
            _target.RdpDomain = string.IsNullOrWhiteSpace(domain) ? null : domain;
            _target.RdpRedirectClipboard = redirectClipboard;
            _target.RdpAdminSession = adminSession;
            _target.RdpUseMultiMonitor = useMultiMonitor;

            _rdpControl.ConnectTo(
                _target.Host,
                width,
                height,
                userName,
                domain,
                redirectClipboard,
                adminSession,
                useMultiMonitor);

            // SmartSizing is intended for a single desktop surface. Multi-monitor sessions
            // use the native monitor layout instead.
            _rdpControl.SetSmartSizing(!useMultiMonitor && SmartSizingCheckBox.IsChecked == true);

            if (useMultiMonitor)
                SetStatus($"Verbinde mit {_target.Host} über mehrere Monitore …");
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "RDP-Verbindung konnte nicht gestartet werden.";
            System.Windows.MessageBox.Show(
                ex.Message,
                "Eingebettetes RDP",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private (string? UserName, string? Domain) ResolveIdentity()
    {
        var userName = UserNameTextBox.Text.Trim();
        var domain = DomainTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(domain))
        {
            var separator = userName.IndexOf('\\');
            if (separator > 0 && separator < userName.Length - 1)
            {
                domain = userName[..separator];
                userName = userName[(separator + 1)..];
                DomainTextBox.Text = domain;
                UserNameTextBox.Text = userName;
            }
        }

        return (
            string.IsNullOrWhiteSpace(userName) ? null : userName,
            string.IsNullOrWhiteSpace(domain) ? null : domain);
    }

    private void OnDisconnected(RdpDisconnectedEventArgs e)
    {
        if (_disconnectRequestedByUser)
        {
            SetStatus("Getrennt");
            return;
        }

        var details = string.IsNullOrWhiteSpace(e.Description)
            ? $"Grund {e.Reason}, erweitert {e.ExtendedReason}"
            : e.Description;

        SetStatus($"Verbindung getrennt: {details}");
    }

    private void OnAutoReconnecting(RdpAutoReconnectingEventArgs e)
    {
        var network = e.NetworkAvailable ? "Netz verfügbar" : "Netz nicht verfügbar";
        var attempts = e.MaxAttemptCount > 0
            ? $"Versuch {e.AttemptCount}/{e.MaxAttemptCount}"
            : $"Versuch {e.AttemptCount}";

        SetStatus($"Automatische Wiederverbindung: {attempts} · {network} · Grund {e.DisconnectReason}");
    }

    private void SetStatus(string text)
    {
        if (Dispatcher.CheckAccess())
        {
            StatusTextBlock.Text = text;
            return;
        }

        _ = Dispatcher.InvokeAsync(() => StatusTextBlock.Text = text);
    }

    private void RunRemoteAction(RdpRemoteAction action, string successText)
    {
        try
        {
            _rdpControl.SendRemoteAction(action);
            SetStatus(successText);
        }
        catch (Exception ex)
        {
            SetStatus($"Remote-Aktion nicht verfügbar: {ex.Message}");
        }
    }

    private void ReconnectButton_OnClick(object sender, RoutedEventArgs e)
    {
        _disconnectRequestedByUser = true;
        _rdpControl.DisconnectSession();
        Connect();
    }

    private void DisconnectButton_OnClick(object sender, RoutedEventArgs e)
    {
        _disconnectRequestedByUser = true;
        _rdpControl.DisconnectSession();
        StatusTextBlock.Text = "Getrennt";
    }

    private void SmartSizingCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        if (MultiMonitorCheckBox?.IsChecked == true)
            return;

        _rdpControl.SetSmartSizing(SmartSizingCheckBox.IsChecked == true);
    }

    private void AppSwitchButton_OnClick(object sender, RoutedEventArgs e) =>
        RunRemoteAction(RdpRemoteAction.AppSwitch, "Alt+Tab an Remotesitzung gesendet");

    private void StartScreenButton_OnClick(object sender, RoutedEventArgs e) =>
        RunRemoteAction(RdpRemoteAction.StartScreen, "Start-Aktion an Remotesitzung gesendet");

    private void TaskManagerButton_OnClick(object sender, RoutedEventArgs e) =>
        RunRemoteAction(RdpRemoteAction.TaskManager, "Task-Manager-Aktion an Remotesitzung gesendet");

    private void FullscreenButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (WindowStyle == WindowStyle.None && WindowState == WindowState.Maximized)
        {
            WindowStyle = _previousWindowStyle;
            WindowState = _previousWindowState;
            FullscreenButton.Content = "Vollbild";
            return;
        }

        _previousWindowStyle = WindowStyle;
        _previousWindowState = WindowState == WindowState.Minimized ? WindowState.Normal : WindowState;
        WindowStyle = WindowStyle.None;
        WindowState = WindowState.Maximized;
        FullscreenButton.Content = "Fenstermodus";
    }
}
