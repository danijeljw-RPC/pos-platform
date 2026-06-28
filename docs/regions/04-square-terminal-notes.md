# Square Terminal Notes

## Requirement

The POS must control the payment flow.

The staff should not manually type amounts into the Square terminal.

## Correct Square product

Use:

```text
Square Terminal API
```

Do not use the Square POS app as the main POS.

## Model

```text
Your POS
↓
Creates order
↓
Calculates total
↓
Sends checkout request to Square Terminal API
↓
Square Terminal displays amount
↓
Customer taps/inserts/swipes
↓
Square returns payment result
↓
Your POS closes the order
```

## Merchant requirements

The business still needs:

```text
Square merchant account
Square Terminal hardware
Square account connected to your app
Terminal paired to your POS/location
```

But the business does not need to use Square POS for:

```text
POS order entry
menus
tables
staff workflows
modifiers
KDS
inventory
venue reporting
loyalty
gift cards
customer display
```

Your system remains the POS.

Square remains the payment terminal/acquirer.

## Payment flow

```text
Staff presses Pay in your POS

Your backend creates Square Terminal checkout:
- amount
- currency
- order/reference ID
- terminal device ID
- idempotency key

Square sends request to the terminal

Terminal shows:
$42.50
Tap / insert / swipe

Customer pays

Square returns:
APPROVED / DECLINED / CANCELED

Your POS records:
- payment ID
- Square checkout ID
- amount
- status
- card brand / masked info if provided
- receipt URL if provided
```

## Refund flow

```text
Staff opens past order in your POS
↓
Staff chooses refund
↓
Your POS calls Square Refunds API using Square payment ID
↓
Square processes refund
↓
Your POS records refund against original order
```

Some refunds may require card-present refund on the terminal depending on provider/card rules.
