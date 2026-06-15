# Domain / Catalog

Pure domain model for the **Catalog** bounded context.

## Types (planned)

| Type | Kind | Notes |
|---|---|---|
| `Product` | Aggregate Root | Id, Name, Description, CategoryId, Images |
| `Category` | Entity | Hierarchical: parent/child |
| `Price` | Value Object | Amount + Currency; immutable |
| `ProductId` | Value Object | Strongly typed identifier |
| `Brand` | Value Object | Name, slug |
| `IProductRepository` | Repository interface | Implemented in Infrastructure |

## Invariants

- A product must always have a non-empty name.
- Price must be non-negative.
- A product must belong to exactly one leaf category.
