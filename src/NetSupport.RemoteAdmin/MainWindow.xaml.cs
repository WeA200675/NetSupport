using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Providers;
using NetSupport.RemoteAdmin.Services;
using Forms = System.Windows.Forms;

namespace NetSupport.RemoteAdmin;

public partial class MainWindow : Window
{
    private readonly AppConfig _config;
    private readonly ConfigService _configService;
    private readonly RemoteProviderRegistry _providers;
    private readonly Forms.NotifyIcon _trayIcon;
    private bool _allowExit;

    public MainWindow(AppConfig config, ConfigService configService, RemoteProviderRegistry providers)
    {
        InitializeComponent();

        _config = config;
        _configService = configService;
        _providers = providers;

        ProviderComboBox.ItemsSource = _providers.All.Where(p => p.IsAvailable).ToList();
        ProviderComboBox.DisplayMemberPath = nameof(IRemoteProvider.DisplayName);
        ProviderComboBox.SelectedIndex = ProviderComboBox.Items.Count > 0 ? 0 : -1;

        RefreshTargets();

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Öffnen", null, (_, _) => ShowFromTray());
        menu.Items.Add("Beenden", null, (_, _) => ExitApplication());

        _trayIcon = new Forms.NotifyIcon
        {
            Text = "NetSupport Remote Admin",
            Icon = SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = menu
        };
        _trayIcon.DoubleClick += (_, _) => ShowFromTray();

        Closing += (_, e) =>
        {
            if (_allowExit)
                return;

            e.Cancel = true;
            Hide();
            StatusTextBlock.Text = "Läuft im Infobereich weiter";
        };
    }

    private void RefreshTargets()
    {
        TargetsListBox.ItemsSource = null;
        TargetsListBox.ItemsSource = _config.Targets
            .OrderBy(t => string.IsNullOrWhiteSpace(t.Name) ? t.Host : t.Name)
            .ToList();
    }

    private void ProviderComboBox_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ProviderComboBox.SelectedItem is not IRemoteProvider provider)
        {
            ActionComboBox.ItemsSource = null;
            return;
        }

        ActionComboBox.ItemsSource = provider.SupportedActions;
        ActionComboBox.SelectedItem = provider.SupportedActions.Contains(RemoteAction.Control)
            ? RemoteAction.Control
            : provider.SupportedActions.FirstOrDefault();
    }

    private async void ConnectButton_OnClick(object sender, RoutedEventArgs e) => await ConnectAsync();

    private async void HostTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        await ConnectAsync();
    }

    private async void TargetsListBox_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (TargetsListBox.SelectedItem is not RemoteTarget target)
            return;

        HostTextBox.Text = target.Host;
        await ConnectAsync();
    }

    private async Task ConnectAsync()
    {
        var host = HostTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            StatusTextBlock.Text = "Bitte Rechnername oder IP-Adresse eingeben.";
            HostTextBox.Focus();
            return;
        }

        if (ProviderComboBox.SelectedItem is not IRemoteProvider provider ||
            ActionComboBox.SelectedItem is not RemoteAction action)
        {
            StatusTextBlock.Text = "Kein verfügbarer Remote-Provider ausgewählt.";
            return;
        }

        try
        {
            ConnectButton.IsEnabled = false;
            StatusTextBlock.Text = $"Starte {provider.DisplayName} für {host} …";
            await provider.ConnectAsync(new RemoteTarget { Host = host, Name = host }, action);
            StatusTextBlock.Text = $"Verbindung zu {host} gestartet.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Verbindung konnte nicht gestartet werden.";
            MessageBox.Show(ex.Message, "Remote-Verbindung", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ConnectButton.IsEnabled = true;
        }
    }

    private async void SaveTargetButton_OnClick(object sender, RoutedEventArgs e)
    {
        var host = HostTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(host))
            return;

        if (_config.Targets.Any(t => string.Equals(t.Host, host, StringComparison.OrdinalIgnoreCase)))
        {
            StatusTextBlock.Text = $"{host} ist bereits gespeichert.";
            return;
        }

        _config.Targets.Add(new RemoteTarget { Name = host, Host = host });
        await _configService.SaveAsync(_config);
        RefreshTargets();
        StatusTextBlock.Text = $"{host} gespeichert.";
    }

    private void OpenConfigButton_OnClick(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(_configService.ConfigDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = _configService.ConfigDirectory,
            UseShellExecute = true
        });
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        HostTextBox.Focus();
    }

    private void ExitApplication()
    {
        _allowExit = true;
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Current.Shutdown();
    }
}
