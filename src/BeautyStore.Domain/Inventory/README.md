# Domain / Inventory

Pure domain model for the **Inventory** bounded context.

## Types (planned)

| Type | Kind | Notes |
|---|---|---|
| `StockItem` | Aggregate Root | ProductId, AvailableUnits, ReservedUnits |
| `Reservation` | Entity (inside aggregate) | OrderId, Quantity, ExpiresAt |
| `StockLevel` | Value Object | AvailableUnits + ReservedUnits snapshot |
| `IStockRepository` | Repository interface | Implemented in Infrastructure |

## Domain Events raised

| Event | When |
|---|---|
| `InventoryReserved` | `StockItem.Reserve(orderId, qty)` succeeds |
| `InsufficientStock` | `StockItem.Reserve(...)` fails — not enough available units |

## Invariants

- `AvailableUnits` must never go below zero after a reservation.
- A reservation for the same `OrderId` must not be created twice (idempotency enforced at domain level).
- Releasing a reservation that does not exist is a no-op (safe to replay).
