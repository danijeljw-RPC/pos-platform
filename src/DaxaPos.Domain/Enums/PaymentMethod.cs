namespace DaxaPos.Domain.Enums;

/// <summary>
/// <see cref="Integrated"/> is defined now (PLAN-0005 Milestone B) but rejected at the endpoint
/// layer — no <c>IPaymentTerminalProvider</c> adapter exists yet to actually call a terminal;
/// PLAN-0009 implements the first concrete adapter and the route that accepts this method.
/// <c>GiftCard</c>/<c>StoreCredit</c> are out of this plan's scope entirely (Non-goals).
/// </summary>
public enum PaymentMethod
{
    Cash = 0,
    ManualEftpos = 1,
    Integrated = 2,
}
