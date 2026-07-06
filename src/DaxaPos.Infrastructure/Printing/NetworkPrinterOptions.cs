namespace DaxaPos.Infrastructure.Printing;

/// <summary>
/// Configuration for <see cref="NetworkPrinterTransport"/> — a single configured receipt-printer
/// endpoint (<c>Printing:ReceiptPrinter:Host</c>/<c>Port</c>). Per-location/per-terminal printer
/// routing (i.e. picking a different printer per order) is not built in this milestone — see
/// PLAN-0005's Milestone E kickoff scope note; that is a device-routing/orchestration concern
/// deferred to a later plan, not "printer discovery."
/// </summary>
public sealed class NetworkPrinterOptions
{
    public const string SectionName = "Printing:ReceiptPrinter";

    /// <summary>Standard ESC/POS-over-raw-TCP port used by most network thermal receipt printers.</summary>
    public const int DefaultPort = 9100;

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = DefaultPort;
}
