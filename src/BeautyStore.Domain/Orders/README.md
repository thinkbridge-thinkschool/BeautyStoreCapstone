# Domain / Orders

Pure domain model for the **Orders** bounded context. This is the **core aggregate** of the system.

## Types (planned)

| Type | Kind | Notes |
|---|---|---|
| `Order` | Aggregate Root | The central aggregate; controls all state transitions |
| `OrderLine` | Entity (inside aggregate) | ProductId, Quantity, UnitPrice |
| `OrderId` | Value Object | Strongly typed GUID |
| `OrderStatus` | Enum | Pending → Confirmed → AwaitingShipment → Shipped → Delivered / Cancelled |
| `Money` | Value Object | Shared across contexts; Amount + Currency |
| `Address` | Value Object | Shipping destination |
| `IOrderRepository` | Repository interface | Implemented in Infrastructure |

## Domain Events raised by `Order`

| Event | When |
|---|---|
| `OrderCreated` | `Order.Place(...)` called |
| `OrderCancelled` | `Order.Cancel()` called |

## Invariants

- An order cannot be cancelled once it has been shipped.
- An order must contain at least one `OrderLine`.
- Total value is derived from lines — never stored separately.
- State transitions are only allowed in the defined sequence; invalid transitions throw `InvalidOrderStateException`.
