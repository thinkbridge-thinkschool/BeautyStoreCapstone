// ── infra/modules/sql.bicep ───────────────────────────────────────────────────
// Responsibility : Azure SQL Server + one database
//
// All five bounded contexts (Catalog, Orders, Inventory, Payments, Shipping)
// share a single database, separated by EF Core schema prefixes:
//   Catalog.*    Orders.*    Inventory.*    Payments.*    Shipping.*
//
// This is the Modular Monolith pattern: one deployable unit, no network hops
// for cross-context reads, but logical isolation at the schema level.

// ── Parameters ────────────────────────────────────────────────────────────────

param serverName   string
param location     string
param tags         object
param databaseName string = 'BeautyStoreDb'

@description('SQL Server admin login. Cannot be "admin", "sa", "root", or "administrator".')
param adminLogin string

@secure()
@description('SQL admin password. Requires upper + lower + digit + special, >= 8 chars.')
param adminPassword string

@description('DTU SKU name. Basic = 5 DTU (~$5/mo). S1 = 20 DTU (~$30/mo).')
param skuName string = 'Basic'

@description('Pricing tier matching the SKU name.')
param skuTier string = 'Basic'

// ── SQL Server ────────────────────────────────────────────────────────────────

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name    : serverName
  location: location
  tags    : tags
  properties: {
    administratorLogin        : adminLogin
    administratorLoginPassword: adminPassword
    version                   : '12.0'
    minimalTlsVersion         : '1.2'     // rejects TLS 1.0 and 1.1
    publicNetworkAccess       : 'Enabled'
  }
}

// ── Firewall: Allow Azure Services ───────────────────────────────────────────
// The IP range 0.0.0.0 → 0.0.0.0 is Azure's magic sentinel for
// "allow all Azure-origin traffic". Container Apps use dynamic outbound IPs,
// so this rule is necessary. In production with a VNet, replace with a
// Virtual Network Rule instead.
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name  : 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress  : '0.0.0.0'
  }
}

// ── Database ──────────────────────────────────────────────────────────────────

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent  : sqlServer
  name    : databaseName
  location: location
  tags    : tags
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    collation              : 'SQL_Latin1_General_CP1_CI_AS'
    requestedBackupStorageRedundancy: 'Local'
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

output serverName string = sqlServer.name
output serverFqdn string = sqlServer.properties.fullyQualifiedDomainName

// @secure() prevents the connection string appearing in:
//   az deployment group show, Portal Outputs blade, and any CI/CD logs.
@secure()
output connectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${databaseName};User ID=${adminLogin};Password=${adminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
