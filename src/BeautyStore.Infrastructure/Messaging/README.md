# Infrastructure / Messaging

Azure Service Bus integration and Transactional Outbox relay.

## Contents (planned)

| File | Purpose |
|---|---|
| `ServiceBusPublisher.cs` | Implements `IMessagePublisher`; sends messages to ASB topics |
| `ServiceBusConsumer.cs` | Base consumer; handles receive, lock, complete, dead-letter |
| `OutboxRelayWorker.cs` | `BackgroundService` that polls outbox table and publishes to ASB |
| `MessageEnvelope.cs` | Wrapper with `MessageType`, `CorrelationId`, `OccurredAt` |
| `ServiceBusOptions.cs` | Configuration POCO for connection strings and topic names |

## How the Outbox Relay works

1. Application layer writes domain events to `outbox_messages` table **in the same DB transaction** as the aggregate save.
2. `OutboxRelayWorker` wakes up every N seconds, fetches unpublished messages.
3. Publishes each to the Azure Service Bus topic.
4. Marks message as `Published` in the outbox.
5. If the process crashes between steps 2–4, the message is retried on restart (at-least-once delivery).

This pattern was implemented end-to-end on ThinkSchool Day 20.
