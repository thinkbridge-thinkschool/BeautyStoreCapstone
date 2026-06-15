# Application / Shipping

Use-cases for the **Shipping** bounded context.

## Commands / Queries (planned)

| Name | Type | Description |
|---|---|---|
| `CreateShipmentCommand` | Command | Book a shipment with the logistics provider; raises `ShipmentCreated` |
| `UpdateTrackingStatusCommand` | Command | Process tracking webhook from provider |
| `GetShipmentStatusQuery` | Query | Current shipment and tracking info for an order |

## Event Handlers (planned)

| Handler | Listens to | Action |
|---|---|---|
| `OnPaymentSucceeded` | `PaymentSucceeded` | Trigger shipment creation with the provider |

## Notes

The shipping provider adapter (`IShippingProvider`) is defined as a port here. Infrastructure contains the concrete Shiprocket / Delhivery HTTP adapter. Tracking updates arrive via webhook and are stored as an append-only log per shipment.
