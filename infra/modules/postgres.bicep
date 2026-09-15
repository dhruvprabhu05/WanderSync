@description('PostgreSQL Flexible Server name. Globally unique, lowercase.')
param name string

@description('Azure region.')
param location string

@description('Administrator login.')
param adminUsername string

@secure()
@description('Administrator password.')
param adminPassword string

@description('Database name created alongside the server.')
param databaseName string = 'wandersync'

@description('PostgreSQL major version.')
param postgresVersion string = '16'

resource server 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: name
  location: location
  sku: {
    // Burstable is the cheapest tier that exists. Anything larger is
    // unaffordable on a $100/12-month credit.
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    version: postgresVersion
    administratorLogin: adminUsername
    administratorLoginPassword: adminPassword
    storage: {
      storageSizeGB: 32
      autoGrow: 'Disabled'
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
    // No availabilityZone specified on purpose: student subscriptions frequently
    // reject explicit zones with "unsupported availability zone". Letting Azure
    // choose avoids a deployment failure that is tedious to diagnose.
    network: {
      publicNetworkAccess: 'Enabled'
    }
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: server
  name: databaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// Container Apps egresses from shared Azure IPs, so this is the pragmatic
// Phase 3 answer. Private networking needs a VNet the budget cannot justify;
// the real fix is Entra authentication, tracked for Phase 4.
resource allowAzureServices 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: server
  name: 'AllowAllAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

output fullyQualifiedDomainName string = server.properties.fullyQualifiedDomainName
output databaseName string = database.name
