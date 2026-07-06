using System.Net.Sockets;
using Microsoft.Extensions.Options;

namespace DaxaPos.Infrastructure.Printing;

/// <summary>
/// Sends already-generated ESC/POS bytes to a network receipt printer over raw TCP (the standard
/// ESC/POS-over-network transport most thermal printers listen on, conventionally port 9100).
/// Deliberately minimal (approved Human Decision #1's scope boundary): one configured endpoint, no
/// discovery, no connection pooling, no printer-model-specific handshake — a new TCP connection per
/// print job, closed immediately after. Throws on any transport failure; the caller
/// (<c>DaxaPos.Workers</c>' outbox processor) is responsible for retry via the outbox retry policy,
/// this class never retries internally.
/// </summary>
public sealed class NetworkPrinterTransport(IOptions<NetworkPrinterOptions> options) : IPrinterTransport
{
    public async Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        var target = options.Value;

        using var client = new TcpClient();
        await client.ConnectAsync(target.Host, target.Port, cancellationToken);

        await using var stream = client.GetStream();
        await stream.WriteAsync(data, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }
}
