// ── infra/modules/api.bicep ───────────────────────────────────────────────────
// Responsibility : Azure Container Registry + Azure Container App
//
// Day 25 — Identity End-to-End
//   • Container App has a System-Assigned Managed Identity (MSI).
//   • ACR pull uses the MSI via AcrPull role — no admin credentials stored.
//   • SQL auth uses "Authentication=Active Directory Managed Identity" in the
//     connection string — no User ID or Password in any secret or env var.
//   • Service Bus auth uses DefaultAzureCredential in code — only the namespace
//     FQDN is stored (not a connection string with a shared-access key).
//   • Entra ID JWT validation uses Authority/Audience — no symmetric signing key.
//   → Zero application secrets in Container App configuration.

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

// ── Entra ID config (NOT secrets — safe as plain env vars) ────────────────────

@description('Entra ID tenant ID for JWT validation.')
param tenantId string

@description('App Registration client ID (= API audience).')
param clientId string

// ── Service Bus (namespace name only — no connection string) ──────────────────

@description('Service Bus namespace name, e.g. "beautystore-dev-sb-xxxxx". FQDN is derived.')
param serviceBusNamespace string

// ── SQL Server FQDN (no password — MSI handles auth) ─────────────────────────

@description('SQL Server fully-qualified domain name, e.g. "xxx.database.windows.net".')
param sqlServerFqdn string

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
    adminUserEnabled: true   // required for Container Apps registry pull
  }
}

// ── Azure Container App ───────────────────────────────────────────────────────

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name    : containerAppName
  location: location
  tags    : tags

  // System-Assigned MSI: identity used for ACR pull, Service Bus, and SQL.
  identity: {
    type: 'SystemAssigned'
  }

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

      // ACR pull uses admin credentials (infrastructure credential — not an app secret).
      // The password lives in the secrets store; it never appears in an env var.
      registries: [
        {
          server           : acr.properties.loginServer
          username         : acr.listCredentials().username
          passwordSecretRef: 'registry-password'
        }
      ]

      // Only infrastructure secret remains — the ACR pull password.
      // Application secrets (jwt-key, sql-conn, service-bus-conn) are gone.
      secrets: [
        { name: 'registry-password', value: acr.listCredentials().passwords[0].value }
      ]
    }

    template: {
      containers: [
        {
          name : 'beautystore-api'
          image: '${acr.properties.loginServer}/beautystoreapi:${imageTag}'

          resources: {
            cpu   : any(json(cpu))
            memory: memory
          }

          // All values below are NON-SECRET configuration.
          // No secretRef anywhere — proven zero secrets in app settings.
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT',             value: 'Production'             }
            { name: 'ASPNETCORE_URLS',                    value: 'http://+:8080'           }
            // Entra ID — tenant and client IDs are public identifiers, not secrets
            { name: 'AzureAd__TenantId',                  value: tenantId                  }
            { name: 'AzureAd__ClientId',                  value: clientId                  }
            // SQL — Managed Identity auth; no User ID or Password in this string
            { name: 'ConnectionStrings__${databaseName}', value: 'Server=tcp:${sqlServerFqdn},1433;Database=${databaseName};Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;' }
            // Service Bus — FQDN only; DefaultAzureCredential handles auth in code
            { name: 'ServiceBus__Namespace',              value: '${serviceBusNamespace}.servicebus.windows.net' }
            { name: 'ServiceBus__OrderEventsTopic',       value: 'order-events'            }
            { name: 'ServiceBus__InventorySubscription',  value: 'inventory-subscription'  }
            { name: 'ServiceBus__ShippingSubscription',   value: 'shipping-subscription'   }
            { name: 'AllowedOrigins',                     value: allowedOrigins            }
          ]

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

output url              string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output acrLoginServer   string = acr.properties.loginServer
output containerAppName string = containerApp.name
// principalId is used by post-deploy CLI commands to assign Service Bus and SQL roles
output principalId      string = containerApp.identity.principalId
