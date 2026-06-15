# Application / Payments

Use-cases for the **Payments** bounded context.

## Commands / Queries (planned)

| Name | Type | Description |
|---|---|---|
| `InitiatePaymentCommand` | Command | Create a payment intent via the gateway |
| `HandlePaymentWebhookCommand` | Command | Process gateway callback; publish `PaymentSucceeded` or `PaymentFailed` |
| `IssueRefundCommand` | Command | Reverse a captured payment on cancellation |
| `GetPaymentStatusQuery` | Query | Current status of a payment for an order |

## Event Handlers (planned)

| Handler | Listens to | Action |
|---|---|---|
| `OnInventoryReserved` | `InventoryReserved` | Initiate payment collection |
| `OnOrderCancelled` | `OrderCancelled` | Trigger refund if payment was captured |

## Notes

Payment gateway communication happens through the `IPaymentGateway` port defined here; the actual HTTP client lives in `BeautyStore.Infrastructure.Payments`.
