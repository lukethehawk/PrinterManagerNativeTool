using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace PrinterManagerNativeTool.Services;

public sealed record NetworkDiagnosticResult(
    string Ping,
    string Raw9100,
    long? PingMilliseconds);

public sealed class NetworkDiagnosticsService
{
    public async Task<NetworkDiagnosticResult> DiagnoseAsync(string host, CancellationToken cancellationToken = default)
    {
        var pingText = "Non disponibile";
        long? pingMs = null;

        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, TimeSpan.FromSeconds(2), Array.Empty<byte>(), new PingOptions(), cancellationToken);
            if (reply.Status == IPStatus.Success)
            {
                pingMs = reply.RoundtripTime;
                pingText = $"OK ({reply.RoundtripTime} ms)";
            }
            else
            {
                pingText = reply.Status.ToString();
            }
        }
        catch (Exception ex)
        {
            pingText = $"Errore: {ex.Message}";
        }

        var rawText = "Non disponibile";
        try
        {
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(host, 9100, cancellationToken).AsTask().WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
            rawText = "OK";
        }
        catch (Exception ex)
        {
            rawText = $"Non raggiungibile ({ex.GetType().Name})";
        }

        return new NetworkDiagnosticResult(pingText, rawText, pingMs);
    }
}
