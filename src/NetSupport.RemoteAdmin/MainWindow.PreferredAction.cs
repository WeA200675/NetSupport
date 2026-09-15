using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Providers;
using WpfButton = System.Windows.Controls.Button;

namespace NetSupport.RemoteAdmin;

public partial class MainWindow
{
    private bool _preferredActionHooksInstalled;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        if (_preferredActionHooksInstalled)
            return;

        _preferredActionHooksInstalled = true;

        // Keep the existing target-card population and add the preferred-action layer after it.
        TargetsListBox.SelectionChanged += PreferredAction_TargetSelectionChanged;
        ActionComboBox.SelectionChanged += PreferredAction_EditorSelectionChanged;
        ProviderComboBox.SelectionChanged += PreferredAction_ProviderSelectionChanged;

        // Replace only the workflows whose default action used to be hard-coded to Control.
        StandardConnectButton.Click -= StandardConnectButton_OnClick;
        StandardConnectButton.Click += StandardConnectWithPreferredActionButton_OnClick;

        TargetsListBox.MouseDoubleClick -= TargetsListBox_OnMouseDoubleClick;
        TargetsListBox.MouseDoubleClick += TargetsListBox_WithPreferredAction_OnMouseDoubleClick;

        // The save button has no x:Name in the original XAML. Find it once and replace its handler.
        var saveButton = FindButtonByContent(this, "Speichern / Aktualisieren");
        if (saveButton is not null)
        {
            saveButton.Click -= SaveTargetButton_OnClick;
            saveButton.Click += SaveTargetWithPreferredActionButton_OnClick;
        }

        UpdatePreferredActionEditor(SelectedTarget);
    }

    private void PreferredAction_TargetSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdatePreferredActionEditor(SelectedTarget);

    private void PreferredAction_EditorSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_preferredActionHooksInstalled)
            return;

        UpdateStandardActionButtonText(GetEditorPreferredAction());
    }

    private void PreferredAction_ProviderSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_preferredActionHooksInstalled)
            return;

        // The existing provider handler rebuilds the action list. Re-apply the target preference
        // afterwards so a provider refresh does not silently reset the visible standard action.
        Dispatcher.BeginInvoke((Action)(() => UpdatePreferredActionEditor(SelectedTarget)));
    }

    private void UpdatePreferredActionEditor(RemoteTarget? target)
    {
        var action = target is null ? RemoteAction.Control : ResolveStoredPreferredAction(target);

        if (ActionComboBox.ItemsSource is IEnumerable<RemoteAction> actions && actions.Contains(action))
            ActionComboBox.SelectedItem = action;
        else if (ActionComboBox.Items.Contains(action))
            ActionComboBox.SelectedItem = action;

        UpdateStandardActionButtonText(action);
    }

    private void UpdateStandardActionButtonText(RemoteAction action)
    {
        StandardConnectButton.Content = $"Standardaktion: {GetActionDisplayName(action)}";
        StandardConnectButton.ToolTip = "Startet die gewählte NetSupport-Aktion. Dauerhaft wird sie erst mit 'Speichern / Aktualisieren'.";
    }

    private RemoteAction GetEditorPreferredAction()
    {
        if (ActionComboBox.SelectedItem is RemoteAction action && IsApprovedStandardAction(action))
            return action;

        return RemoteAction.Control;
    }

    private static RemoteAction ResolveStoredPreferredAction(RemoteTarget target)
    {
        if (!string.IsNullOrWhiteSpace(target.PreferredAction) &&
            Enum.TryParse<RemoteAction>(target.PreferredAction, ignoreCase: true, out var action) &&
            IsApprovedStandardAction(action))
        {
            return action;
        }

        return RemoteAction.Control;
    }

    private static bool IsApprovedStandardAction(RemoteAction action) => action is
        RemoteAction.Control or
        RemoteAction.View or
        RemoteAction.Chat or
        RemoteAction.Inventory or
        RemoteAction.CommandPrompt or
        RemoteAction.FileTransfer;

    private static string GetActionDisplayName(RemoteAction action) => action switch
    {
        RemoteAction.Control => "Steuern",
        RemoteAction.View => "Nur ansehen",
        RemoteAction.Chat => "Chat",
        RemoteAction.Inventory => "Inventar",
        RemoteAction.CommandPrompt => "Remote CMD",
        RemoteAction.FileTransfer => "Dateien",
        _ => action.ToString()
    };

    private async void StandardConnectWithPreferredActionButton_OnClick(object sender, RoutedEventArgs e)
    {
        var target = CurrentTarget;
        if (target is null)
        {
            StatusTextBlock.Text = "Bitte zuerst einen Rechner auswählen oder eingeben.";
            return;
        }

        var provider = ResolvePreferredControlProvider(target);
        if (provider is null)
        {
            StatusTextBlock.Text = "NetSupport Manager ist auf diesem Admin-PC nicht verfügbar.";
            return;
        }

        // Allow trying a changed editor value immediately without persisting it implicitly.
        await LaunchProviderActionAsync(provider.Id, GetEditorPreferredAction());
    }

    private async void TargetsListBox_WithPreferredAction_OnMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
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

        // Double-click is deliberately deterministic: it uses the last saved preference,
        // not an unsaved editor change. Missing/invalid legacy values fall back to Control.
        await LaunchProviderActionAsync(provider.Id, ResolveStoredPreferredAction(target));
    }

    private async void SaveTargetWithPreferredActionButton_OnClick(object sender, RoutedEventArgs e)
    {
        var target = CurrentTarget;
        if (target is null)
            return;

        if (SelectedTarget is not null)
            ApplyTargetEditor(target);
        else
            target.PreferredProviderId = ApprovedProviderId;

        target.PreferredAction = GetEditorPreferredAction().ToString();

        var existing = _config.Targets.FirstOrDefault(saved =>
            string.Equals(saved.Host, target.Host, StringComparison.OrdinalIgnoreCase));
        var wasExisting = existing is not null;

        if (existing is null)
        {
            existing = CreatePersistentTarget(target);
            existing.PreferredAction = target.PreferredAction;
            _config.Targets.Add(existing);

            if (!_targets.Any(item => string.Equals(item.Host, target.Host, StringComparison.OrdinalIgnoreCase)))
                _targets.Add(existing);
        }
        else
        {
            CopyPersistentTargetValues(target, existing);
            existing.PreferredAction = target.PreferredAction;
        }

        await _configService.SaveAsync(_config);
        RefreshGroupFilterOptions();
        RefreshTargets();
        SelectTargetByHost(target.Host);
        UpdatePreferredActionEditor(SelectedTarget ?? existing);

        _diagnosticLog.Info(
            $"Ziel {(wasExisting ? "aktualisiert" : "gespeichert")}: {target.Host}; Standardaktion {target.PreferredAction}");
        StatusTextBlock.Text = wasExisting
            ? $"{target.Host} aktualisiert · Standardaktion {GetActionDisplayName(GetEditorPreferredAction())}."
            : $"{target.Host} gespeichert · Standardaktion {GetActionDisplayName(GetEditorPreferredAction())}.";
    }

    private static WpfButton? FindButtonByContent(DependencyObject root, string content)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is WpfButton button && string.Equals(button.Content?.ToString(), content, StringComparison.Ordinal))
                return button;

            var nested = FindButtonByContent(child, content);
            if (nested is not null)
                return nested;
        }

        return null;
    }
}
