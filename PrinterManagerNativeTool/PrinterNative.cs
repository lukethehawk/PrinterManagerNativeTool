using System.Runtime.InteropServices;
using System.Text;

namespace PrinterManagerNativeTool.Native;

[Flags]
internal enum PrinterEnumFlags : uint
{
    Local = 0x00000002,
    Connections = 0x00000004
}

[StructLayout(LayoutKind.Sequential)]
internal struct PrinterInfo2Native
{
    public IntPtr pServerName;
    public IntPtr pPrinterName;
    public IntPtr pShareName;
    public IntPtr pPortName;
    public IntPtr pDriverName;
    public IntPtr pComment;
    public IntPtr pLocation;
    public IntPtr pDevMode;
    public IntPtr pSepFile;
    public IntPtr pPrintProcessor;
    public IntPtr pDatatype;
    public IntPtr pParameters;
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

internal static class PrinterNative
{
    [DllImport("winspool.drv", EntryPoint = "EnumPrintersW", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool EnumPrinters(
        PrinterEnumFlags flags,
        string? name,
        uint level,
        IntPtr printerEnum,
        uint bufferSize,
        out uint bytesNeeded,
        out uint returned);

    [DllImport("winspool.drv", EntryPoint = "GetDefaultPrinterW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetDefaultPrinter(StringBuilder? buffer, ref uint bufferChars);

    [DllImport("winspool.drv", EntryPoint = "SetDefaultPrinterW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetDefaultPrinter(string printerName);

    internal static string PtrToString(IntPtr value) =>
        value == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni(value) ?? string.Empty;
}
