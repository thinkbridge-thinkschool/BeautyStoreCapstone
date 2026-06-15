# Capstone Design Document — BeautyStore

> ThinkSchool Day 23 — Capstone Kickoff

---

## Product Slice

**Nykaa-style Beauty & Skincare Marketplace**

A customer can browse products, add items to a cart, place an order, make a payment, and receive shipment updates. Stock is reserved atomically during checkout. Cancellations trigger compensating flows to release stock and reverse payments.

---

## Bounded Contexts

| Context | Responsibility |
|---|---|
| **Catalog** | Product listings, categories, pricing, search |
| **Orders** | Order lifecycle: created → confirmed → fulfilled → cancelled |
| **Inventory** | Stock levels, reservations, replenishment |
| **Payments** | Charge, refund, payment status tracking |
| **Shipping** | Shipment creation, tracking, delivery confirmation |

Each context owns its own data model. Cross-context communication happens via **domain events published to an in-process event bus** (or Azure Service Bus for async durability).

---

## Core Aggregate

### `Order`

The `Order` aggregate is the heart of the system. It enforces business invariants:

- An order can only be confirmed if inventory is reserved.
- An order can only be shipped if payment has succeeded.
- A cancelled order must trigger stock release and payment reversal.

**Aggregate Root:** `Order`  
**Entities inside aggregate:** `OrderLine`  
**Value Objects:** `OrderId`, `Money`, `Address`, `ProductId`, `Quantity`

---

## Domain Events

| Event | Raised By | Consumed By |
|---|---|---|
| `OrderCreated` | Orders | Inventory, Payments |
| `InventoryReserved` | Inventory | Orders |
| `PaymentSucceeded` | Payments | Orders, Shipping |
| `PaymentFailed` | Payments | Orders, Inventory |
| `ShipmentCreated` | Shipping | Orders |
| `OrderCancelled` | Orders | Inventory, Payments |

Events are **immutable records** — they represent facts that have already happened.

---

## Async Flows

### Purchase Flow

```
Customer places order
        |
        v
[Orders] OrderCreated event published
        |
        +---------> [Inventory] Reserve stock
        |                  |
        |            InventoryReserved event
        |                  |
        v                  v
[Payments] Charge customer
        |
   PaymentSucceeded / PaymentFailed
        |
        v
[Shipping] Create shipment (on PaymentSucceeded)
        |
   ShipmentCreated event
        |
        v
[Orders] Mark order as "Fulfilled"
```

### Cancellation Flow

```
Customer / system cancels order
        |
        v
[Orders] OrderCancelled event published
        |
        +---------> [Inventory] Release reserved stock
        |
        +---------> [Payments] Issue refund (if payment was taken)
        |
        v
Order status updated to "Cancelled"
```

---

## Why Modular Monolith

| Concern | Microservices | Modular Monolith (chosen) |
|---|---|---|
| Deployment complexity | High — many services, k8s, service mesh | Low — single deployable |
| Network latency | Cross-service HTTP calls | In-process — zero latency |
| Distributed transactions | Saga / 2PC needed | Simpler local transactions |
| Team size fit | Large, autonomous teams | Small team (early stage) |
| Eventual migration path | Already microservices | Extract per context when ready |

At this stage of the product the team size and deployment complexity do **not** justify microservices. The modular monolith gives strict module isolation without operational overhead, and each module can be extracted independently if the business grows.

---

## Architecture Principles Applied

- **Dependency Inversion** — Domain never imports Infrastructure.
- **Ports & Adapters** — Repository and messaging interfaces are ports; EF Core / ASB are adapters.
- **Transactional Outbox** — Domain events are persisted in the same DB transaction as the aggregate, then relayed by a background worker. Prevents lost events on crash.
- **Idempotent Consumers** — All event handlers check for duplicate delivery before acting.
- **Ubiquitous Language** — Code uses the same vocabulary as the domain: `Order`, `Reserve`, `Fulfil`, not `Record`, `Save`, `Process`.
