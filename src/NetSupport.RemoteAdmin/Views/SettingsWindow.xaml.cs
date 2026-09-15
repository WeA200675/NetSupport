using System.Diagnostics;
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

            var product = string.IsNullOrWhiteSpace(candidate.ProductName)
                ? "NetSupport Manager"
                : candidate.ProductName;
            NetSupportInfoTextBlock.Text = $"Gefunden: {product} · Version {candidate.VersionText}";
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
        if (!string.IsNullOrWhiteSpace(netSupportPath) &&
            (!File.Exists(netSupportPath) ||
             !string.Equals(Path.GetFileName(netSupportPath), "PCICTLUI.EXE", StringComparison.OrdinalIgnoreCase)))
        {
            var result = System.Windows.MessageBox.Show(
                "Der angegebene NetSupport-Pfad zeigt nicht auf eine vorhandene PCICTLUI.EXE. Trotzdem speichern?",
                "NetSupport-Pfad",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
                return;
        }

        try
        {
            _config.StartMinimized = StartMinimizedCheckBox.IsChecked == true;
            _config.DiagnosticLoggingEnabled = DiagnosticLoggingCheckBox.IsChecked == true;
            _config.NetSupportExecutable = string.IsNullOrWhiteSpace(netSupportPath) ? null : netSupportPath;

            _autoStartService.SetEnabled(AutoStartCheckBox.IsChecked == true);
            await _configService.SaveAsync(_config);

            _diagnosticLog.Info("Einstellungen gespeichert. Remotezugriffsrichtlinie bleibt NetSupport-only.");
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
