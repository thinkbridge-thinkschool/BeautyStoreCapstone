# BeautyStore.Api

**Layer: Presentation**

ASP.NET Core Web API — the HTTP entry point for the BeautyStore application.

## Responsibilities

- Define REST controllers for each bounded context (CatalogController, OrdersController, etc.).
- Handle authentication and authorization middleware (JWT bearer).
- Wire up dependency injection — register Application services, Infrastructure adapters.
- Global exception handling and problem-details formatting.
- Swagger / OpenAPI documentation.

## What does NOT belong here

- Business rules — those live in `BeautyStore.Application`.
- Database access — that lives in `BeautyStore.Infrastructure`.
- Domain logic — that lives in `BeautyStore.Domain`.

## Depends on

- `BeautyStore.Application` (use-case interfaces)
- `BeautyStore.Infrastructure` (only at the DI composition root)
