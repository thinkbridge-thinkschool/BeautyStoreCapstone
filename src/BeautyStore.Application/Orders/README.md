# Application / Orders

Use-cases for the **Orders** bounded context.

## Commands / Queries (planned)

| Name | Type | Description |
|---|---|---|
| `PlaceOrderCommand` | Command | Customer places a new order; raises `OrderCreated` |
| `CancelOrderCommand` | Command | Customer or system cancels; raises `OrderCancelled` |
| `ConfirmOrderCommand` | Command | Internal — called after `InventoryReserved` received |
| `GetOrderByIdQuery` | Query | Fetch order details and current status |
| `ListOrdersByCustomerQuery` | Query | Order history for a customer |

## Event Handlers (planned)

| Handler | Listens to | Action |
|---|---|---|
| `OnInventoryReserved` | `InventoryReserved` | Advance order state to `Confirmed` |
| `OnPaymentSucceeded` | `PaymentSucceeded` | Advance order state to `AwaitingShipment` |
| `OnPaymentFailed` | `PaymentFailed` | Advance order state to `PaymentFailed`; trigger cancellation |
| `OnShipmentCreated` | `ShipmentCreated` | Advance order state to `Shipped` |
