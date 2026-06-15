# Domain / Payments

Pure domain model for the **Payments** bounded context.

## Types (planned)

| Type | Kind | Notes |
|---|---|---|
| `Payment` | Aggregate Root | OrderId, Amount, Status, GatewayReference |
| `PaymentId` | Value Object | Strongly typed GUID |
| `PaymentStatus` | Enum | Pending → Captured / Failed / Refunded |
| `Money` | Value Object | Shared value object; Amount + Currency |
| `IPaymentRepository` | Repository interface | Implemented in Infrastructure |
| `IPaymentGateway` | Port (interface) | Abstracts Razorpay / Stripe; implemented in Infrastructure |

## Domain Events raised

| Event | When |
|---|---|
| `PaymentSucceeded` | Gateway confirms capture |
| `PaymentFailed` | Gateway returns failure or timeout |

## Invariants

- A payment can only be refunded if its status is `Captured`.
- Refund amount must not exceed the originally captured amount.
- A payment is tied 1:1 to an `OrderId`.
