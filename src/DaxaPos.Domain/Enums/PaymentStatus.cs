namespace DaxaPos.Domain.Enums;

/// <summary>
/// Matches <c>docs/modules/payments.md</c>'s documented lifecycle exactly (PLAN-0005 Milestone B).
/// Cash and manual EFTPOS jump straight to <see cref="Recorded"/> — there is no integrated terminal
/// round-trip for either method, so the module doc's <c>SentToTerminal</c>/<c>AwaitingCustomer</c>
/// intermediate states are not modelled here; PLAN-0009's integrated-adapter work can extend this
/// enum when it actually needs those states (adding an enum value later needs no migration).
/// </summary>
public enum PaymentStatus
{
    Created = 0,
    Approved = 1,
    Declined = 2,
    Cancelled = 3,
    TimedOut = 4,
    Recorded = 5,
}
