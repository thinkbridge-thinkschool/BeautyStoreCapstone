using './main.bicep'

// ── dev.bicepparam ────────────────────────────────────────────────────────────
// Target RG  : rg-beautystore-dev
// Region     : centralindia
// Goal       : smallest SKUs, scale-to-zero, cost ≈ $0 when idle
//
// Why dev credentials are safe to check in:
//   • rg-beautystore-dev is ephemeral — torn down after each demo session.
//   • SQL firewall allows Azure-origin traffic only (0.0.0.0 → 0.0.0.0).
//   • JWT key has no value outside this dev Container App.
//   Never copy these values into prod.

param environment      = 'dev'
param location         = 'centralindia'
param imageTag         = 'latest'
param sqlAdminLogin    = 'beautyadmin'
param sqlAdminPassword = 'Dev@Passw0rd!'
param databaseName     = 'BeautyStoreDb'
// Entra ID — NOT secrets. Replace with your App Registration values.
param tenantId         = 'abcb3509-e099-4c7c-88c5-1a2f27ee93b5'
param clientId         = '37c13986-f680-4593-95ec-49d0c1eb62de'
param allowedOrigins   = 'http://localhost:4200'

// ── Existing Container Apps Environment ───────────────────────────────────────
// Azure Free / Trial subscriptions allow exactly ONE Container Apps Environment
// per region per subscription. Multiple projects share the same CAE — this is
// the correct pattern, not a workaround.
// Replace with your own CAE name and resource group if different.
param existingCaeName          = 'cae-beautystore-dev'
param existingCaeResourceGroup = 'rg-beautystore-shared'
