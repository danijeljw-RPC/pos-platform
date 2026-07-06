namespace DaxaPos.Infrastructure.Printing;

/// <summary>
/// The printer transport boundary (PLAN-0005 Milestone E, approved Human Decision #1) — the only
/// seam between generated ESC/POS bytes (<c>DaxaPos.Application.Printing.EscPosReceiptFormatter</c>,
/// pure and print-transport-agnostic) and however those bytes actually reach a physical printer.
/// Network is the only concrete implementation this milestone ships
/// (<see cref="NetworkPrinterTransport"/>) — USB printing is a Windows POS terminal (MAUI) device
/// capability, not this backend service's concern, per CLAUDE.md's device strategy.
/// </summary>
public interface IPrinterTransport
{
    Task SendAsync(byte[] data, CancellationToken cancellationToken = default);
}
