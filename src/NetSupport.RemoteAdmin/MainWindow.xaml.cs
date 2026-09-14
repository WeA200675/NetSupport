using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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
        UpdateSelectedTargetCard(null);

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

    private RemoteTarget? SelectedTarget => TargetsListBox.SelectedItem as RemoteTarget;

    private RemoteTarget? CurrentTarget
    {
        get
        {
            if (SelectedTarget is not null)
                return SelectedTarget;

            var host = HostTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(host))
                return null;

            return _targets.FirstOrDefault(t => string.Equals(t.Host, host, StringComparison.OrdinalIgnoreCase))
                   ?? new RemoteTarget { Name = host, Host = host };
        }
    }

    private void RefreshTargets()
    {
        var selectedHost = SelectedTarget?.Host;
        var filter = FilterTextBox?.Text?.Trim() ?? string.Empty;
        var query = _targets.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(t =>
                t.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                t.Host.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                (t.Description?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var items = query
            .OrderBy(t => string.IsNullOrWhiteSpace(t.Name) ? t.Host : t.Name)
            .ToList();

        TargetsListBox.ItemsSource = null;
        TargetsListBox.ItemsSource = items;

        if (!string.IsNullOrWhiteSpace(selectedHost))
        {
            TargetsListBox.SelectedItem = items.FirstOrDefault(t =>
                string.Equals(t.Host, selectedHost, StringComparison.OrdinalIgnoreCase));
        }
    }

    private void FilterTextBox_OnTextChanged(object sender, TextChangedEventArgs e) => RefreshTargets();

    private void ProviderComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
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

    private void TargetsListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var target = SelectedTarget;
        UpdateSelectedTargetCard(target);

        if (target is not null)
            HostTextBox.Text = target.Host;
    }

    private void UpdateSelectedTargetCard(RemoteTarget? target)
    {
        if (target is null)
        {
            SelectedTargetNameTextBlock.Text = "Kein Rechner ausgewählt";
            SelectedTargetHostTextBlock.Text = string.Empty;
            SelectedTargetStatusTextBlock.Text = "Rechner auswählen oder oben einen Namen eingeben.";
            return;
        }

        SelectedTargetNameTextBlock.Text = string.IsNullOrWhiteSpace(target.Name) ? target.Host : target.Name;
        SelectedTargetHostTextBlock.Text = target.Host;
        SelectedTargetStatusTextBlock.Text = target.StatusText;
    }

    private async void QuickActionButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag })
            return;

        var parts = tag.Split('|', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !Enum.TryParse<RemoteAction>(parts[1], true, out var action))
        {
            StatusTextBlock.Text = "Schnellaktion ist ungültig konfiguriert.";
            return;
        }

        await LaunchProviderActionAsync(parts[0], action);
    }

    private async Task LaunchProviderActionAsync(string providerId, RemoteAction action)
    {
        var target = CurrentTarget;
        if (target is null)
        {
            StatusTextBlock.Text = "Bitte zuerst einen Rechner auswählen oder eingeben.";
            HostTextBox.Focus();
            return;
        }

        try
        {
            var provider = _providers.Get(providerId);
            if (!provider.IsAvailable)
                throw new InvalidOperationException($"{provider.DisplayName} ist auf diesem Rechner nicht verfügbar.");

            if (!provider.SupportedActions.Contains(action))
                throw new NotSupportedException($"{provider.DisplayName} unterstützt die Aktion '{action}' nicht.");

            StatusTextBlock.Text = $"Starte {provider.DisplayName} für {target.Host} …";
            await provider.ConnectAsync(target, action);
            StatusTextBlock.Text = $"{provider.DisplayName} für {target.Host} gestartet.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Aktion konnte nicht gestartet werden.";
            System.Windows.MessageBox.Show(ex.Message, "Remote-Aktion", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
            System.Windows.MessageBox.Show(ex.Message, "Active Directory", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            UpdateSelectedTargetCard(SelectedTarget);

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
        if (SelectedTarget is not RemoteTarget target)
            return;

        HostTextBox.Text = target.Host;
        await LaunchProviderActionAsync("netsupport", RemoteAction.Control);
    }

    private async Task ConnectAsync()
    {
        var target = CurrentTarget;
        if (target is null)
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
            StatusTextBlock.Text = $"Starte {provider.DisplayName} für {target.Host} …";
            await provider.ConnectAsync(target, action);
            StatusTextBlock.Text = $"Verbindung zu {target.Host} gestartet.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Verbindung konnte nicht gestartet werden.";
            System.Windows.MessageBox.Show(ex.Message, "Remote-Verbindung", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ConnectButton.IsEnabled = true;
        }
    }

    private async void SaveTargetButton_OnClick(object sender, RoutedEventArgs e)
    {
        var target = CurrentTarget;
        if (target is null)
            return;

        if (_config.Targets.Any(t => string.Equals(t.Host, target.Host, StringComparison.OrdinalIgnoreCase)))
        {
            StatusTextBlock.Text = $"{target.Host} ist bereits gespeichert.";
            return;
        }

        _config.Targets.Add(new RemoteTarget
        {
            Name = target.Name,
            Host = target.Host,
            Description = target.Description
        });

        if (!_targets.Any(t => string.Equals(t.Host, target.Host, StringComparison.OrdinalIgnoreCase)))
            _targets.Add(target);

        await _configService.SaveAsync(_config);
        RefreshTargets();
        StatusTextBlock.Text = $"{target.Host} gespeichert.";
    }

    private void OpenConfigButton_OnClick(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(_configService.ConfigDirectory);
        _ = Process.Start(new ProcessStartInfo
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
