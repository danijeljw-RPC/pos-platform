# Payment Adapter Architecture

## Goal

Support multiple payment providers globally while keeping the POS flow consistent.

## Provider adapter model

```text
PaymentProvider
├─ TyroAdapter
├─ ZellerAdapter
├─ SquareAdapter
├─ StripeTerminalAdapter
├─ WindcaveAdapter
├─ AdyenAdapter
├─ WorldlineAdapter
└─ GlobalPaymentsAdapter
```

## Interface concept

```csharp
public interface IPaymentTerminalProvider
{
    Task<PaymentTerminalResult> StartPaymentAsync(StartPaymentRequest request);
    Task<PaymentTerminalResult> RefundAsync(RefundPaymentRequest request);
    Task<PaymentTerminalStatus> GetTerminalStatusAsync(string terminalId);
    Task CancelPaymentAsync(string paymentRequestId);
}
```

## Provider definition

```text
PaymentProviderDefinition
├─ Region availability
├─ Supported currencies
├─ Supported features
├─ Terminal pairing method
├─ Credential fields
├─ Refund capability
├─ Tip capability
├─ Surcharge capability
├─ Offline capability
├─ Webhook/result model
└─ Settlement/reporting capability
```

## Venue configuration

```text
Venue: Cake Shop
POS Terminal: Front Counter 1
Payment Provider: Zeller
EFTPOS Terminal: Zeller terminal BAR-01
```

## Payment request lifecycle

```text
Created
↓
SentToTerminal
↓
AwaitingCustomer
↓
Approved / Declined / Cancelled / TimedOut
↓
Recorded
↓
OrderClosed or PaymentRetry
```

## Data to capture

```text
Provider
ProviderPaymentId
ProviderCheckoutId
ProviderTerminalId
Amount
Currency
Status
OrderId
TerminalId
StaffUserId
IdempotencyKey
ApprovedAt
DeclinedReason
ReceiptUrl
Raw provider response reference
```
