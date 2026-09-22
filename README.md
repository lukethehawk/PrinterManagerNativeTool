# Printer Manager

A fast native Windows toolbox for managing and diagnosing installed printers without navigating through the Windows 11 Settings app.

The v2 branch modernizes the original WinForms utility with WPF, .NET 10 and the native Windows Fluent theme while keeping the application small, portable and focused on technicians.

## Features

- Modern Windows 11-style WPF interface with automatic light/dark theme
- Fast list of local and connected printers
- Search by printer name, driver, port or network address
- Printer status, current jobs, port, driver and default-printer state
- Open print queue
- Open printing preferences
- Open printer properties
- Set the default printer
- Print a Windows test page
- Open the embedded Web UI when a Standard TCP/IP address can be detected
- Network diagnostics:
  - ICMP ping
  - RAW printing port 9100
- Copy a compact diagnostic report to the clipboard
- Spooler status
- Restart the Print Spooler with UAC elevation only when required
- Safely clear the spool directory by stopping and restarting the service
- Portable self-contained single-file build

## Architecture

The application deliberately avoids mixing Windows printer APIs with UI code.

- `Models/PrinterInfo.cs` - printer data model and status formatting
- `Native/PrinterNative.cs` - minimal `winspool.drv` interop
- `Services/PrinterService.cs` - enumeration and common printer actions
- `Services/NetworkDiagnosticsService.cs` - TCP/IP diagnostics
- `Services/SpoolerService.cs` - privileged spooler operations
- `MainWindow.xaml` - WPF/Fluent user interface

Printer discovery uses the native Windows spooler API instead of using WMI as a second source of truth.

## Requirements

### Development

- Windows 10 or Windows 11
- .NET 10 SDK
- Visual Studio 2026+ or another .NET 10-compatible IDE

### Running the published build

No .NET runtime installation is required for the default self-contained build.

## Build

```powershell
dotnet restore PrinterManagerNativeTool.sln
dotnet build PrinterManagerNativeTool.sln -c Release
```

## Portable publish

```powershell
dotnet publish .\PrinterManagerNativeTool\PrinterManagerNativeTool.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true
```

The generated executable is architecture-specific. Additional targets such as `win-arm64` can be published separately.

## UAC model

Printer Manager normally runs with standard user privileges.

Administrative elevation is requested only for operations that require it:

- Restart Print Spooler
- Clear spooler files

The elevated process performs the requested operation and then exits.

## Network detection

For Standard TCP/IP printer ports, Printer Manager reads the Windows print monitor configuration to determine the host/IP address. If the address is available, the UI can open the printer's Web interface and test ping and RAW port 9100.

WSD, vendor-specific and virtual ports may not expose a directly usable IP address and are therefore shown without network diagnostics.

## License

MIT.
