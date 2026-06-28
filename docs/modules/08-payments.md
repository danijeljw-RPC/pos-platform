# Payments

Payments are a major module. The system should support manual and integrated payment workflows.

## Payment methods

| Payment type | Notes |
|---|---|
| Cash | Cash drawer support |
| External EFTPOS | Manual record only |
| Integrated EFTPOS | Tyro, Zeller, Square, Stripe Terminal, Windcave |
| Gift card | Internal gift card system |
| Store credit | Customer account balance |
| Voucher | Promo/marketing vouchers |
| Account/customer charge | Pay later / invoice |
| Deposit | Partial payment |
| Split payment | Multiple methods |
| Refund | Full/partial |
| Tips | US/CA-heavy |
| Surcharge | Card/Sunday/public holiday/service |

## Integrated EFTPOS wishlist

| Feature | Notes |
|---|---|
| Provider selection | Per venue/location |
| Terminal pairing | Per POS terminal |
| Send amount to terminal | No manual amount entry |
| Payment status polling | Pending/approved/declined/cancelled |
| Cancel payment | Staff cancels from POS |
| Refund payment | From original order |
| Partial refund | Refund selected items/amount |
| Reconciliation ID | Store provider payment ID |
| Settlement reporting | Match payouts later |
| Multiple terminals | Front counter 1, bar 2, etc. |
| Provider adapters | Tyro/Zeller/Square/Stripe/Windcave/etc. |

## Provider adapter interface concept

```csharp
public interface IPaymentTerminalProvider
{
    Task<PaymentTerminalResult> StartPaymentAsync(StartPaymentRequest request);
    Task<PaymentTerminalResult> RefundAsync(RefundPaymentRequest request);
    Task<PaymentTerminalStatus> GetTerminalStatusAsync(string terminalId);
    Task CancelPaymentAsync(string paymentRequestId);
}
```

## POS payment flow

```text
Staff presses Pay
↓
POS creates payment request
↓
Selected provider adapter sends amount to terminal
↓
Customer taps/inserts/swipes
↓
Provider returns approved/declined/cancelled
↓
POS records payment result
↓
Order closes or retries
```

## Provider roadmap

For AU/NZ first:

```text
1. Manual EFTPOS
2. Tyro
3. Zeller
4. Square
5. Stripe Terminal
6. Windcave
```

For international expansion:

```text
7. Adyen
8. Worldline
9. Global Payments
```
