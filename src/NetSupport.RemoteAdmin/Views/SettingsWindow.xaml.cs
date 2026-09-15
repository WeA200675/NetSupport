using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using NetSupport.RemoteAdmin.Services;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace NetSupport.RemoteAdmin.Views;

public partial class SettingsWindow : Window
{
    private readonly AppConfig _config;
    private readonly ConfigService _configService;
    private readonly IAutoStartService _autoStartService;
    private readonly IDiagnosticLogService _diagnosticLog;
    private readonly ISupportBundleService _supportBundleService;
    private readonly ISystemHealthService _systemHealthService;
    private readonly INetSupportInstallationService _netSupportInstallationService = new NetSupportInstallationService();
    private readonly INetSupportProfileService _netSupportProfileService = new NetSupportProfileService();

    public SettingsWindow(
        AppConfig config,
        ConfigService configService,
        IAutoStartService autoStartService,
        IDiagnosticLogService diagnosticLog)
    {
        InitializeComponent();

        _config = config;
        _configService = configService;
        _autoStartService = autoStartService;
        _diagnosticLog = diagnosticLog;
        _supportBundleService = new SupportBundleService(
            _config,
            _configService,
            new JsonSessionHistoryService(_configService.ConfigDirectory),
            _diagnosticLog);
        _systemHealthService = new SystemHealthService(_config, _configService, _autoStartService);

        AutoStartCheckBox.IsChecked = _autoStartService.IsEnabled;
        StartMinimizedCheckBox.IsChecked = _config.StartMinimized;
        DiagnosticLoggingCheckBox.IsChecked = _config.DiagnosticLoggingEnabled;
        NetSupportPathTextBox.Text = _config.NetSupportExecutable ?? string.Empty;
        NetSupportClientPortTextBox.Text = _config.NetSupportClientPort.ToString(CultureInfo.InvariantCulture);
        NetSupportLockProfileCheckBox.IsChecked = _config.NetSupportLockProfile;
        RefreshNetSupportProfiles(_config.NetSupportProfileName);
        LogPathTextBlock.Text = $"Protokoll: {_diagnosticLog.LogPath}";
        UpdateNetSupportSummary();
    }

    private void BrowseNetSupportButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new WpfOpenFileDialog
        {
            Title = "PCICTLUI.EXE auswählen",
            Filter = "NetSupport Control (PCICTLUI.EXE)|PCICTLUI.EXE|Programme (*.exe)|*.exe|Alle Dateien (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (!string.IsNullOrWhiteSpace(NetSupportPathTextBox.Text))
        {
            try
            {
                dialog.InitialDirectory = Path.GetDirectoryName(NetSupportPathTextBox.Text);
            }
            catch
            {
                // Ignore an invalid manually entered path while opening the picker.
            }
        }

        if (dialog.ShowDialog(this) == true)
            NetSupportPathTextBox.Text = dialog.FileName;
    }

