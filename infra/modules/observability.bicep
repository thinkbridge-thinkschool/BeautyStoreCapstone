// ── infra/modules/observability.bicep ────────────────────────────────────────
// Responsibility : Log Analytics Workspace + Application Insights
//
// All telemetry (traces, logs, metrics) is stored in the Log Analytics
// workspace and queried via KQL in the Application Insights blade or
// directly in Log Analytics.

param prefix   string
param suffix   string
param location string
param tags     object

@description('Log retention in days. PerGB2018 minimum is 30.')
@minValue(30)
@maxValue(730)
param retentionDays int = 30

// ── Log Analytics Workspace ───────────────────────────────────────────────────

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name    : '${prefix}-law-${suffix}'
  location: location
  tags    : tags
  properties: {
    sku            : { name: 'PerGB2018' }
    retentionInDays: retentionDays
  }
}

// ── Application Insights ──────────────────────────────────────────────────────
// Workspace-based: telemetry is stored in Log Analytics, not Classic AI tables.
// This enables cross-resource KQL queries and alert rules on custom metrics.

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name    : '${prefix}-ai-${suffix}'
  location: location
  tags    : tags
  kind    : 'web'
  properties: {
    Application_Type               : 'web'
    WorkspaceResourceId            : logAnalytics.id
    IngestionMode                  : 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery    : 'Enabled'
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

// Connection string (not @secure — safe to expose; contains only the
// instrumentation key GUID, not a password or access token).
output connectionString     string = appInsights.properties.ConnectionString
output appInsightsName      string = appInsights.name
output appInsightsId        string = appInsights.id
output logAnalyticsName     string = logAnalytics.name
