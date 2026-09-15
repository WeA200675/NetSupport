using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Providers;
using NetSupport.RemoteAdmin.Services;
using NetSupport.RemoteAdmin.Views;
using Forms = System.Windows.Forms;
using WpfButton = System.Windows.Controls.Button;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace NetSupport.RemoteAdmin;

public partial class MainWindow : Window
{
    private const string AllGroupsLabel = "Alle Gruppen";
    private const string ApprovedProviderId = "netsupport";

    private readonly AppConfig _config;
    private readonly ConfigService _configService;
    private readonly RemoteProviderRegistry _providers;
    private readonly ITargetDiscoveryService _discovery;
    private readonly HostAvailabilityService _availability;
    private readonly ITargetDetailsService _detailsService;
    private readonly ISessionHistoryService _historyService;
    private readonly IAutoStartService _autoStartService;
    private readonly IDiagnosticLogService _diagnosticLog;
    private readonly List<IRemoteProvider> _availableProviders;
    private readonly Forms.NotifyIcon _trayIcon;
    private readonly List<RemoteTarget> _targets = new();
    private readonly List<SessionHistoryEntry> _historyEntries = new();
    private CancellationTokenSource? _statusCancellation;
    private bool _allowExit;
    private bool _uiReady;
    private bool _updatingGroupFilter;
    private bool _updatingSavedViews;
    private bool _policyNormalizationPending;