    private void AutoDetectNetSupportButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var detected = _netSupportInstallationService.FindBestExecutable(NetSupportPathTextBox.Text);
            if (string.IsNullOrWhiteSpace(detected))
            {
                System.Windows.MessageBox.Show(
                    "Es wurde keine vorhandene PCICTLUI.EXE in den bekannten NetSupport-Pfaden oder den lokalen Installationsinformationen gefunden.",
                    "NetSupport erkennen",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            NetSupportPathTextBox.Text = detected;
            _diagnosticLog.Info($"NetSupport automatisch erkannt: {Path.GetFileName(detected)}");
        }
        catch (Exception ex)
        {
            _diagnosticLog.Error("NetSupport konnte nicht automatisch erkannt werden.", ex);
            System.Windows.MessageBox.Show(ex.Message, "NetSupport erkennen", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void InspectNetSupportButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new NetSupportDiagnosticsWindow(
                _netSupportInstallationService,
                NetSupportPathTextBox.Text)
            {
                Owner = this
            };

            if (window.ShowDialog() == true && !string.IsNullOrWhiteSpace(window.SelectedExecutablePath))
                NetSupportPathTextBox.Text = window.SelectedExecutablePath;
        }
        catch (Exception ex)
        {
            _diagnosticLog.Error("NetSupport-Installationsprüfung fehlgeschlagen.", ex);
            System.Windows.MessageBox.Show(ex.Message, "NetSupport prüfen", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshNetSupportProfilesButton_OnClick(object sender, RoutedEventArgs e) =>
        RefreshNetSupportProfiles(NetSupportProfileComboBox.Text);

    private void RefreshNetSupportProfiles(string? preferredProfile)
    {
        var profiles = _netSupportProfileService.DiscoverProfiles();
        NetSupportProfileComboBox.ItemsSource = profiles;
        NetSupportProfileComboBox.Text = preferredProfile?.Trim() ?? string.Empty;

        if (profiles.Count == 0)
        {
            NetSupportProfileInfoTextBlock.Text =
                $"Keine lokalen Control-Profile unter HKCU\\{NetSupportProfileService.ConfigListRegistryPath} gefunden. Leer lassen, um NetSupports Standardverhalten zu verwenden.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(preferredProfile) &&
            !profiles.Any(profile => string.Equals(profile, preferredProfile.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            NetSupportProfileInfoTextBlock.Text =
                $"{profiles.Count} Profil(e) gefunden. Das aktuell eingetragene Profil '{preferredProfile.Trim()}' ist lokal nicht vorhanden.";
            return;
        }

        NetSupportProfileInfoTextBlock.Text =
            $"{profiles.Count} lokale(s) Control-Profil(e) gefunden. Leer lassen = NetSupports Standardverhalten.";
    }

    private void NetSupportPathTextBox_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) =>
        UpdateNetSupportSummary();

    private void UpdateNetSupportSummary()
    {
        if (NetSupportInfoTextBlock is null)
            return;

        var path = NetSupportPathTextBox?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            NetSupportInfoTextBlock.Text = "Kein NetSupport-Control-Pfad konfiguriert.";
            return;
        }

        try
        {
            var candidate = _netSupportInstallationService.Inspect(path, "Eingabe");
            if (!candidate.Exists)
            {
                NetSupportInfoTextBlock.Text = "PCICTLUI.EXE wurde an diesem Pfad nicht gefunden.";
                return;
            }

            if (!candidate.IsControlExecutable)
            {
                NetSupportInfoTextBlock.Text = "Datei vorhanden, aber nicht PCICTLUI.EXE. Dieser Pfad kann nicht als NetSupport-Control verwendet werden.";
                return;
            }

            var product = string.IsNullOrWhiteSpace(candidate.ProductName)
                ? "NetSupport Manager"
                : candidate.ProductName;
            NetSupportInfoTextBlock.Text = $"Gültig: {product} · Version {candidate.VersionText}";
        }
        catch (Exception ex)
        {
            NetSupportInfoTextBlock.Text = $"Pfad konnte nicht geprüft werden: {ex.Message}";
        }
    }

    private void SystemHealthButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new SystemHealthWindow(_systemHealthService, _diagnosticLog)
        {
            Owner = this
        };
        window.ShowDialog();
    }

    private void OpenLogFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(_diagnosticLog.LogDirectory);
            _ = Process.Start(new ProcessStartInfo
            {
                FileName = _diagnosticLog.LogDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _diagnosticLog.Error("Diagnoseordner konnte nicht geöffnet werden.", ex);
            System.Windows.MessageBox.Show(ex.Message, "Diagnoseordner", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void CreateSupportBundleButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new WpfSaveFileDialog
        {
            Title = "Supportpaket speichern",
            Filter = "ZIP-Archiv (*.zip)|*.zip|Alle Dateien (*.*)|*.*",
            DefaultExt = ".zip",
            AddExtension = true,
            FileName = $"NetSupport-RemoteAdmin-Support-{DateTime.Now:yyyyMMdd-HHmm}.zip"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            CreateSupportBundleButton.IsEnabled = false;
            var anonymize = AnonymizeSupportBundleCheckBox.IsChecked != false;
            await _supportBundleService.CreateAsync(dialog.FileName, anonymize);

            System.Windows.MessageBox.Show(
                $"Supportpaket erstellt:\n{dialog.FileName}\n\nAnonymisierung: {(anonymize ? "aktiv" : "deaktiviert")}",
                "Supportpaket",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _diagnosticLog.Error("Supportpaket konnte nicht erstellt werden.", ex);
            System.Windows.MessageBox.Show(ex.Message, "Supportpaket", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            CreateSupportBundleButton.IsEnabled = true;
        }
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        var netSupportPath = NetSupportPathTextBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(netSupportPath))
        {
            var candidate = _netSupportInstallationService.Inspect(netSupportPath, "Eingabe");
            if (!candidate.IsUsable)
            {
                var result = System.Windows.MessageBox.Show(
                    "Der angegebene NetSupport-Pfad zeigt nicht auf eine vorhandene PCICTLUI.EXE. Trotzdem als Konfigurationswert speichern? Remote-Aktionen bleiben damit blockiert, bis ein gültiger Pfad gesetzt ist.",
                    "NetSupport-Pfad",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                    return;
            }
        }

        if (!int.TryParse(
                NetSupportClientPortTextBox.Text.Trim(),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var clientPort) || clientPort is < 1 or > 65535)
        {
            System.Windows.MessageBox.Show(
                "Der NetSupport-Client-Port muss eine Zahl zwischen 1 und 65535 sein. Standard ist TCP 5405.",
                "NetSupport Client-Port",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            NetSupportClientPortTextBox.Focus();
            return;
        }

        string? profileName = null;
        var profileInput = NetSupportProfileComboBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(profileInput))
        {
            try
            {
                profileName = NetSupportProfileService.NormalizeProfileName(profileInput);
            }
            catch (ArgumentException ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "NetSupport-Profil", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_netSupportProfileService.ProfileExists(profileName))
            {
                var result = System.Windows.MessageBox.Show(
                    $"Das Control-Profil '{profileName}' wurde auf diesem Windows-Benutzerkonto derzeit nicht gefunden. Trotzdem speichern? Remote-Aktionen werden blockiert, bis dieses Profil in NetSupport Manager vorhanden ist.",
                    "NetSupport-Profil",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                    return;
            }
        }

        var lockProfile = NetSupportLockProfileCheckBox.IsChecked == true;
        if (lockProfile && profileName is null)
        {
            System.Windows.MessageBox.Show(
                "'/F' kann nur zusammen mit einem ausgewählten NetSupport-Control-Profil verwendet werden.",
                "NetSupport-Profil",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            _config.StartMinimized = StartMinimizedCheckBox.IsChecked == true;
            _config.DiagnosticLoggingEnabled = DiagnosticLoggingCheckBox.IsChecked == true;
            _config.NetSupportExecutable = string.IsNullOrWhiteSpace(netSupportPath) ? null : netSupportPath;
            _config.NetSupportClientPort = clientPort;
            _config.NetSupportProfileName = profileName;
            _config.NetSupportLockProfile = lockProfile;

            _autoStartService.SetEnabled(AutoStartCheckBox.IsChecked == true);
            await _configService.SaveAsync(_config);

            _diagnosticLog.Info(
                $"Einstellungen gespeichert. Remotezugriffsrichtlinie bleibt NetSupport-only. Client-Port: {clientPort}; Control-Profil: {(profileName ?? "Standard")}; Profilbindung: {lockProfile}.");
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            _diagnosticLog.Error("Einstellungen konnten nicht gespeichert werden.", ex);
            System.Windows.MessageBox.Show(ex.Message, "Einstellungen", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
