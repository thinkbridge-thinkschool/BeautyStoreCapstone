# BeautyStore.Application

**Layer: Application**

Orchestrates use-cases for each bounded context. Contains Commands, Queries, and Application Services. Uses MediatR (or a simple command bus) to route requests.

## Structure

Each subfolder is one bounded context:

```
Application/
├── Catalog/      — browse products, search, pricing queries
├── Orders/       — place order, cancel order, get order status
├── Inventory/    — check stock, reserve units, release reservation
├── Payments/     — initiate charge, handle webhook, issue refund
└── Shipping/     — create shipment, get tracking status
```

## Responsibilities

- Command and Query DTOs (inputs / outputs).
- Application service / handler classes that call Domain aggregates.
- Publish domain events after aggregate state changes.
- Transaction boundaries (unit of work).

## Depends on

- `BeautyStore.Domain` (aggregates, repository interfaces, domain events)
- Does NOT depend on Infrastructure directly — only on interfaces defined in Domain.
