# Application / Inventory

Use-cases for the **Inventory** bounded context.

## Commands / Queries (planned)

| Name | Type | Description |
|---|---|---|
| `ReserveStockCommand` | Command | Reserve units for a confirmed order |
| `ReleaseReservationCommand` | Command | Release units on cancellation |
| `GetStockLevelQuery` | Query | Current available + reserved units for a product |
| `ReplenishStockCommand` | Command | Admin: increase available stock |

## Event Handlers (planned)

| Handler | Listens to | Action |
|---|---|---|
| `OnOrderCreated` | `OrderCreated` | Attempt to reserve the required stock; publish `InventoryReserved` or `InsufficientStock` |
| `OnOrderCancelled` | `OrderCancelled` | Release previously reserved stock |

## Notes

Reservations are scoped to an `OrderId` so they can be cleanly released. The handler is idempotent — a duplicate `OrderCreated` event must not double-reserve.
