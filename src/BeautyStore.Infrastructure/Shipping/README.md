# Infrastructure — Shipping

Adapter implementation for the Shipping bounded context.

Responsibilities:
- HTTP client for an external shipping carrier API (e.g., Delhivery, ShipRocket).
- Maps domain `Shipment` objects to carrier-specific request/response DTOs.
- Implements the `IShippingGateway` port defined in `BeautyStore.Domain`.
- Publishes `ShipmentCreated` events via the outbox after a successful carrier booking.
