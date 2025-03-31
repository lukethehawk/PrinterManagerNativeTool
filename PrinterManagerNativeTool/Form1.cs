using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace PrinterManagerNativeTool
{
    public partial class Form1 : Form
    {
        private ListBox lstPrinters;
        private Button btnRicarica;
        private Button btnRiavviaSpooler;
        private Button btnSvuotaSpooler;
        private Label lblAdminWarning;
        private ContextMenuStrip contextMenu;

        public Form1()
        {
            InitializeComponent();
            _ = CaricaStampanti();
        }

        private void InitializeComponent()
        {
            this.lstPrinters = new ListBox();
            this.btnRicarica = new Button();
            this.btnRiavviaSpooler = new Button();
            this.btnSvuotaSpooler = new Button();
            this.lblAdminWarning = new Label();
            this.contextMenu = new ContextMenuStrip();

            // 
            // lstPrinters
            // 
            this.lstPrinters.Location = new Point(10, 10);
            this.lstPrinters.Size = new Size(380, 250);
            this.lstPrinters.ContextMenuStrip = this.contextMenu;
            this.lstPrinters.MouseDown += LstPrinters_MouseDown;

            // 
            // btnRicarica
            // 
            this.btnRicarica.Text = "Ricarica stampanti";
            this.btnRicarica.Location = new Point(10, 270);
            this.btnRicarica.Size = new Size(380, 30);
            this.btnRicarica.Click += BtnLoad_Click;

            // 
            // btnRiavviaSpooler
            // 
            this.btnRiavviaSpooler.Text = "Riavvia spooler";
            this.btnRiavviaSpooler.Location = new Point(10, 310);
            this.btnRiavviaSpooler.Size = new Size(185, 30);
            this.btnRiavviaSpooler.Click += BtnRiavviaSpooler_Click;

            // 
            // btnSvuotaSpooler
            // 
            this.btnSvuotaSpooler.Text = "Svuota spooler";
            this.btnSvuotaSpooler.Location = new Point(205, 310);
            this.btnSvuotaSpooler.Size = new Size(185, 30);
            this.btnSvuotaSpooler.Click += BtnSvuotaSpooler_Click;

            // 
            // lblAdminWarning
            // 
            this.lblAdminWarning.Text = "⚠ Alcune funzioni richiedono i permessi da amministratore.";
            this.lblAdminWarning.Font = new Font("Segoe UI", 7F, FontStyle.Italic);
            this.lblAdminWarning.ForeColor = Color.Gray;
            this.lblAdminWarning.AutoSize = true;
            this.lblAdminWarning.Location = new Point(10, 345);

            // 
            // contextMenu
            // 
            var preferenze = new ToolStripMenuItem("Preferenze di stampa");
            preferenze.Click += (s, e) => ApriPreferenze();

            var proprieta = new ToolStripMenuItem("Proprietà stampante...");
            proprieta.Click += (s, e) => ApriProprietaStampante();

            var spooler = new ToolStripMenuItem("Apri spooler");
            spooler.Click += (s, e) => ApriCartellaSpooler();

            var predefinita = new ToolStripMenuItem("Imposta come predefinita");
            predefinita.Click += (s, e) => ImpostaComePredefinita();

            contextMenu.Items.Add(preferenze);
            contextMenu.Items.Add(proprieta);
            contextMenu.Items.Add(spooler);
            contextMenu.Items.Add(predefinita);

            // 
            // Form1
            // 
            this.Text = "Printer Manager Tool";
            this.ClientSize = new Size(400, 370);
            this.Controls.Add(lstPrinters);
            this.Controls.Add(btnRicarica);
            this.Controls.Add(btnRiavviaSpooler);
            this.Controls.Add(btnSvuotaSpooler);
            this.Controls.Add(lblAdminWarning);
        }

        private async void BtnLoad_Click(object sender, EventArgs e)
        {
            await CaricaStampanti();
        }

        private async Task CaricaStampanti()
        {
            lstPrinters.Items.Clear();
            lstPrinters.Items.Add("Caricamento in corso...");

            List<PrinterInfo> printers = await Task.Run(() =>
            {
                return PrinterHelper.EnumPrintersMultiThread()
                    .OrderBy(p => p.PrinterName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            });

            lstPrinters.Items.Clear();
            foreach (var p in printers)
            {
                string stato = p.RawStatus == 0 ? "[Pronta]" : "[Offline]";
                lstPrinters.Items.Add($"{p.PrinterName} {stato}");
            }
        }

        private void BtnRiavviaSpooler_Click(object sender, EventArgs e)
        {
            EseguiComeAmministratore("riavviaSpooler");
        }

        private void BtnSvuotaSpooler_Click(object sender, EventArgs e)
        {
            EseguiComeAmministratore("svuotaSpooler");
        }

        private void EseguiComeAmministratore(string argomento)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    Arguments = argomento,
                    Verb = "runas",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Azione annullata o errore:\n" + ex.Message);
            }
        }

        private void LstPrinters_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int index = lstPrinters.IndexFromPoint(e.Location);
                if (index != ListBox.NoMatches)
                {
                    lstPrinters.SelectedIndex = index;
                }
            }
        }

        private void ApriPreferenze()
        {
            if (lstPrinters.SelectedItem == null) return;
            string printerName = lstPrinters.SelectedItem.ToString().Split('[')[0].Trim();
            Process.Start("rundll32.exe", $"printui.dll,PrintUIEntry /e /n \"{printerName}\"");
        }

        private void ApriProprietaStampante()
        {
            if (lstPrinters.SelectedItem == null) return;
            string printerName = lstPrinters.SelectedItem.ToString().Split('[')[0].Trim();
            Process.Start("rundll32.exe", $"printui.dll,PrintUIEntry /p /n \"{printerName}\"");
        }

        private void ApriCartellaSpooler()
        {
            Process.Start("explorer.exe", "C:\\Windows\\System32\\spool\\PRINTERS");
        }

        private void ImpostaComePredefinita()
        {
            if (lstPrinters.SelectedItem == null) return;
            string printerName = lstPrinters.SelectedItem.ToString().Split('[')[0].Trim();
            Process.Start("RUNDLL32.EXE", $"PRINTUI.DLL,PrintUIEntry /y /n \"{printerName}\"");
        }
    }
}
