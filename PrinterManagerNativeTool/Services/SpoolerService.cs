using System.Diagnostics;
using System.IO;
using System.ServiceProcess;

namespace PrinterManagerNativeTool.Services;

public sealed class SpoolerService
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(25);

    public string GetStatus()
    {
        using var service = new ServiceController("Spooler");
        service.Refresh();
        return service.Status.ToString();
    }

    public async Task RestartAsync()
    {
        await Task.Run(() =>
        {
            using var service = new ServiceController("Spooler");
            service.Refresh();

            if (service.Status != ServiceControllerStatus.Stopped &&
                service.Status != ServiceControllerStatus.StopPending)
            {
                service.Stop();
            }

            service.WaitForStatus(ServiceControllerStatus.Stopped, Timeout);
            service.Start();
            service.WaitForStatus(ServiceControllerStatus.Running, Timeout);
        });
    }

    public async Task ClearAsync()
    {
        await Task.Run(() =>
        {
            using var service = new ServiceController("Spooler");
            var shouldRestart = false;

            try
            {
                service.Refresh();
                if (service.Status != ServiceControllerStatus.Stopped)
                {
                    shouldRestart = true;
                    if (service.Status != ServiceControllerStatus.StopPending)
                        service.Stop();

                    service.WaitForStatus(ServiceControllerStatus.Stopped, Timeout);
                }

                var spoolDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    "System32", "spool", "PRINTERS");

                if (!Directory.Exists(spoolDirectory))
                    return;

                foreach (var file in Directory.EnumerateFiles(spoolDirectory))
                {
                    File.Delete(file);
                }
            }
            finally
            {
                service.Refresh();
                if (shouldRestart && service.Status == ServiceControllerStatus.Stopped)
                {
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, Timeout);
                }
            }
        });
    }

    public static void RunElevated(string action)
    {
        var executable = Environment.ProcessPath
            ?? throw new InvalidOperationException("Impossibile determinare il percorso dell'eseguibile.");

        Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            Arguments = $"--admin {action}",
            Verb = "runas",
            UseShellExecute = true
        });
    }
}
