using System.Windows;
using System.Windows.Controls;
using NetSupport.RemoteAdmin.Models;
using NetSupport.RemoteAdmin.Services;

namespace NetSupport.RemoteAdmin.Views;

public partial class NetSupportDiagnosticsWindow : Window
{
    private readonly INetSupportInstallationService _installationService;
    private readonly string? _configuredPath;

    public NetSupportDiagnosticsWindow(
        INetSupportInstallationService installationService,
        string? configuredPath)
    {
        InitializeComponent();
        _installationService = installationService;
        _configuredPath = configuredPath;
        ConfiguredPathTextBlock.Text = string.IsNullOrWhiteSpace(configuredPath)
            ? "Nicht konfiguriert"
            : configuredPath;
        RefreshCandidates();
    }

    public string? SelectedExecutablePath { get; private set; }

    private void RefreshButton_OnClick(object sender, RoutedEventArgs e) => RefreshCandidates();

    private void RefreshCandidates()
    {
        var candidates = _installationService.Discover(_configuredPath);
        CandidatesListBox.ItemsSource = candidates;
        CandidatesListBox.SelectedItem = candidates.FirstOrDefault(candidate => candidate.IsUsable)
                                             ?? candidates.FirstOrDefault();
        UpdateSelectionState();
    }

    private void CandidatesListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdateSelectionState();

    private void UpdateSelectionState()
    {
        UseSelectedButton.IsEnabled = CandidatesListBox.SelectedItem is NetSupportInstallationCandidate { IsUsable: true };
    }

    private void UseSelectedButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (CandidatesListBox.SelectedItem is not NetSupportInstallationCandidate { IsUsable: true } candidate)
            return;

        SelectedExecutablePath = candidate.Path;
        DialogResult = true;
        Close();
    }
}
