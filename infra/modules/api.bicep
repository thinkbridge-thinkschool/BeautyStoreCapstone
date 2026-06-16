// ── infra/modules/api.bicep ───────────────────────────────────────────────────
// Responsibility : Azure Container Registry + Azure Container App
//
// This module owns two resources that share a tight lifecycle:
//   1. ACR — stores the BeautyStore.Api Docker image
//   2. Container App — pulls from ACR and serves HTTP traffic
//
// The Container Apps Environment (CAE) is passed in as a resource ID parameter.
// It is NOT created here — the caller (main.bicep) resolves it via `existing`.

// ── Identity ──────────────────────────────────────────────────────────────────

@description('Name of the Azure Container Registry. Alphanumeric only, 5-50 chars.')
param acrName string

@description('Name of the Azure Container App.')
param containerAppName string

param location     string
param tags         object

// ── Placement ─────────────────────────────────────────────────────────────────

@description('Resource ID of the Container Apps Environment.')
param environmentId string

// ── Image ─────────────────────────────────────────────────────────────────────

@description('Container image tag. "latest" for dev; Git SHA for prod.')
param imageTag string = 'latest'

// ── Secrets (injected from main.bicep, sourced from sql + servicebus modules) ─

@secure()
param jwtKey string

@secure()
param serviceBusConnectionString string

@secure()
param sqlConnectionString string

// ── App configuration ─────────────────────────────────────────────────────────

param allowedOrigins string
param databaseName   string = 'BeautyStoreDb'

// ── Scaling / sizing ──────────────────────────────────────────────────────────

@description('Minimum replicas. Set 0 for scale-to-zero in dev (idle cost = $0).')
param minReplicas int = 0

@description('Maximum replicas under load.')
param maxReplicas int = 3

@description('vCPU allocation per replica. Must be a string; any(json()) converts it to a decimal for ARM.')
param cpu string = '0.25'

@description('Memory per replica, e.g. "0.5Gi" or "1Gi".')
param memory string = '0.5Gi'

// ── ACR SKU ───────────────────────────────────────────────────────────────────

@allowed(['Basic', 'Standard', 'Premium'])
param acrSku string = 'Basic'

// ── Azure Container Registry ──────────────────────────────────────────────────

resource acr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name    : acrName
  location: location
  tags    : tags
  sku     : { name: acrSku }
  properties: {
    adminUserEnabled: true   // required for Container Apps password-based pull
  }
}

// ── Azure Container App ───────────────────────────────────────────────────────

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name    : containerAppName
  location: location
  tags    : tags

  properties: {
    environmentId: environmentId

    configuration: {

      // Public HTTPS ingress. Container Apps issues a free managed TLS cert.
      // allowInsecure: false enforces HTTPS-only at the ingress controller.
      ingress: {
        external     : true
        targetPort   : 8080
        transport    : 'http'
        allowInsecure: false
      }

      // ACR credentials. The password lives in the secrets store below —
      // it never appears in an environment variable.
      registries: [
        {
          server           : acr.properties.loginServer
          username         : acr.listCredentials().username
          passwordSecretRef: 'registry-password'
        }
      ]

      // ARM encrypts secrets at rest. Environment variables reference them
      // by name via `secretRef` — the actual value is never logged.
      secrets: [
        { name: 'registry-password', value: acr.listCredentials().passwords[0].value }
        { name: 'jwt-key',           value: jwtKey                                    }
        { name: 'service-bus-conn',  value: serviceBusConnectionString                }
        { name: 'sql-conn',          value: sqlConnectionString                       }
      ]
    }

    template: {
      containers: [
        {
          name : 'beautystore-api'
          image: '${acr.properties.loginServer}/beautystoreapi:${imageTag}'

          resources: {
            // any(json(cpu)) converts the string '0.25' to the decimal 0.25
            // that the ARM schema expects. Bicep has no native float literal.
            cpu   : any(json(cpu))
            memory: memory
          }

          // ASP.NET Core maps double-underscore → config section colon:
          //   ConnectionStrings__BeautyStoreDb → ConnectionStrings:BeautyStoreDb
          //   ServiceBus__OrderEventsTopic     → ServiceBus:OrderEventsTopic
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT',             value    : 'Production'            }
            { name: 'ASPNETCORE_URLS',                    value    : 'http://+:8080'          }
            { name: 'Jwt__Key',                           secretRef: 'jwt-key'                }
            { name: 'Jwt__Issuer',                        value    : 'BeautyStoreApi'         }
            { name: 'Jwt__Audience',                      value    : 'BeautyStoreApi'         }
            { name: 'Jwt__ExpiryMinutes',                 value    : '15'                     }
            { name: 'Jwt__RefreshExpiryDays',             value    : '7'                      }
            { name: 'ConnectionStrings__${databaseName}', secretRef: 'sql-conn'               }
            { name: 'ServiceBus__ConnectionString',       secretRef: 'service-bus-conn'       }
            { name: 'ServiceBus__OrderEventsTopic',       value    : 'order-events'           }
            { name: 'ServiceBus__InventorySubscription',  value    : 'inventory-subscription' }
            { name: 'ServiceBus__ShippingSubscription',   value    : 'shipping-subscription'  }
            { name: 'AllowedOrigins',                     value    : allowedOrigins           }
          ]

          // Liveness  — restart the container if it stops responding.
          // Readiness — hold traffic until the app finishes startup.
          probes: [
            {
              type   : 'Liveness'
              httpGet: { path: '/health', port: 8080, scheme: 'HTTP' }
              initialDelaySeconds: 15
              periodSeconds      : 30
              failureThreshold   : 3
            }
            {
              type   : 'Readiness'
              httpGet: { path: '/health', port: 8080, scheme: 'HTTP' }
              initialDelaySeconds: 5
              periodSeconds      : 10
              successThreshold   : 1
            }
          ]
        }
      ]

      // KEDA HTTP scaler: add a replica when any active replica exceeds
      // 20 concurrent requests. minReplicas: 0 = scale-to-zero in dev.
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
        rules: [
          {
            name: 'http-concurrency'
            http: { metadata: { concurrentRequests: '20' } }
          }
        ]
      }
    }
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

output url            string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output acrLoginServer string = acr.properties.loginServer
output containerAppName string = containerApp.name
