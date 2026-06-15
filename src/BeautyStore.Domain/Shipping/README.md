# Domain / Shipping

Pure domain model for the **Shipping** bounded context.

## Types (planned)

| Type | Kind | Notes |
|---|---|---|
| `Shipment` | Aggregate Root | OrderId, TrackingNumber, Provider, Status |
| `ShipmentId` | Value Object | Strongly typed GUID |
| `TrackingNumber` | Value Object | Immutable string from the logistics provider |
| `ShipmentStatus` | Enum | Created → InTransit → OutForDelivery → Delivered / Failed |
| `Address` | Value Object | Shared; destination for this shipment |
| `IShipmentRepository` | Repository interface | Implemented in Infrastructure |
| `IShippingProvider` | Port (interface) | Abstracts Shiprocket / Delhivery; implemented in Infrastructure |

## Domain Events raised

| Event | When |
|---|---|
| `ShipmentCreated` | `Shipment.Create(...)` called after payment |

## Invariants

- A shipment must always have a non-empty `TrackingNumber` once created.
- Once delivered, status cannot regress.
