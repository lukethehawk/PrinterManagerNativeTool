using System;
using System.Runtime.InteropServices;

namespace PrinterManagerNativeTool
{
    [Flags]
    public enum PrinterEnumFlags : uint
    {
        PRINTER_ENUM_LOCAL = 0x00000002,
        PRINTER_ENUM_CONNECTIONS = 0x00000004
        // Puoi aggiungere altri flag se vuoi enumerare stampanti di rete remote, ecc.
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct PRINTER_INFO_2
    {
        public string pServerName;
        public string pPrinterName;
        public string pShareName;
        public string pPortName;
        public string pDriverName;
        public string pComment;
        public string pLocation;
        public IntPtr pDevMode;
        public string pSepFile;
        public string pPrintProcessor;
        public string pDatatype;
        public string pParameters;
        public IntPtr pSecurityDescriptor;
        public uint Attributes;
        public uint Priority;
        public uint DefaultPriority;
        public uint StartTime;
        public uint UntilTime;
        public uint Status;
        public uint cJobs;
        public uint AveragePPM;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct PRINTER_INFO_2_DETAILED
    {
        public string pServerName;
        public string pPrinterName;
        public string pShareName;
        public string pPortName;
        public string pDriverName;
        public string pComment;
        public string pLocation;
        public IntPtr pDevMode;
        public string pSepFile;
        public string pPrintProcessor;
        public string pDatatype;
        public string pParameters;
        public IntPtr pSecurityDescriptor;
        public uint Attributes;
        public uint Priority;
        public uint DefaultPriority;
        public uint StartTime;
        public uint UntilTime;
        public uint Status;
        public uint cJobs;
        public uint AveragePPM;
    }

    public static class PrinterNative
    {
        // EnumPrinters
        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool EnumPrinters(
            uint Flags,
            string Name,
            uint Level,
            IntPtr pPrinterEnum,
            uint cbBuf,
            out uint pcbNeeded,
            out uint pcReturned
        );

        // GetPrinter
        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GetPrinter(
            IntPtr hPrinter,
            uint Level,
            IntPtr pPrinter,
            uint cbBuf,
            out uint pcbNeeded
        );

        // OpenPrinter / ClosePrinter
        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool OpenPrinter(
            string pPrinterName,
            out IntPtr phPrinter,
            IntPtr pDefault
        );

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool ClosePrinter(
            IntPtr hPrinter
        );
    }
}
