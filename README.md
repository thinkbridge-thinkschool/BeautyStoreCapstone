# BeautyStore Capstone — ThinkSchool Day 23

Nykaa-style Beauty & Skincare Marketplace built as a **Modular Monolith** following **Clean Architecture** and **Domain-Driven Design (DDD)** principles.

---

## Clean Architecture

Each module is organized in four strict layers. Dependencies point **inward only** — outer layers depend on inner layers, never the reverse.

```
Presentation  (BeautyStore.Api)
     |
Application   (BeautyStore.Application)
     |
Domain        (BeautyStore.Domain)        <-- no external dependencies
     |
Infrastructure (BeautyStore.Infrastructure)
```

| Layer | Responsibility |
|---|---|
| **Domain** | Aggregates, Entities, Value Objects, Domain Events, Repository interfaces |
| **Application** | Use-cases (Commands / Queries), Application Services, DTOs |
| **Infrastructure** | EF Core, messaging (Azure Service Bus), payment gateway, shipping adapter |
| **Api** | ASP.NET Core controllers, middleware, DI wiring |

---

## Modular Monolith

Rather than splitting into separate deployable microservices, all bounded contexts live in one process but are **physically separated** into their own folders and namespaces. Benefits:

- Single deployment unit — simpler CI/CD.
- Cross-module calls happen in-process via interfaces, not HTTP.
- Each module owns its own database schema slice (no shared tables).
- Ready to extract to microservices later if the business warrants it.

---

## DDD Concepts Used

| Concept | Where |
|---|---|
| **Bounded Context** | Catalog / Orders / Inventory / Payments / Shipping |
| **Aggregate** | `Order` is the core aggregate; contains `OrderLine` value objects |
| **Domain Events** | `OrderCreated`, `InventoryReserved`, `PaymentSucceeded`, etc. |
| **Repository Pattern** | Interfaces in Domain; implementations in Infrastructure |
| **Value Objects** | `Money`, `Address`, `ProductId`, `OrderId` |
| **Ubiquitous Language** | Each bounded context has its own model and terminology |

---

## Project Structure

```
BeautyStoreCapstone/
├── src/
│   ├── BeautyStore.Api                 # HTTP entry point
│   ├── BeautyStore.Application/        # Use-cases per bounded context
│   ├── BeautyStore.Domain/             # Pure domain model
│   └── BeautyStore.Infrastructure/     # External concerns
├── tests/
│   ├── BeautyStore.UnitTests
│   └── BeautyStore.IntegrationTests
└── CAPSTONE_DESIGN.md                  # Architecture decisions & async flows
```
