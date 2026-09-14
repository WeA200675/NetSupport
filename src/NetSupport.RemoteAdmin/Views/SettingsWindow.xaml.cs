using System.Diagnostics;
using System.IO;
using System.Windows;
using NetSupport.RemoteAdmin.Services;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace NetSupport.RemoteAdmin.Views;

public partial class SettingsWindow : Window
{
    private readonly AppConfig _config;
    private readonly ConfigService _configService;
    private readonly IAutoStartService _autoStartService;
    private readonly IDiagnosticLogService _diagnosticLog;

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

        AutoStartCheckBox.IsChecked = _autoStartService.IsEnabled;
        StartMinimizedCheckBox.IsChecked = _config.StartMinimized;
        UseEmbeddedRdpCheckBox.IsChecked = _config.UseEmbeddedRdp;
        UseFullScreenRdpCheckBox.IsChecked = _config.UseFullScreenRdp;
        DiagnosticLoggingCheckBox.IsChecked = _config.DiagnosticLoggingEnabled;
        NetSupportPathTextBox.Text = _config.NetSupportExecutable ?? string.Empty;
        LogPathTextBlock.Text = $"Protokoll: {_diagnosticLog.LogPath}";
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
            System.Windows.MessageBox.Show(ex.Message, "Diagnoseordner", MessageBoxButton.OK, MessageBoxImage.Error);
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
            _config.UseEmbeddedRdp = UseEmbeddedRdpCheckBox.IsChecked == true;
            _config.UseFullScreenRdp = UseFullScreenRdpCheckBox.IsChecked == true;
            _config.DiagnosticLoggingEnabled = DiagnosticLoggingCheckBox.IsChecked == true;
            _config.NetSupportExecutable = string.IsNullOrWhiteSpace(netSupportPath) ? null : netSupportPath;

            _autoStartService.SetEnabled(AutoStartCheckBox.IsChecked == true);
            await _configService.SaveAsync(_config);

            _diagnosticLog.Info("Einstellungen gespeichert.");
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
