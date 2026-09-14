using System.Diagnostics;
using System.Drawing;
using System.Windows;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Providers;
using NetSupport.RemoteAdmin.Services;
using Forms = System.Windows.Forms;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;

namespace NetSupport.RemoteAdmin;

public partial class MainWindow : Window
{
    private readonly AppConfig _config;
    private readonly ConfigService _configService;
    private readonly RemoteProviderRegistry _providers;
    private readonly ITargetDiscoveryService _discovery;
    private readonly HostAvailabilityService _availability;
    private readonly Forms.NotifyIcon _trayIcon;
    private readonly List<RemoteTarget> _targets = new();
    private CancellationTokenSource? _statusCancellation;
    private bool _allowExit;

    public MainWindow(
        AppConfig config,
        ConfigService configService,
        RemoteProviderRegistry providers,
        ITargetDiscoveryService discovery,
        HostAvailabilityService availability)
    {
        InitializeComponent();

        _config = config;
        _configService = configService;
        _providers = providers;
        _discovery = discovery;
        _availability = availability;
        _targets.AddRange(_config.Targets);

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
        var filter = FilterTextBox?.Text?.Trim() ?? string.Empty;
        var query = _targets.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(t =>
                t.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                t.Host.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                (t.Description?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        TargetsListBox.ItemsSource = null;
        TargetsListBox.ItemsSource = query
            .OrderBy(t => string.IsNullOrWhiteSpace(t.Name) ? t.Host : t.Name)
            .ToList();
    }

    private void FilterTextBox_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => RefreshTargets();

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

    private async void LoadDomainButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            LoadDomainButton.IsEnabled = false;
            StatusTextBlock.Text = $"Lade Rechner aus {_discovery.DisplayName} …";

            var discovered = await _discovery.DiscoverAsync();
            foreach (var target in discovered)
            {
                var existing = _targets.FirstOrDefault(t =>
                    string.Equals(t.Host, target.Host, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(t.Name) && string.Equals(t.Name, target.Name, StringComparison.OrdinalIgnoreCase)));

                if (existing is null)
                {
                    _targets.Add(target);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(existing.Name))
                        existing.Name = target.Name;
                    if (string.IsNullOrWhiteSpace(existing.Description))
                        existing.Description = target.Description;
                }
            }

            RefreshTargets();
            StatusTextBlock.Text = $"{discovered.Count} Domänenrechner gefunden, {_targets.Count} Rechner angezeigt.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Domänenrechner konnten nicht geladen werden.";
            MessageBox.Show(ex.Message, "Active Directory", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            LoadDomainButton.IsEnabled = true;
        }
    }

    private async void RefreshStatusButton_OnClick(object sender, RoutedEventArgs e)
    {
        _statusCancellation?.Cancel();
        _statusCancellation?.Dispose();
        _statusCancellation = new CancellationTokenSource();
        var cancellationToken = _statusCancellation.Token;

        try
        {
            RefreshStatusButton.IsEnabled = false;
            StatusTextBlock.Text = $"Prüfe {_targets.Count} Rechner …";

            using var gate = new SemaphoreSlim(12);
            var tasks = _targets.Select(async target =>
            {
                await gate.WaitAsync(cancellationToken);
                try
                {
                    target.Status = await _availability.IsOnlineAsync(target.Host, cancellationToken: cancellationToken)
                        ? HostStatus.Online
                        : HostStatus.Offline;
                }
                finally
                {
                    gate.Release();
                }
            });

            await Task.WhenAll(tasks);
            RefreshTargets();

            var online = _targets.Count(t => t.Status == HostStatus.Online);
            StatusTextBlock.Text = $"Status aktualisiert: {online} online, {_targets.Count - online} nicht erreichbar.";
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = "Statusprüfung abgebrochen.";
        }
        finally
        {
            RefreshStatusButton.IsEnabled = true;
        }
    }

    private async void ConnectButton_OnClick(object sender, RoutedEventArgs e) => await ConnectAsync();

    private async void HostTextBox_OnKeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Enter)
            return;

        e.Handled = true;
        await ConnectAsync();
    }

    private async void TargetsListBox_OnMouseDoubleClick(object sender, WpfMouseButtonEventArgs e)
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

        var target = _targets.FirstOrDefault(t => string.Equals(t.Host, host, StringComparison.OrdinalIgnoreCase))
            ?? new RemoteTarget { Name = host, Host = host };

        _config.Targets.Add(new RemoteTarget
        {
            Name = target.Name,
            Host = target.Host,
            Description = target.Description
        });

        if (!_targets.Contains(target))
            _targets.Add(target);

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
        _statusCancellation?.Cancel();
        _statusCancellation?.Dispose();
        _allowExit = true;
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        System.Windows.Application.Current.Shutdown();
    }
}
