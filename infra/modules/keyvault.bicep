@description('Key Vault name. Globally unique, 3-24 chars.')
@minLength(3)
@maxLength(24)
param name string

@description('Azure region.')
param location string

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    // RBAC rather than access policies: access is granted with role assignments,
    // which are visible in `az role assignment list` alongside everything else.
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    // Deliberately off. Purge protection cannot be disabled once enabled, and a
    // soft-deleted name stays reserved for the full retention window -- which makes
    // tearing down and redeploying this environment impossible. See ADR 0004.
    enablePurgeProtection: null
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

output name string = vault.name
output id string = vault.id
output uri string = vault.properties.vaultUri
