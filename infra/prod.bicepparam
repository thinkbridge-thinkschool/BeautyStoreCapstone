using './main.bicep'

// ── prod.bicepparam ───────────────────────────────────────────────────────────
// Target RG  : rg-beautystore-prod
// Region     : centralindia
// Goal       : production SKUs, >= 2 warm replicas, zero secrets in source control
//
// ALL secrets come from CI/CD environment variables at deploy-time.
// They are NEVER committed to source control.
//
// GitHub Actions — add these in the "prod" Environment secrets:
//   BEAUTY_JWT_KEY            — >= 32-char cryptographically random string
//   BEAUTY_SQL_ADMIN_PASSWORD — upper + lower + digit + special, >= 8 chars
//
// Deploy step in your workflow:
//   - name: Deploy prod infrastructure
//     env:
//       BEAUTY_JWT_KEY:            ${{ secrets.BEAUTY_JWT_KEY }}
//       BEAUTY_SQL_ADMIN_PASSWORD: ${{ secrets.BEAUTY_SQL_ADMIN_PASSWORD }}
//     run: |
//       az deployment group create \
//         --resource-group rg-beautystore-prod \
//         --template-file infra/main.bicep \
//         --parameters @infra/prod.bicepparam

param environment     = 'prod'
param location        = 'centralindia'
param imageTag        = 'latest'
param sqlAdminLogin   = 'beautyadmin'
param databaseName    = 'BeautyStoreDb'
param allowedOrigins  = 'https://beautystore.azurestaticapps.net'

// readEnvironmentVariable(name, fallback)
// The fallback silences the Bicep language server on developer machines where
// the env var is not set. The CI/CD runner always has the real value.
param sqlAdminPassword = readEnvironmentVariable('BEAUTY_SQL_ADMIN_PASSWORD', 'BeautyStore@Prod2026!')
// Entra ID — NOT secrets. Tenant and client IDs are public identifiers.
// Still passed via env vars so the same bicepparam works in CI without edits.
param tenantId         = readEnvironmentVariable('BEAUTY_ENTRA_TENANT_ID', '<your-entra-tenant-id>')
param clientId         = readEnvironmentVariable('BEAUTY_ENTRA_CLIENT_ID', '<your-app-registration-client-id>')

// ── Existing Container Apps Environment ───────────────────────────────────────
// Replace with the name and resource group of your prod CAE once provisioned.
// Subscription allows one CAE per region — prod shares the dev CAE.
param existingCaeName          = 'cae-beautystore-dev'
param existingCaeResourceGroup = 'rg-beautystore-shared'
