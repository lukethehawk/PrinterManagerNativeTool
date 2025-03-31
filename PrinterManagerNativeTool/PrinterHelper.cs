using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PrinterManagerNativeTool
{
    public class PrinterInfo
    {
        public string PrinterName { get; set; }
        public string PortName { get; set; }
        public string DriverName { get; set; }
        public uint RawStatus { get; set; } // or a custom enum if you want
    }

    public static class PrinterHelper
    {
        public static List<PrinterInfo> EnumPrintersMultiThread()
        {
            // 1) Elenco base di stampanti
            var basicList = EnumAllPrinters();

            // 2) Query dettagli in parallelo
            var result = new ConcurrentBag<PrinterInfo>();

            Parallel.ForEach(basicList, (printerStruct) =>
            {
                var details = GetPrinterDetails(printerStruct.pPrinterName);
                result.Add(details);
            });

            return new List<PrinterInfo>(result);
        }

        private static List<PRINTER_INFO_2> EnumAllPrinters()
        {
            var printers = new List<PRINTER_INFO_2>();

            uint flags = (uint)(PrinterEnumFlags.PRINTER_ENUM_LOCAL | PrinterEnumFlags.PRINTER_ENUM_CONNECTIONS);
            uint level = 2; // PRINTER_INFO_2
            uint pcbNeeded = 0;
            uint pcReturned = 0;

            // 1) Prima chiamata: dimensione buffer
            PrinterNative.EnumPrinters(flags, null, level, IntPtr.Zero, 0, out pcbNeeded, out pcReturned);

            if (pcbNeeded == 0) return printers;

            IntPtr pAddr = Marshal.AllocHGlobal((int)pcbNeeded);
            try
            {
                // 2) Richiamo con buffer allocato
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

        private static PrinterInfo GetPrinterDetails(string printerName)
        {
            var pi = new PrinterInfo
            {
                PrinterName = printerName,
                PortName = "",
                DriverName = "",
                RawStatus = 0
            };

            if (!PrinterNative.OpenPrinter(printerName, out IntPtr hPrinter, IntPtr.Zero) || hPrinter == IntPtr.Zero)
            {
                return pi; // non aperto
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
    }
}
