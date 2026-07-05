namespace DaxaPos.Application.Payments;

/// <summary>
/// Minimal placeholder request/result shapes for <see cref="IPaymentTerminalProvider"/> (PLAN-0005
/// Milestone B) — deliberately not fleshed out with provider-specific fields (cardholder data,
/// receipt line items, tip prompts, etc.). PLAN-0009 designs the real field set against its first
/// concrete adapter (Stripe Terminal); guessing that shape now, with no adapter to validate it
/// against, would very likely be wrong and would need reworking anyway.
/// </summary>
public sealed record StartPaymentRequest(Guid PaymentId, decimal Amount, string TerminalId);

public sealed record RefundPaymentRequest(Guid PaymentId, decimal Amount, string ProviderReference);

public sealed record PaymentTerminalResult(bool IsSuccess, string? ProviderReference, string? FailureReason);

public sealed record PaymentTerminalStatus(bool IsOnline, string? StatusDescription);

/// <summary>
/// Provider-agnostic payment terminal adapter interface (PLAN-0005 Milestone B, ADR-0005's
/// conceptual interface, reproduced verbatim). Interface only — no concrete adapter and no DI
/// registration in this milestone, since there is nothing yet to register: ADR-0005's "adapter
/// resolves at runtime based on configuration" describes PLAN-0009's job (the first concrete
/// adapter, Stripe Terminal), not something this milestone can wire with zero implementations. No
/// endpoint in this milestone calls this interface — cash and manual EFTPOS never reach a terminal
/// (approved Human Decision #1's no-hardware-coupling boundary: this interface is the only
/// "adapter-shaped" surface Milestone B introduces, and it is never invoked here).
/// </summary>
public interface IPaymentTerminalProvider
{
    Task<PaymentTerminalResult> StartPaymentAsync(StartPaymentRequest request);

    Task<PaymentTerminalResult> RefundAsync(RefundPaymentRequest request);

    Task<PaymentTerminalStatus> GetTerminalStatusAsync(string terminalId);

    Task CancelPaymentAsync(string paymentRequestId);
}