    public MainWindow(
        AppConfig config,
        ConfigService configService,
        RemoteProviderRegistry providers,
        ITargetDiscoveryService discovery,
        HostAvailabilityService availability,
        ITargetDetailsService detailsService,
        ISessionHistoryService historyService,
        IAutoStartService autoStartService,
        IDiagnosticLogService diagnosticLog)
    {
        InitializeComponent();

        _config = config;
        _configService = configService;
        _providers = providers;
        _discovery = discovery;
        _availability = availability;
        _detailsService = detailsService;
        _historyService = historyService;
        _autoStartService = autoStartService;
        _diagnosticLog = diagnosticLog;

        _policyNormalizationPending = NormalizeDomainPolicyPreferences();
        _targets.AddRange(_config.Targets);
        _availableProviders = _providers.All.Where(provider => provider.IsAvailable).ToList();

        RefreshProviderAvailability();
        RefreshGroupFilterOptions();
        RefreshSavedViewOptions();
        RefreshTargets();
        UpdateSelectedTargetCard(null);
        _uiReady = true;

        Loaded += async (_, _) =>
        {
            if (_policyNormalizationPending)
            {
                try
                {
                    await _configService.SaveAsync(_config);
                    _policyNormalizationPending = false;
                    _diagnosticLog.Info("Alte nicht zulässige Remote-Provider-Präferenzen wurden auf NetSupport normalisiert.");
                }
                catch (Exception ex)
                {
                    _diagnosticLog.Error("Domänenrichtlinien-Migration konnte nicht gespeichert werden.", ex);
                }
            }

            await RefreshHistoryAsync();
        };

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

        _diagnosticLog.Info("Hauptfenster initialisiert. Domänenrichtlinie: NetSupport-only.");
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
                   ?? new RemoteTarget { Name = host, Host = host, PreferredProviderId = ApprovedProviderId };
        }
    }

    private bool NormalizeDomainPolicyPreferences()
    {
        var changed = false;
        foreach (var target in _config.Targets)
        {
            if (!string.Equals(target.PreferredProviderId, ApprovedProviderId, StringComparison.OrdinalIgnoreCase))
            {
                target.PreferredProviderId = ApprovedProviderId;
                changed = true;
            }
        }

        return changed;
    }

    private void RefreshProviderAvailability()
    {
        var selectedProviderId = (ProviderComboBox.SelectedItem as IRemoteProvider)?.Id;

        _availableProviders.Clear();
        _availableProviders.AddRange(_providers.All.Where(provider =>
            provider.IsAvailable &&
            string.Equals(provider.Id, ApprovedProviderId, StringComparison.OrdinalIgnoreCase)));

        ProviderComboBox.ItemsSource = null;
        ProviderComboBox.ItemsSource = _availableProviders;
        ProviderComboBox.DisplayMemberPath = nameof(IRemoteProvider.DisplayName);
        ProviderComboBox.SelectedItem = _availableProviders.FirstOrDefault(provider =>
            string.Equals(provider.Id, selectedProviderId, StringComparison.OrdinalIgnoreCase))
            ?? _availableProviders.FirstOrDefault();

        PreferredProviderComboBox.ItemsSource = null;
        PreferredProviderComboBox.ItemsSource = _availableProviders;
        PreferredProviderComboBox.DisplayMemberPath = nameof(IRemoteProvider.DisplayName);
        PreferredProviderComboBox.SelectedItem = _availableProviders.FirstOrDefault();
    }

    private void RefreshTargets()
    {
        var selectedHost = SelectedTarget?.Host;
        var filter = FilterTextBox?.Text?.Trim() ?? string.Empty;
        var selectedGroup = GroupFilterComboBox?.SelectedItem as string;
        var favoritesOnly = FavoritesOnlyCheckBox?.IsChecked == true;
        var query = _targets.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(t =>
                t.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                t.Host.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                (t.Description?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Group?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (favoritesOnly)
            query = query.Where(target => target.IsFavorite);

        if (!string.IsNullOrWhiteSpace(selectedGroup) &&
            !string.Equals(selectedGroup, AllGroupsLabel, StringComparison.Ordinal))
        {
            query = query.Where(target =>
                string.Equals(target.Group?.Trim(), selectedGroup, StringComparison.OrdinalIgnoreCase));
        }

        var items = query
            .OrderByDescending(target => target.IsFavorite)
            .ThenBy(target => string.IsNullOrWhiteSpace(target.Group) ? 1 : 0)
            .ThenBy(target => target.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(target => string.IsNullOrWhiteSpace(target.Name) ? target.Host : target.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        TargetsListBox.ItemsSource = null;
        TargetsListBox.ItemsSource = items;

        if (!string.IsNullOrWhiteSpace(selectedHost))
            SelectTargetByHost(selectedHost);
    }

    private void RefreshGroupFilterOptions()
    {
        _updatingGroupFilter = true;
        try
        {
            var selected = GroupFilterComboBox.SelectedItem as string;
            var groups = _targets
                .Select(target => target.Group?.Trim())
                .Where(group => !string.IsNullOrWhiteSpace(group))
                .Select(group => group!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var items = new List<string> { AllGroupsLabel };
            items.AddRange(groups);
            GroupFilterComboBox.ItemsSource = items;

            GroupFilterComboBox.SelectedItem = !string.IsNullOrWhiteSpace(selected) &&
                                                items.Contains(selected, StringComparer.OrdinalIgnoreCase)
                ? items.First(item => string.Equals(item, selected, StringComparison.OrdinalIgnoreCase))
                : AllGroupsLabel;
        }
        finally
        {
            _updatingGroupFilter = false;
        }
    }

    private void RefreshSavedViewOptions(string? selectName = null)
    {
        _updatingSavedViews = true;
        try
        {
            var currentName = selectName ?? (SavedViewComboBox.SelectedItem as SavedTargetView)?.Name;
            var items = _config.SavedViews
                .OrderBy(view => view.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            SavedViewComboBox.ItemsSource = items;

            var selected = string.IsNullOrWhiteSpace(currentName)
                ? null
                : items.FirstOrDefault(view =>
                    string.Equals(view.Name, currentName, StringComparison.OrdinalIgnoreCase));

            SavedViewComboBox.SelectedItem = selected;
            if (selected is not null)
                SavedViewComboBox.Text = selected.Name;
        }
        finally
        {
            _updatingSavedViews = false;
        }
    }

    private void SelectTargetByHost(string host)
    {
        if (TargetsListBox.ItemsSource is not IEnumerable<RemoteTarget> items)
            return;

        TargetsListBox.SelectedItem = items.FirstOrDefault(target =>
            string.Equals(target.Host, host, StringComparison.OrdinalIgnoreCase));
    }

    private void FilterTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_uiReady)
            RefreshTargets();
    }

    private void GroupFilterComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_uiReady && !_updatingGroupFilter)
            RefreshTargets();
    }

    private void FavoritesOnlyCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_uiReady)
            RefreshTargets();
    }

    private void SavedViewComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_uiReady || _updatingSavedViews)
            return;

        if (SavedViewComboBox.SelectedItem is SavedTargetView view)
            ApplySavedView(view);
    }

    private void ApplySavedView(SavedTargetView view)
    {
        _uiReady = false;
        try
        {
            FilterTextBox.Text = view.SearchText ?? string.Empty;
            FavoritesOnlyCheckBox.IsChecked = view.FavoritesOnly;

            var groups = GroupFilterComboBox.ItemsSource as IEnumerable<string>;
            var selectedGroup = !string.IsNullOrWhiteSpace(view.Group) &&
                                groups?.FirstOrDefault(group =>
                                    string.Equals(group, view.Group, StringComparison.OrdinalIgnoreCase)) is { } match
                ? match
                : AllGroupsLabel;

            GroupFilterComboBox.SelectedItem = selectedGroup;
        }
        finally
        {
            _uiReady = true;
        }

        RefreshTargets();
        StatusTextBlock.Text = $"Ansicht '{view.Name}' angewendet.";
    }

    private async void SaveViewButton_OnClick(object sender, RoutedEventArgs e)
    {
        var name = SavedViewComboBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusTextBlock.Text = "Bitte einen Namen für die Ansicht eingeben.";
            SavedViewComboBox.Focus();
            return;
        }

        var selectedGroup = GroupFilterComboBox.SelectedItem as string;
        if (string.Equals(selectedGroup, AllGroupsLabel, StringComparison.Ordinal))
            selectedGroup = null;

        var existing = _config.SavedViews.FirstOrDefault(view =>
            string.Equals(view.Name, name, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            existing = new SavedTargetView { Name = name };
            _config.SavedViews.Add(existing);
        }

        existing.SearchText = NullIfWhiteSpace(FilterTextBox.Text);
        existing.Group = NullIfWhiteSpace(selectedGroup);
        existing.FavoritesOnly = FavoritesOnlyCheckBox.IsChecked == true;

        await _configService.SaveAsync(_config);
        RefreshSavedViewOptions(existing.Name);
        _diagnosticLog.Info($"Gespeicherte Ansicht aktualisiert: {existing.Name}");
        StatusTextBlock.Text = $"Ansicht '{existing.Name}' gespeichert.";
    }

    private async void DeleteViewButton_OnClick(object sender, RoutedEventArgs e)
    {
        var selected = SavedViewComboBox.SelectedItem as SavedTargetView;
        var name = selected?.Name ?? SavedViewComboBox.Text.Trim();
        var existing = _config.SavedViews.FirstOrDefault(view =>
            string.Equals(view.Name, name, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            StatusTextBlock.Text = "Keine gespeicherte Ansicht zum Löschen ausgewählt.";
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"Ansicht '{existing.Name}' wirklich löschen?",
            "Gespeicherte Ansicht",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
            return;

        _config.SavedViews.Remove(existing);
        await _configService.SaveAsync(_config);
        RefreshSavedViewOptions();
        SavedViewComboBox.SelectedItem = null;
        SavedViewComboBox.Text = string.Empty;
        _diagnosticLog.Info($"Gespeicherte Ansicht gelöscht: {existing.Name}");
        StatusTextBlock.Text = $"Ansicht '{existing.Name}' gelöscht.";
    }

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
        var hasTarget = target is not null;
        RefreshDetailsButton.IsEnabled = hasTarget;
        StandardConnectButton.IsEnabled = hasTarget;
        FavoriteCheckBox.IsEnabled = hasTarget;
        TargetGroupTextBox.IsEnabled = hasTarget;
        PreferredProviderComboBox.IsEnabled = hasTarget;

        if (target is null)
        {
            SelectedTargetNameTextBlock.Text = "Kein Rechner ausgewählt";
            SelectedTargetHostTextBlock.Text = string.Empty;
            SelectedTargetStatusTextBlock.Text = "Rechner auswählen oder oben einen Namen eingeben.";
            FavoriteCheckBox.IsChecked = false;
            TargetGroupTextBox.Text = string.Empty;
            PreferredProviderComboBox.SelectedItem = null;
            SelectedTargetIpTextBlock.Text = "–";
            SelectedTargetUserTextBlock.Text = "–";
            SelectedTargetOsTextBlock.Text = "–";
            SelectedTargetModelTextBlock.Text = "–";
            SelectedTargetLastCheckTextBlock.Text = "–";
            SelectedTargetLastSessionTextBlock.Text = "–";
            SelectedTargetDetailsErrorTextBlock.Text = string.Empty;
            return;
        }

        SelectedTargetNameTextBlock.Text = string.IsNullOrWhiteSpace(target.Name) ? target.Host : target.Name;
        SelectedTargetHostTextBlock.Text = target.Host;
        SelectedTargetStatusTextBlock.Text = target.StatusText;
        FavoriteCheckBox.IsChecked = target.IsFavorite;
        TargetGroupTextBox.Text = target.Group ?? string.Empty;
        PreferredProviderComboBox.SelectedItem = ResolvePreferredControlProvider(target);

        var details = target.Details;
        SelectedTargetIpTextBlock.Text = details?.IpAddresses ?? "–";
        SelectedTargetUserTextBlock.Text = details?.LoggedOnUser ?? "–";
        SelectedTargetOsTextBlock.Text = details?.OperatingSystemDisplay ?? "–";
        SelectedTargetModelTextBlock.Text = details?.ComputerModel ?? "–";

        var lastCheck = target.LastStatusCheck ?? details?.CheckedAt;
        SelectedTargetLastCheckTextBlock.Text = lastCheck is null
            ? "–"
            : lastCheck.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");

        var lastSession = _historyEntries.FirstOrDefault(entry =>
            string.Equals(entry.Host, target.Host, StringComparison.OrdinalIgnoreCase));
        SelectedTargetLastSessionTextBlock.Text = lastSession is null
            ? "–"
            : $"{lastSession.StartedAt.ToLocalTime():dd.MM.yyyy HH:mm} · {lastSession.ProviderName} · {lastSession.Action}" +
              (lastSession.Succeeded ? string.Empty : " · Fehler");

        SelectedTargetDetailsErrorTextBlock.Text = string.IsNullOrWhiteSpace(details?.ManagementError)
            ? string.Empty
            : $"Verwaltungsdaten nicht vollständig: {details.ManagementError}";
    }

    private IRemoteProvider? ResolvePreferredControlProvider(RemoteTarget target) =>
        _availableProviders.FirstOrDefault(provider =>
            provider.SupportedActions.Contains(RemoteAction.Control) &&
            string.Equals(provider.Id, ApprovedProviderId, StringComparison.OrdinalIgnoreCase));

    private void ApplyTargetEditor(RemoteTarget target)
    {
        target.IsFavorite = FavoriteCheckBox.IsChecked == true;
        target.Group = NullIfWhiteSpace(TargetGroupTextBox.Text);
        target.PreferredProviderId = ApprovedProviderId;
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private async void StandardConnectButton_OnClick(object sender, RoutedEventArgs e)
    {
        var target = CurrentTarget;
        if (target is null)
            return;

        var provider = ResolvePreferredControlProvider(target);
        if (provider is null)
        {
            StatusTextBlock.Text = "NetSupport Manager ist auf diesem Admin-PC nicht verfügbar.";
            return;
        }

        await LaunchProviderActionAsync(provider.Id, RemoteAction.Control);
    }

    private async void RefreshDetailsButton_OnClick(object sender, RoutedEventArgs e)
    {
        var target = CurrentTarget;
        if (target is null)
        {
            StatusTextBlock.Text = "Bitte zuerst einen Rechner auswählen.";
            return;
        }

        try
        {
            RefreshDetailsButton.IsEnabled = false;
            StatusTextBlock.Text = $"Lade Rechnerdetails für {target.Host} über {_detailsService.DisplayName} …";

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            target.Details = await _detailsService.GetDetailsAsync(target, timeout.Token);

            if (SelectedTarget is not null &&
                string.Equals(SelectedTarget.Host, target.Host, StringComparison.OrdinalIgnoreCase))
            {
                UpdateSelectedTargetCard(target);
            }

            StatusTextBlock.Text = string.IsNullOrWhiteSpace(target.Details.ManagementError)
                ? $"Rechnerdetails für {target.Host} aktualisiert."
                : $"Rechnerdetails für {target.Host} teilweise geladen; CIM/WSMan ist nicht vollständig verfügbar.";
        }
        catch (OperationCanceledException)
        {
            _diagnosticLog.Info($"Rechnerdetails Zeitlimit: {target.Host}");
            StatusTextBlock.Text = $"Rechnerdetails für {target.Host}: Zeitlimit erreicht.";
        }
        catch (Exception ex)
        {
            _diagnosticLog.Error($"Rechnerdetails fehlgeschlagen: {target.Host}", ex);
            StatusTextBlock.Text = $"Rechnerdetails konnten nicht geladen werden: {ex.Message}";
        }
        finally
        {
            RefreshDetailsButton.IsEnabled = SelectedTarget is not null;
        }
    }

    private async void QuickActionButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not WpfButton { Tag: string tag })
            return;

        var parts = tag.Split('|', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !Enum.TryParse<RemoteAction>(parts[1], true, out var action))
        {
            StatusTextBlock.Text = "Schnellaktion ist ungültig konfiguriert.";
            return;
        }

        if (!string.Equals(parts[0], ApprovedProviderId, StringComparison.OrdinalIgnoreCase))
        {
            StatusTextBlock.Text = "Dieser Remote-Provider ist gemäß Domänenrichtlinie nicht zugelassen.";
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

        if (!string.Equals(providerId, ApprovedProviderId, StringComparison.OrdinalIgnoreCase))
        {
            var policyError = "Remotezugriff ist in dieser Domäne ausschließlich über NetSupport Manager zulässig.";
            _diagnosticLog.Info($"Nicht zugelassener Remote-Provider blockiert: {providerId} -> {target.Host}");
            StatusTextBlock.Text = policyError;
            System.Windows.MessageBox.Show(policyError, "Domänenrichtlinie", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IRemoteProvider? provider = null;
        try
        {
            provider = _providers.Get(providerId);
            if (!provider.IsAvailable)
                throw new InvalidOperationException($"{provider.DisplayName} ist auf diesem Rechner nicht verfügbar.");

            if (!provider.SupportedActions.Contains(action))
                throw new NotSupportedException($"{provider.DisplayName} unterstützt die Aktion '{action}' nicht.");

            _diagnosticLog.Info($"Remote-Aktion startet: {provider.Id}/{action} -> {target.Host}");
            StatusTextBlock.Text = $"Starte {provider.DisplayName} für {target.Host} …";
            await provider.ConnectAsync(target, action);
            await RecordHistorySafeAsync(target, provider, providerId, action, succeeded: true, error: null);
            _diagnosticLog.Info($"Remote-Aktion gestartet: {provider.Id}/{action} -> {target.Host}");
            StatusTextBlock.Text = $"{provider.DisplayName} für {target.Host} gestartet.";
        }
        catch (Exception ex)
        {
            await RecordHistorySafeAsync(target, provider, providerId, action, succeeded: false, error: ex.Message);
            _diagnosticLog.Error($"Remote-Aktion fehlgeschlagen: {providerId}/{action} -> {target.Host}", ex);
            StatusTextBlock.Text = "Aktion konnte nicht gestartet werden.";
            System.Windows.MessageBox.Show(ex.Message, "Remote-Aktion", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RecordHistorySafeAsync(
        RemoteTarget target,
        IRemoteProvider? provider,
        string providerId,
        RemoteAction action,
        bool succeeded,
        string? error)
    {
        try
        {
            await _historyService.RecordAsync(new SessionHistoryEntry
            {
                StartedAt = DateTimeOffset.Now,
                Host = target.Host,
                TargetName = NullIfWhiteSpace(target.Name),
                ProviderId = provider?.Id ?? providerId,
                ProviderName = provider?.DisplayName ?? providerId,
                Action = action.ToString(),
                Succeeded = succeeded,
                Error = NullIfWhiteSpace(error)
            });

            await RefreshHistoryAsync();
        }
        catch (Exception ex)
        {
            _diagnosticLog.Error("Verbindungsverlauf konnte nicht geschrieben werden.", ex);
        }
    }

    private async Task RefreshHistoryAsync()
    {
        try
        {
            var entries = await _historyService.GetRecentAsync(20);
            _historyEntries.Clear();
            _historyEntries.AddRange(entries);
            HistoryListBox.ItemsSource = null;
            HistoryListBox.ItemsSource = _historyEntries;
            UpdateSelectedTargetCard(SelectedTarget ?? CurrentTarget);
        }
        catch (Exception ex)
        {
            _diagnosticLog.Error("Verbindungsverlauf konnte nicht gelesen werden.", ex);
        }
    }

    private async void RefreshHistoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        await RefreshHistoryAsync();
        StatusTextBlock.Text = $"Verlauf aktualisiert: {_historyEntries.Count} Einträge angezeigt.";
    }

    private async void ExportHistoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new WpfSaveFileDialog
        {
            Title = "Verbindungsverlauf als CSV exportieren",
            Filter = "CSV-Datei (*.csv)|*.csv|Alle Dateien (*.*)|*.*",
            DefaultExt = ".csv",
            AddExtension = true,
            FileName = $"NetSupport-RemoteAdmin-Verlauf-{DateTime.Now:yyyyMMdd-HHmm}.csv"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            await _historyService.ExportCsvAsync(dialog.FileName);
            _diagnosticLog.Info($"Verbindungsverlauf exportiert: {Path.GetFileName(dialog.FileName)}");
            StatusTextBlock.Text = $"Verlauf exportiert: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            _diagnosticLog.Error("CSV-Export des Verbindungsverlaufs fehlgeschlagen.", ex);
            StatusTextBlock.Text = "Verlauf konnte nicht exportiert werden.";
            System.Windows.MessageBox.Show(ex.Message, "CSV-Export", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ClearHistoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show(
            "Lokalen Verbindungsverlauf wirklich löschen?",
            "Verbindungsverlauf",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
            return;

        await _historyService.ClearAsync();
        await RefreshHistoryAsync();
        _diagnosticLog.Info("Lokaler Verbindungsverlauf gelöscht.");
        StatusTextBlock.Text = "Lokaler Verbindungsverlauf gelöscht.";
    }

    private void HistoryListBox_OnMouseDoubleClick(object sender, WpfMouseButtonEventArgs e)
    {
        if (HistoryListBox.SelectedItem is not SessionHistoryEntry entry)
            return;

        TargetsListBox.SelectedItem = null;
        HostTextBox.Text = entry.Host;

        var target = _targets.FirstOrDefault(item =>
            string.Equals(item.Host, entry.Host, StringComparison.OrdinalIgnoreCase));
        if (target is not null)
        {
            SelectTargetByHost(entry.Host);
            if (SelectedTarget is null)
                UpdateSelectedTargetCard(target);
        }

        StatusTextBlock.Text = $"{entry.Host} aus dem Verlauf übernommen.";
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
                    target.PreferredProviderId = ApprovedProviderId;
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
            _diagnosticLog.Info($"Active Directory geladen: {discovered.Count} Rechner gefunden.");
            StatusTextBlock.Text = $"{discovered.Count} Domänenrechner gefunden, {_targets.Count} Rechner angezeigt.";
        }
        catch (Exception ex)
        {
            _diagnosticLog.Error("Active-Directory-Suche fehlgeschlagen.", ex);
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
                    target.LastStatusCheck = DateTimeOffset.Now;
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
            _diagnosticLog.Info($"Statusprüfung beendet: {online}/{_targets.Count} online.");
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
        var provider = ResolvePreferredControlProvider(target);
        if (provider is null)
        {
            StatusTextBlock.Text = "NetSupport Manager ist auf diesem Admin-PC nicht verfügbar.";
            return;
        }

        await LaunchProviderActionAsync(provider.Id, RemoteAction.Control);
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
            StatusTextBlock.Text = "NetSupport Manager ist nicht verfügbar oder keine Aktion ist ausgewählt.";
            return;
        }

        try
        {
            ConnectButton.IsEnabled = false;
            await LaunchProviderActionAsync(provider.Id, action);
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

        if (SelectedTarget is not null)
            ApplyTargetEditor(target);
        else
            target.PreferredProviderId = ApprovedProviderId;

        var existing = _config.Targets.FirstOrDefault(saved =>
            string.Equals(saved.Host, target.Host, StringComparison.OrdinalIgnoreCase));
        var wasExisting = existing is not null;

        if (existing is null)
        {
            existing = CreatePersistentTarget(target);
            _config.Targets.Add(existing);

            if (!_targets.Any(item => string.Equals(item.Host, target.Host, StringComparison.OrdinalIgnoreCase)))
                _targets.Add(existing);
        }
        else
        {
            CopyPersistentTargetValues(target, existing);
        }

        await _configService.SaveAsync(_config);
        RefreshGroupFilterOptions();
        RefreshTargets();
        SelectTargetByHost(target.Host);

        _diagnosticLog.Info($"Ziel {(wasExisting ? "aktualisiert" : "gespeichert")}: {target.Host}");
        StatusTextBlock.Text = wasExisting
            ? $"{target.Host} aktualisiert."
            : $"{target.Host} gespeichert.";
    }

    private static RemoteTarget CreatePersistentTarget(RemoteTarget source)
    {
        var target = new RemoteTarget();
        CopyPersistentTargetValues(source, target);
        return target;
    }

    private static void CopyPersistentTargetValues(RemoteTarget source, RemoteTarget destination)
    {
        destination.Name = source.Name;
        destination.Host = source.Host;
        destination.Description = source.Description;
        destination.IsFavorite = source.IsFavorite;
        destination.Group = source.Group;
        destination.PreferredProviderId = ApprovedProviderId;
    }

    private void OpenSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new SettingsWindow(_config, _configService, _autoStartService, _diagnosticLog)
        {
            Owner = this
        };

        if (window.ShowDialog() == true)
        {
            RefreshProviderAvailability();
            UpdateSelectedTargetCard(SelectedTarget);
            StatusTextBlock.Text = "Einstellungen gespeichert.";
        }
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
        _diagnosticLog.Info("Anwendung wird beendet.");
        _allowExit = true;
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        System.Windows.Application.Current.Shutdown();
    }
}
