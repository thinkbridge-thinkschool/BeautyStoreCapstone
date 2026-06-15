# Infrastructure / Persistence

EF Core data access layer.

## Contents (planned)

| File | Purpose |
|---|---|
| `BeautyStoreDbContext.cs` | Single `DbContext` for the whole modular monolith |
| `OrderRepository.cs` | Implements `IOrderRepository` from Domain |
| `ProductRepository.cs` | Implements `IProductRepository` from Domain |
| `StockRepository.cs` | Implements `IStockRepository` from Domain |
| `PaymentRepository.cs` | Implements `IPaymentRepository` from Domain |
| `ShipmentRepository.cs` | Implements `IShipmentRepository` from Domain |
| `OutboxMessage.cs` | EF entity for the Transactional Outbox table |
| `Migrations/` | EF Core migration files |

## Schema naming convention

Each module uses a table prefix to avoid coupling:

| Module | Table prefix |
|---|---|
| Catalog | `catalog_` |
| Orders | `orders_` |
| Inventory | `inventory_` |
| Payments | `payments_` |
| Shipping | `shipping_` |
| Outbox | `outbox_` |

This enforces module isolation at the database level — no cross-module joins in ORM queries.
