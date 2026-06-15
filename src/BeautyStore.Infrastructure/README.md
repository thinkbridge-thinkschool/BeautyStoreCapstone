# BeautyStore.Infrastructure

**Layer: Infrastructure**

Implements the interfaces declared in Domain and Application. All external I/O lives here.

## Structure

```
Infrastructure/
├── Persistence/   — EF Core DbContext, repository implementations, migrations
├── Messaging/     — Azure Service Bus publisher and consumer wiring, outbox relay
├── Payments/      — Payment gateway adapter (e.g., Razorpay / Stripe)
└── Shipping/      — Shipping provider adapter (e.g., Shiprocket / Delhivery)
```

## Responsibilities

- EF Core `BeautyStoreDbContext` with schema-per-module table naming.
- Concrete `IOrderRepository`, `IProductRepository`, etc. backed by EF Core.
- Outbox table and `OutboxRelayWorker` (transactional outbox pattern — see Day 20).
- Azure Service Bus `IMessagePublisher` and `IMessageConsumer` wrappers.
- Third-party payment gateway HTTP client.
- Shipping provider HTTP client.

## Depends on

- `BeautyStore.Domain` (implements repository interfaces)
- `BeautyStore.Application` (implements application ports where needed)
- External packages: EF Core, Azure.Messaging.ServiceBus, HttpClient adapters
