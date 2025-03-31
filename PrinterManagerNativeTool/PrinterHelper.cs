using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Management;

namespace PrinterManagerNativeTool
{
    public class PrinterInfo
    {
        public string PrinterName { get; set; } = string.Empty;
        public string PortName { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public uint RawStatus { get; set; }
        public bool IsOffline { get; set; }
        public int Availability { get; set; }
    }

    public static class PrinterHelper
    {
        public static List<PrinterInfo> EnumPrintersMultiThread()
        {
            var basicList = EnumAllPrinters();
            var result = new ConcurrentBag<PrinterInfo>();

            Parallel.ForEach(basicList, (printerStruct) =>
            {
                var details = GetPrinterDetails(printerStruct.pPrinterName);
                result.Add(details);
            });

            // 🔄 Secondo passaggio: aggiorna Availability e WorkOffline via WMI
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name, Availability, WorkOffline FROM Win32_Printer");
                foreach (ManagementObject printer in searcher.Get())
                {
                    string name = printer["Name"]?.ToString() ?? "";
                    var match = result.FirstOrDefault(p => p.PrinterName.Equals(name, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        if (printer["Availability"] != null)
                            match.Availability = Convert.ToInt32(printer["Availability"]);

                        if (printer["WorkOffline"] != null && Convert.ToBoolean(printer["WorkOffline"]))
                            match.IsOffline = true;
                    }
                }
            }
            catch
            {
                // Silenzia eventuali errori WMI
            }

            return new List<PrinterInfo>(result);
        }


        private static List<PRINTER_INFO_2> EnumAllPrinters()
        {
            var printers = new List<PRINTER_INFO_2>();

            uint flags = (uint)(PrinterEnumFlags.PRINTER_ENUM_LOCAL | PrinterEnumFlags.PRINTER_ENUM_CONNECTIONS);
            uint level = 2;
            uint pcbNeeded = 0;
            uint pcReturned = 0;

            PrinterNative.EnumPrinters(flags, null, level, IntPtr.Zero, 0, out pcbNeeded, out pcReturned);

            if (pcbNeeded == 0) return printers;

            IntPtr pAddr = Marshal.AllocHGlobal((int)pcbNeeded);
            try
            {
                bool ok = PrinterNative.EnumPrinters(flags, null, level, pAddr, pcbNeeded, out pcbNeeded, out pcReturned);
                if (!ok) return printers;

                int sizeStruct = Marshal.SizeOf(typeof(PRINTER_INFO_2));
                IntPtr currentPtr = pAddr;

                for (int i = 0; i < pcReturned; i++)
                {
                    PRINTER_INFO_2 info = (PRINTER_INFO_2)Marshal.PtrToStructure(currentPtr, typeof(PRINTER_INFO_2))!;
                    printers.Add(info);
                    currentPtr = IntPtr.Add(currentPtr, sizeStruct);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pAddr);
            }

            return printers;
        }
        private static bool PortExists(string portName)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_SerialPort");
                foreach (ManagementObject port in searcher.Get())
                {
                    string name = port["Name"]?.ToString() ?? "";
                    if (name.Contains(portName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch
            {
                // Logga se necessario
            }

            return false;
        }

        private static PrinterInfo GetPrinterDetails(string printerName)
        {
            var pi = new PrinterInfo
            {
                PrinterName = printerName,
                PortName = string.Empty,
                DriverName = string.Empty,
                RawStatus = 0,
                IsOffline = false
            };
           
            if (!PrinterNative.OpenPrinter(printerName, out IntPtr hPrinter, IntPtr.Zero) || hPrinter == IntPtr.Zero)
            {
                return pi;
            }

            try
            {
                uint level = 2;
                PrinterNative.GetPrinter(hPrinter, level, IntPtr.Zero, 0, out uint cbNeeded);
                if (cbNeeded == 0)
                {
                    return pi;
                }

                IntPtr pAddr = Marshal.AllocHGlobal((int)cbNeeded);
                try
                {
                    bool ok = PrinterNative.GetPrinter(hPrinter, level, pAddr, cbNeeded, out _);
                    if (!ok) return pi;

                    var info2 = (PRINTER_INFO_2_DETAILED)Marshal.PtrToStructure(pAddr, typeof(PRINTER_INFO_2_DETAILED))!;
                    pi.PortName = info2.pPortName;
                    pi.DriverName = info2.pDriverName;
                    pi.RawStatus = info2.Status;
                    pi.IsOffline =
                        (info2.Status & (uint)PrinterStatus.PRINTER_STATUS_OFFLINE) != 0 ||
                        (info2.Attributes & 0x400) != 0; // PRINTER_ATTRIBUTE_WORK_OFFLINE

                    // ✅ Verifica anche se la porta USB è presente
                    if (pi.PortName.StartsWith("USB", StringComparison.OrdinalIgnoreCase))
                    {
                        bool usbPortActive = PortExists(pi.PortName);
                        if (!usbPortActive)
                        {
                            pi.IsOffline = true;
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pAddr);
                }
            }
            finally
            {
                PrinterNative.ClosePrinter(hPrinter);
            }

            return pi;
        }

        [Flags]
        public enum PrinterStatus : uint
        {
            PRINTER_STATUS_PAUSED = 0x00000001,
            PRINTER_STATUS_ERROR = 0x00000002,
            PRINTER_STATUS_PENDING_DELETION = 0x00000004,
            PRINTER_STATUS_PAPER_JAM = 0x00000008,
            PRINTER_STATUS_PAPER_OUT = 0x00000010,
            PRINTER_STATUS_MANUAL_FEED = 0x00000020,
            PRINTER_STATUS_PAPER_PROBLEM = 0x00000040,
            PRINTER_STATUS_OFFLINE = 0x00000080,
            PRINTER_STATUS_IO_ACTIVE = 0x00000100,
            PRINTER_STATUS_BUSY = 0x00000200,
            PRINTER_STATUS_PRINTING = 0x00000400,
            PRINTER_STATUS_OUTPUT_BIN_FULL = 0x00000800,
            PRINTER_STATUS_NOT_AVAILABLE = 0x00001000,
            PRINTER_STATUS_WAITING = 0x00002000,
            PRINTER_STATUS_PROCESSING = 0x00004000,
            PRINTER_STATUS_INITIALIZING = 0x00008000,
            PRINTER_STATUS_WARMING_UP = 0x00010000,
            PRINTER_STATUS_TONER_LOW = 0x00020000,
            PRINTER_STATUS_NO_TONER = 0x00040000,
            PRINTER_STATUS_PAGE_PUNT = 0x00080000,
            PRINTER_STATUS_USER_INTERVENTION = 0x00100000,
            PRINTER_STATUS_OUT_OF_MEMORY = 0x00200000,
            PRINTER_STATUS_DOOR_OPEN = 0x00400000,
            PRINTER_STATUS_SERVER_UNKNOWN = 0x00800000,
            PRINTER_STATUS_POWER_SAVE = 0x01000000
        }

    }
}