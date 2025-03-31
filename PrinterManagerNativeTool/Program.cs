using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace PrinterManagerNativeTool
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();

            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "riavviaSpooler":
                        RiavviaSpooler();
                        return;
                    case "svuotaSpooler":
                        SvuotaSpooler();
                        return;
                }
            }

            Application.Run(new Form1());
        }

        static void RiavviaSpooler()
        {
            try
            {
                Process.Start(new ProcessStartInfo("sc", "stop spooler")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                })?.WaitForExit();

                Process.Start(new ProcessStartInfo("sc", "start spooler")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                })?.WaitForExit();

                MessageBox.Show("Spooler riavviato correttamente.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Errore nel riavvio dello spooler:\n" + ex.Message);
            }
        }

        static void SvuotaSpooler()
        {
            try
            {
                string path = @"C:\\Windows\\System32\\spool\\PRINTERS";
                foreach (var file in Directory.GetFiles(path))
                {
                    File.Delete(file);
                }

                MessageBox.Show("Spooler svuotato con successo.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Errore durante la pulizia dello spooler:\n" + ex.Message);
            }
        }
    }
}
