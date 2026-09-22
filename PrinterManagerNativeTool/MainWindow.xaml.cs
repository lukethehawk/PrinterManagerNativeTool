using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using PrinterManagerNativeTool.Models;
using PrinterManagerNativeTool.Services;

namespace PrinterManagerNativeTool;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<PrinterInfo> _printers = [];
    private readonly ICollectionView _printerView;
    private readonly PrinterService _printerService = new();
    private readonly NetworkDiagnosticsService _networkDiagnostics = new();
    private readonly SpoolerService _spoolerService = new();

    private NetworkDiagnosticResult? _lastNetworkDiagnostic;

    public MainWindow()
    {
        InitializeComponent();

        _printerView = CollectionViewSource.GetDefaultView(_printers);
        _printerView.Filter = FilterPrinter;
        PrintersGrid.ItemsSource = _printerView;

        Loaded += async (_, _) => await RefreshPrintersAsync();
    }

    private PrinterInfo? SelectedPrinter => PrintersGrid.SelectedItem as PrinterInfo;

    private bool FilterPrinter(object item)
    {
        if (item is not PrinterInfo printer)
            return false;

        var query = SearchBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query))
            return true;

        return Contains(printer.Name, query)
            || Contains(printer.PortName, query)
            || Contains(printer.DriverName, query)
            || Contains(printer.NetworkHost, query);
    }

    private static bool Contains(string? value, string query) =>
        value?.Contains(query, StringComparison.CurrentCultureIgnoreCase) == true;

    private async Task RefreshPrintersAsync()
    {
        var previousSelection = SelectedPrinter?.Name;
        RefreshButton.IsEnabled = false;
        StatusBarText.Text = "Lettura stampanti in corso...";

        try
        {
            var printers = await Task.Run(_printerService.GetPrinters);

            _printers.Clear();
            foreach (var printer in printers)
                _printers.Add(printer);

            if (!string.IsNullOrWhiteSpace(previousSelection))
            {
                PrintersGrid.SelectedItem = _printers.FirstOrDefault(
                    p => string.Equals(p.Name, previousSelection, StringComparison.OrdinalIgnoreCase));
            }

            if (PrintersGrid.SelectedItem is null && _printers.Count > 0)
                PrintersGrid.SelectedIndex = 0;

            StatusBarText.Text = $"{_printers.Count} stampanti rilevate";
            RefreshSpoolerStatus();
        }
        catch (Exception ex)
        {
            StatusBarText.Text = "Errore durante il caricamento";
            ShowError(ex);
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void RefreshSpoolerStatus()
    {
        try
        {
            SpoolerStatusText.Text = $"Spooler: {_spoolerService.GetStatus()}";
        }
        catch
        {
            SpoolerStatusText.Text = "Spooler: stato non disponibile";
        }
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) =>
        _printerView.Refresh();

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) =>
        await RefreshPrintersAsync();

    private void PrintersGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        _lastNetworkDiagnostic = null;
        UpdateDetails();
    }

    private void PrintersGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SelectedPrinter is not null)
            RunPrinterAction(p => _printerService.OpenQueue(p.Name));
    }

    private void UpdateDetails()
    {
        var printer = SelectedPrinter;
        var selected = printer is not null;

        PrinterActionsPanel.IsEnabled = selected;
        DiagnoseButton.IsEnabled = selected && printer!.IsNetworkPrinter;
        CopyDiagnosticButton.IsEnabled = selected;
        OpenWebButton.IsEnabled = selected && printer!.IsNetworkPrinter;

        if (!selected)
        {
            SelectedPrinterName.Text = "Seleziona una stampante";
            SelectedPrinterStatus.Text = string.Empty;
            DriverValue.Text = PortValue.Text = HostValue.Text = JobsValue.Text = DefaultValue.Text = "—";
            PingValue.Text = RawValue.Text = "—";
            return;
        }

        SelectedPrinterName.Text = printer!.Name;
        SelectedPrinterStatus.Text = printer.StatusText;
        DriverValue.Text = printer.DriverName;
        PortValue.Text = printer.PortName;
        HostValue.Text = printer.NetworkHost ?? "Non rilevato";
        JobsValue.Text = printer.JobCount.ToString();
        DefaultValue.Text = printer.IsDefault ? "Sì" : "No";
        PingValue.Text = printer.IsNetworkPrinter ? "Non eseguito" : "N/D";
        RawValue.Text = printer.IsNetworkPrinter ? "Non eseguito" : "N/D";
    }

    private void OpenQueueButton_Click(object sender, RoutedEventArgs e) =>
        RunPrinterAction(p => _printerService.OpenQueue(p.Name));

    private void PreferencesButton_Click(object sender, RoutedEventArgs e) =>
        RunPrinterAction(p => _printerService.OpenPreferences(p.Name));

    private void PropertiesButton_Click(object sender, RoutedEventArgs e) =>
        RunPrinterAction(p => _printerService.OpenProperties(p.Name));

    private async void SetDefaultButton_Click(object sender, RoutedEventArgs e)
    {
        var printer = SelectedPrinter;
        if (printer is null)
            return;

        try
        {
            _printerService.SetDefault(printer.Name);
            StatusBarText.Text = $"{printer.Name} impostata come predefinita";
            await RefreshPrintersAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void TestPageButton_Click(object sender, RoutedEventArgs e) =>
        RunPrinterAction(p => _printerService.PrintTestPage(p.Name));

    private void OpenWebButton_Click(object sender, RoutedEventArgs e) =>
        RunPrinterAction(_printerService.OpenWebInterface);

    private async void DiagnoseButton_Click(object sender, RoutedEventArgs e)
    {
        var printer = SelectedPrinter;
        if (printer?.NetworkHost is null)
            return;

        DiagnoseButton.IsEnabled = false;
        PingValue.Text = "Test in corso...";
        RawValue.Text = "Test in corso...";
        StatusBarText.Text = $"Diagnostica {printer.NetworkHost}...";

        try
        {
            _lastNetworkDiagnostic = await _networkDiagnostics.DiagnoseAsync(printer.NetworkHost);
            PingValue.Text = _lastNetworkDiagnostic.Ping;
            RawValue.Text = _lastNetworkDiagnostic.Raw9100;
            StatusBarText.Text = "Diagnostica completata";
        }
        catch (Exception ex)
        {
            StatusBarText.Text = "Diagnostica non riuscita";
            ShowError(ex);
        }
        finally
        {
            DiagnoseButton.IsEnabled = true;
        }
    }

    private void CopyDiagnosticButton_Click(object sender, RoutedEventArgs e)
    {
        var printer = SelectedPrinter;
        if (printer is null)
            return;

        try
        {
            Clipboard.SetText(BuildDiagnosticReport(printer));
            StatusBarText.Text = "Report diagnostico copiato negli Appunti";
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private string BuildDiagnosticReport(PrinterInfo printer)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Printer Manager Diagnostic Report");
        sb.AppendLine($"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        sb.AppendLine($"Computer: {Environment.MachineName}");
        sb.AppendLine($"Windows: {Environment.OSVersion.VersionString}");
        sb.AppendLine();
        sb.AppendLine($"Printer: {printer.Name}");
        sb.AppendLine($"Status: {printer.StatusText}");
        sb.AppendLine($"Default: {(printer.IsDefault ? "Yes" : "No")}");
        sb.AppendLine($"Driver: {printer.DriverName}");
        sb.AppendLine($"Port: {printer.PortName}");
        sb.AppendLine($"Host: {printer.NetworkHost ?? "N/A"}");
        sb.AppendLine($"Jobs: {printer.JobCount}");
        sb.AppendLine($"Spooler: {SafeSpoolerStatus()}");

        if (_lastNetworkDiagnostic is not null && printer.IsNetworkPrinter)
        {
            sb.AppendLine($"Ping: {_lastNetworkDiagnostic.Ping}");
            sb.AppendLine($"RAW 9100: {_lastNetworkDiagnostic.Raw9100}");
        }

        return sb.ToString();
    }

    private string SafeSpoolerStatus()
    {
        try
        {
            return _spoolerService.GetStatus();
        }
        catch
        {
            return "Unavailable";
        }
    }

    private void RestartSpoolerButton_Click(object sender, RoutedEventArgs e) =>
        RunElevatedSpoolerAction("restart-spooler");

    private void ClearSpoolerButton_Click(object sender, RoutedEventArgs e) =>
        RunElevatedSpoolerAction("clear-spooler");

    private void RunElevatedSpoolerAction(string action)
    {
        try
        {
            SpoolerService.RunElevated(action);
            StatusBarText.Text = "Operazione spooler avviata con privilegi amministrativi";
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            StatusBarText.Text = "Operazione annullata";
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void RunPrinterAction(Action<PrinterInfo> action)
    {
        var printer = SelectedPrinter;
        if (printer is null)
            return;

        try
        {
            action(printer);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private static void ShowError(Exception ex) =>
        MessageBox.Show(
            ex.Message,
            "Printer Manager - Errore",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
}
