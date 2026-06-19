// ── infra/main.bicep ──────────────────────────────────────────────────────────
// Project  : BeautyStoreCapstone — Beauty & Skincare Marketplace
// Scope    : resourceGroup
//
// Deployment entry point. Orchestrates three domain modules:
//
//   main.bicep
//   ├── caEnv          (existing Container Apps Environment)
//   ├── modules/api.bicep
//   │     ├── Azure Container Registry   (stores BeautyStore.Api image)
//   │     └── Azure Container App        (runs BeautyStore.Api)
//   ├── modules/sql.bicep
//   │     ├── Azure SQL Server
//   │     └── BeautyStoreDb
//   └── modules/servicebus.bicep
//         ├── Namespace (Standard — required for Topics)
//         ├── order-events  (topic)
//         ├── inventory-subscription
//         └── shipping-subscription

targetScope = 'resourceGroup'

// ── Parameters ────────────────────────────────────────────────────────────────

@description('Environment discriminator. Drives SKU selection and replica counts.')
@allowed(['dev', 'prod'])
param environment string

@description('Azure region. Inherits the resource-group region by default.')
param location string = resourceGroup().location

@description('Container image tag. Use a Git SHA in prod; "latest" is fine for dev.')
param imageTag string = 'latest'

@description('SQL Server administrator login name.')
param sqlAdminLogin string = 'beautyadmin'

@secure()
@description('SQL administrator password — upper + lower + digit + special, >= 8 chars.')
param sqlAdminPassword string

@description('Name of the Azure SQL database.')
param databaseName string = 'BeautyStoreDb'

@description('Entra ID tenant ID for JWT validation. Not a secret — safe in source control.')
param tenantId string

@description('App Registration client ID (= API audience). Not a secret.')
param clientId string

@description('Comma-separated CORS allowed origins.')
param allowedOrigins string = 'http://localhost:4200'

@description('''
Name of the existing Container Apps Environment to host the Container App.
Azure limits one CAE per region per subscription on Free / Trial tiers.
Multiple Container Apps from different projects share the same CAE safely.
''')
param existingCaeName string

@description('Resource group that owns the existing Container Apps Environment.')
param existingCaeResourceGroup string

// ── Naming helpers ────────────────────────────────────────────────────────────
// uniqueString(RG id) returns the same 13-char hex hash every time for the
// same resource group, making re-deployments idempotent (no "already exists").
var suffix = substring(uniqueString(resourceGroup().id), 0, 6)
var prefix = 'beautystore-${environment}'

// ACR names are globally unique and must be alphanumeric only (no hyphens).
var acrName          = 'beautystore${environment}acr${suffix}'
var containerAppName = '${prefix}-api'

var tags = {
  environment: environment
  project    : 'BeautyStoreCapstone'
  iac        : 'bicep'
}

// ── Container Apps Environment (existing) ────────────────────────────────────
// `existing` = look this resource up, do NOT create or modify it.
// `scope` is required when the CAE lives in a different resource group.
resource caEnv 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name : existingCaeName
  scope: resourceGroup(existingCaeResourceGroup)
}

// ── Module: Observability ─────────────────────────────────────────────────────
module observability './modules/observability.bicep' = {
  name  : 'observability-deploy'
  params: {
    prefix      : prefix
    suffix      : suffix
    location    : location
    tags        : tags
    retentionDays: environment == 'prod' ? 90 : 30
  }
}

// ── Module: Service Bus ───────────────────────────────────────────────────────
module serviceBus './modules/servicebus.bicep' = {
  name  : 'servicebus-deploy'
  params: {
    namespaceName: '${prefix}-sb-${suffix}'
    location     : location
    tags         : tags
  }
}

// ── Module: SQL ───────────────────────────────────────────────────────────────
module sql './modules/sql.bicep' = {
  name  : 'sql-deploy'
  params: {
    serverName   : '${prefix}-sql-${suffix}'
    location     : location
    tags         : tags
    adminLogin   : sqlAdminLogin
    adminPassword: sqlAdminPassword
    databaseName : databaseName
    skuName      : environment == 'prod' ? 'S1'       : 'Basic'
    skuTier      : environment == 'prod' ? 'Standard' : 'Basic'
  }
}

// ── Module: API ───────────────────────────────────────────────────────────────
// Connection strings from sql and serviceBus flow as @secure() outputs —
// ARM never logs them. They arrive here and are forwarded directly as
// @secure() params into api.bicep where they become Container App secrets.
module api './modules/api.bicep' = {
  name  : 'api-deploy'
  params: {
    acrName                    : acrName
    containerAppName           : containerAppName
    location                   : location
    tags                       : tags
    environmentId              : caEnv.id
    imageTag                   : imageTag
    tenantId                   : tenantId
    clientId                   : clientId
    serviceBusNamespace        : serviceBus.outputs.namespaceName
    sqlServerFqdn              : sql.outputs.serverFqdn
    appInsightsConnectionString: observability.outputs.connectionString
    allowedOrigins             : allowedOrigins
    databaseName               : databaseName
    acrSku              : environment == 'prod' ? 'Standard' : 'Basic'
    minReplicas         : environment == 'prod' ? 2          : 0
    maxReplicas         : environment == 'prod' ? 10         : 3
    cpu                 : environment == 'prod' ? '0.5'      : '0.25'
    memory              : environment == 'prod' ? '1Gi'      : '0.5Gi'
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output apiUrl              string = api.outputs.url
output acrLoginServer      string = api.outputs.acrLoginServer
output sqlServerName       string = sql.outputs.serverName
output databaseName        string = databaseName
output serviceBusNamespace string = serviceBus.outputs.namespaceName
