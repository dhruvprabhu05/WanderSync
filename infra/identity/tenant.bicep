// The Entra External ID tenant that holds WanderSync's end users.
//
// Deployed into rg-wandersync-identity, deliberately separate from the dev
// environment: dev is torn down and redeployed routinely, and the user directory
// must never go with it.
//
// Created as an Azure resource rather than through the Entra admin center because
// the subscription lives in the gatech.edu tenant, where directory-level tenant
// creation is disabled for regular users. See docs/adr/0007.

targetScope = 'resourceGroup'

@description('Tenant resource name. Becomes <name>.onmicrosoft.com and <name>.ciamlogin.com. Alphanumeric, max 26.')
@maxLength(26)
param tenantName string = 'wandersync'

@description('Display name shown in the Entra admin center.')
param displayName string = 'WanderSync'

@description('Data residency. Cannot be changed after creation.')
@allowed(['United States', 'Europe', 'Asia Pacific', 'Australia'])
param dataLocation string = 'United States'

@description('ISO country code for the tenant. Cannot be changed after creation.')
param countryCode string = 'US'

resource externalTenant 'Microsoft.AzureActiveDirectory/ciamDirectories@2023-05-17-preview' = {
  name: tenantName
  location: dataLocation
  sku: {
    // Standard is the MAU-billed tier; the first 50,000 monthly active users are free.
    name: 'Standard'
    tier: 'A0'
  }
  properties: {
    createTenantProperties: {
      countryCode: countryCode
      displayName: displayName
    }
  }
}

output tenantId string = externalTenant.properties.tenantId
output domain string = '${tenantName}.onmicrosoft.com'
output authority string = 'https://${tenantName}.ciamlogin.com/${tenantName}.onmicrosoft.com/'
