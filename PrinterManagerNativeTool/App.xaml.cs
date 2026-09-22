using System.Windows;
using PrinterManagerNativeTool.Services;

namespace PrinterManagerNativeTool;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Length >= 2 &&
            string.Equals(e.Args[0], "--admin", StringComparison.OrdinalIgnoreCase))
        {
            await RunAdministrativeActionAsync(e.Args[1]);
            Shutdown();
            return;
        }

        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    private static async Task RunAdministrativeActionAsync(string action)
    {
        var spooler = new SpoolerService();

        try
        {
            switch (action.ToLowerInvariant())
            {
                case "restart-spooler":
                    await spooler.RestartAsync();
                    MessageBox.Show(
                        "Spooler di stampa riavviato correttamente.",
                        "Printer Manager",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    break;

                case "clear-spooler":
                    await spooler.ClearAsync();
                    MessageBox.Show(
                        "Coda spooler pulita correttamente.",
                        "Printer Manager",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    break;

                default:
                    throw new ArgumentException($"Azione amministrativa sconosciuta: {action}");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Printer Manager - Errore",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
