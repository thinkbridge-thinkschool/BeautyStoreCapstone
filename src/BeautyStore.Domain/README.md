# BeautyStore.Domain

**Layer: Domain**

The innermost layer. Contains the pure business model — no frameworks, no databases, no HTTP. This layer has **zero external dependencies** (no NuGet packages beyond primitives).

## Structure

```
Domain/
├── Catalog/     — Product, Category, Price value object
├── Orders/      — Order aggregate, OrderLine, OrderId, OrderStatus
├── Inventory/   — StockItem, Reservation, StockLevel
├── Payments/    — Payment, PaymentStatus, Money value object
└── Shipping/    — Shipment, TrackingNumber, Address value object
```

## Contents per bounded context

| Type | Examples |
|---|---|
| Aggregate Root | `Order`, `Product`, `StockItem` |
| Value Objects | `Money`, `Address`, `ProductId`, `Quantity` |
| Domain Events | `OrderCreated`, `PaymentSucceeded`, `InventoryReserved` |
| Repository interfaces | `IOrderRepository`, `IProductRepository` |
| Domain exceptions | `InsufficientStockException`, `InvalidOrderStateException` |

## Rules

- No `using` statements for EF Core, ASP.NET, or any external library.
- All business invariants enforced inside aggregate methods — not in services.
- Domain events raised inside aggregates and collected by the Application layer.
