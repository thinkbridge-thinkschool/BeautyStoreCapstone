// ── infra/modules/servicebus.bicep ────────────────────────────────────────────
// Responsibility : Service Bus Namespace + order-events topic + 2 subscriptions
//
// Async domain-event flow across bounded contexts:
//
//   [Orders BC]  publishes  OrderCreated / OrderCancelled
//        │
//        ▼
//   order-events  (topic — Standard SKU required; Basic only supports Queues)
//        │
//   ┌────┴─────────────────────┐
//   ▼                          ▼
// inventory-subscription   shipping-subscription
//        │                          │
//   [Inventory BC]           [Shipping BC]
//   reserves / releases      creates carrier booking
//   stock on OrderCreated    after PaymentSucceeded

// ── Parameters ────────────────────────────────────────────────────────────────

param namespaceName string
param location      string
param tags          object

// ── Service Bus Namespace ─────────────────────────────────────────────────────

resource sbNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name    : namespaceName
  location: location
  tags    : tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
}

// ── Topic: order-events ───────────────────────────────────────────────────────

resource orderEventsTopic 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: sbNamespace
  name  : 'order-events'
  properties: {
    defaultMessageTimeToLive           : 'P14D'   // 14-day retention
    requiresDuplicateDetection         : true      // idempotent publish
    duplicateDetectionHistoryTimeWindow: 'PT10M'   // 10-minute de-dupe window
    enableBatchedOperations            : true
  }
}

// ── Subscription: inventory-subscription ─────────────────────────────────────

resource inventorySubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: orderEventsTopic
  name  : 'inventory-subscription'
  properties: {
    maxDeliveryCount               : 10       // to DLQ after 10 failed deliveries
    lockDuration                   : 'PT1M'   // consumer holds peek-lock for 1 min
    deadLetteringOnMessageExpiration: true     // expired msgs → DLQ, not silent drop
    enableBatchedOperations        : true
  }
}

// ── Subscription: shipping-subscription ──────────────────────────────────────

resource shippingSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: orderEventsTopic
  name  : 'shipping-subscription'
  properties: {
    maxDeliveryCount               : 10
    lockDuration                   : 'PT1M'
    deadLetteringOnMessageExpiration: true
    enableBatchedOperations        : true
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

output namespaceName string = sbNamespace.name

// @secure() ensures the connection string is redacted from ARM deployment logs
// and the Portal Outputs blade at every stage of the chain:
// servicebus.bicep → main.bicep → api.bicep → Container App secret
@secure()
output connectionString string = listKeys(
  '${sbNamespace.id}/AuthorizationRules/RootManageSharedAccessKey',
  sbNamespace.apiVersion
).primaryConnectionString
