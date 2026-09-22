namespace PrinterManagerNativeTool.Models;

public sealed class PrinterInfo
{
    public string Name { get; init; } = string.Empty;
    public string PortName { get; init; } = string.Empty;
    public string DriverName { get; init; } = string.Empty;
    public string? ServerName { get; init; }
    public string? ShareName { get; init; }
    public string? Location { get; init; }
    public string? Comment { get; init; }
    public uint StatusFlags { get; init; }
    public uint Attributes { get; init; }
    public uint JobCount { get; init; }
    public bool IsDefault { get; init; }
    public string? NetworkHost { get; init; }

    public string StatusText => PrinterStatusFormatter.ToDisplayText(StatusFlags);
    public string DefaultText => IsDefault ? "★" : string.Empty;
    public bool IsNetworkPrinter => !string.IsNullOrWhiteSpace(NetworkHost);
}

internal static class PrinterStatusFormatter
{
    private const uint Paused = 0x00000001;
    private const uint Error = 0x00000002;
    private const uint PaperJam = 0x00000008;
    private const uint PaperOut = 0x00000010;
    private const uint Offline = 0x00000080;
    private const uint Busy = 0x00000200;
    private const uint Printing = 0x00000400;
    private const uint UserIntervention = 0x00100000;
    private const uint DoorOpen = 0x00400000;

    public static string ToDisplayText(uint status)
    {
        if ((status & Offline) != 0) return "Offline";
        if ((status & PaperJam) != 0) return "Carta inceppata";
        if ((status & PaperOut) != 0) return "Carta esaurita";
        if ((status & DoorOpen) != 0) return "Sportello aperto";
        if ((status & UserIntervention) != 0) return "Intervento richiesto";
        if ((status & Error) != 0) return "Errore";
        if ((status & Paused) != 0) return "In pausa";
        if ((status & Printing) != 0) return "Stampa";
        if ((status & Busy) != 0) return "Occupata";
        return "Pronta";
    }
}
