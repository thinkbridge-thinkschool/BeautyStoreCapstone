# Application / Catalog

Use-cases for the **Catalog** bounded context.

## Commands / Queries (planned)

| Name | Type | Description |
|---|---|---|
| `GetProductByIdQuery` | Query | Fetch a single product's details |
| `SearchProductsQuery` | Query | Full-text search with filters (brand, category, price range) |
| `ListCategoriesQuery` | Query | Return the category tree |
| `CreateProductCommand` | Command | Admin: add a new product listing |
| `UpdatePricingCommand` | Command | Admin: update price or active promotions |

## Notes

Catalog is a **read-heavy** context. Queries can bypass the Domain layer and hit a read model (thin DTO from DB) for performance. Commands go through the full Domain aggregate path.
