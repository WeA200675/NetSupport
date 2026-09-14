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

    public RdpSessionWindow(RemoteTarget target)
    {
        InitializeComponent();

        _target = target;
        TargetTextBlock.Text = string.IsNullOrWhiteSpace(target.Name)
            ? target.Host
            : $"{target.Name}  ·  {target.Host}";

        RdpHost.Child = _rdpControl;
        Loaded += (_, _) => Connect();
        Closing += (_, _) => _rdpControl.DisconnectSession();
    }

    private void Connect()
    {
        try
        {
            StatusTextBlock.Text = $"Verbinde mit {_target.Host} …";

            var width = Math.Max(800, (int)Math.Round(RdpHost.ActualWidth));
            var height = Math.Max(600, (int)Math.Round(RdpHost.ActualHeight));
            _rdpControl.ConnectTo(_target.Host, width, height);

            StatusTextBlock.Text = $"RDP-Session zu {_target.Host} gestartet";
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

    private void ReconnectButton_OnClick(object sender, RoutedEventArgs e)
    {
        _rdpControl.DisconnectSession();
        Connect();
    }

    private void DisconnectButton_OnClick(object sender, RoutedEventArgs e)
    {
        _rdpControl.DisconnectSession();
        StatusTextBlock.Text = "Getrennt";
    }

    private void FullscreenButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (WindowStyle == WindowStyle.None && WindowState == WindowState.Maximized)
        {
            WindowStyle = _previousWindowStyle;
            WindowState = _previousWindowState;
            return;
        }

        _previousWindowStyle = WindowStyle;
        _previousWindowState = WindowState == WindowState.Minimized ? WindowState.Normal : WindowState;
        WindowStyle = WindowStyle.None;
        WindowState = WindowState.Maximized;
    }
}
