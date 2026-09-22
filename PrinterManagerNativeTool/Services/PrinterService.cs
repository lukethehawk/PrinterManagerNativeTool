using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using PrinterManagerNativeTool.Models;
using PrinterManagerNativeTool.Native;

namespace PrinterManagerNativeTool.Services;

public sealed class PrinterService
{
    private const uint Level = 2;

    public IReadOnlyList<PrinterInfo> GetPrinters()
    {
        var defaultPrinter = GetDefaultPrinterName();

        return EnumeratePrinters(defaultPrinter)
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public void SetDefault(string printerName)
    {
        if (!PrinterNative.SetDefaultPrinter(printerName))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Impossibile impostare la stampante predefinita.");
    }

    public void OpenQueue(string printerName) => RunPrintUi($"/o /n \"{printerName}\"");
    public void OpenPreferences(string printerName) => RunPrintUi($"/e /n \"{printerName}\"");
    public void OpenProperties(string printerName) => RunPrintUi($"/p /n \"{printerName}\"");
    public void PrintTestPage(string printerName) => RunPrintUi($"/k /n \"{printerName}\"");

    public void OpenWebInterface(PrinterInfo printer)
    {
        if (string.IsNullOrWhiteSpace(printer.NetworkHost))
            throw new InvalidOperationException("Non è stato rilevato un indirizzo di rete per questa stampante.");

        Process.Start(new ProcessStartInfo
        {
            FileName = $"http://{printer.NetworkHost}",
            UseShellExecute = true
        });
    }

    private static void RunPrintUi(string arguments)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "rundll32.exe",
            Arguments = $"printui.dll,PrintUIEntry {arguments}",
            UseShellExecute = true
        });
    }

    private static List<PrinterInfo> EnumeratePrinters(string? defaultPrinter)
    {
        var flags = PrinterEnumFlags.Local | PrinterEnumFlags.Connections;

        PrinterNative.EnumPrinters(flags, null, Level, IntPtr.Zero, 0, out var bytesNeeded, out _);
        if (bytesNeeded == 0)
            return [];

        var buffer = Marshal.AllocHGlobal(checked((int)bytesNeeded));
        try
        {
            if (!PrinterNative.EnumPrinters(flags, null, Level, buffer, bytesNeeded, out _, out var returned))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Impossibile enumerare le stampanti installate.");

            var result = new List<PrinterInfo>(checked((int)returned));
            var itemSize = Marshal.SizeOf<PrinterInfo2Native>();

            for (var i = 0; i < returned; i++)
            {
                var ptr = IntPtr.Add(buffer, checked((int)i * itemSize));
                var native = Marshal.PtrToStructure<PrinterInfo2Native>(ptr);

                // Important: all strings referenced by PRINTER_INFO_2 point inside
                // the EnumPrinters buffer. Copy them to managed strings before
                // freeing the buffer, otherwise subsequent reads can return
                // corrupted/empty printer names and ports.
                var name = PrinterNative.PtrToString(native.pPrinterName);
                var portName = PrinterNative.PtrToString(native.pPortName);

                result.Add(new PrinterInfo
                {
                    Name = name,
                    PortName = portName,
                    DriverName = PrinterNative.PtrToString(native.pDriverName),
                    ServerName = NullIfEmpty(PrinterNative.PtrToString(native.pServerName)),
                    ShareName = NullIfEmpty(PrinterNative.PtrToString(native.pShareName)),
                    Location = NullIfEmpty(PrinterNative.PtrToString(native.pLocation)),
                    Comment = NullIfEmpty(PrinterNative.PtrToString(native.pComment)),
                    StatusFlags = native.Status,
                    Attributes = native.Attributes,
                    JobCount = native.cJobs,
                    IsDefault = string.Equals(name, defaultPrinter, StringComparison.OrdinalIgnoreCase),
                    NetworkHost = ResolveNetworkHost(portName)
                });
            }

            return result;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string? GetDefaultPrinterName()
    {
        uint chars = 0;
        PrinterNative.GetDefaultPrinter(null, ref chars);
        if (chars == 0)
            return null;

        var buffer = new StringBuilder(checked((int)chars));
        return PrinterNative.GetDefaultPrinter(buffer, ref chars) ? buffer.ToString() : null;
    }

    private static string? ResolveNetworkHost(string portName)
    {
        if (string.IsNullOrWhiteSpace(portName))
            return null;

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Control\Print\Monitors\Standard TCP/IP Port\Ports\{portName}");

            var ip = key?.GetValue("IPAddress")?.ToString();
            var host = key?.GetValue("HostName")?.ToString();

            if (!string.IsNullOrWhiteSpace(ip))
                return ip;

            if (!string.IsNullOrWhiteSpace(host))
                return host;
        }
        catch
        {
            // Registry metadata is optional. Fall back to common port naming below.
        }

        if (IPAddress.TryParse(portName, out _))
            return portName;

        if (portName.StartsWith("IP_", StringComparison.OrdinalIgnoreCase) ||
            portName.StartsWith("EP_", StringComparison.OrdinalIgnoreCase))
        {
            var candidate = portName[3..];
            if (IPAddress.TryParse(candidate, out _) || candidate.Contains('.'))
                return candidate;
        }

        return null;
    }

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
