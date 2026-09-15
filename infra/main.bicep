targetScope = 'resourceGroup'

@description('Short environment name used in resource names, e.g. dev or prod.')
@allowed(['dev', 'prod'])
param environmentName string

@description('''
Azure region. This subscription is an Azure for Students offer, which restricts
deployment to a policy-controlled region list. As of the last probe only
eastus2 and canadacentral are permitted. See docs/adr/0003-region-constraint.md.
''')
@allowed(['eastus2', 'canadacentral'])
param location string = 'eastus2'

@description('Container image for the API, e.g. ghcr.io/dhruvprabhu05/wandersync-api:abc1234.')
param apiImage string

@description('''
Provision PostgreSQL. Left false by default: a Flexible Server runs about $15/month,
which would consume the entire student credit well inside the year. Turned on in
Phase 3 when the API actually needs a database, and stopped between sessions.
''')
param deployDatabase bool = false

@description('PostgreSQL administrator login. Ignored when deployDatabase is false.')
param postgresAdminUsername string = 'wsadmin'

@secure()
@description('PostgreSQL administrator password, supplied by CI from a secret. Never stored in the repo.')
param postgresAdminPassword string = ''

var namePrefix = 'wandersync-${environmentName}'
var suffix = take(uniqueString(resourceGroup().id), 6)

module observability 'modules/observability.bicep' = {
  name: 'observability'
  params: {
    namePrefix: namePrefix
    location: location
  }
}

module keyVault 'modules/keyvault.bicep' = {
  name: 'keyVault'
  params: {
    name: 'kv-ws-${environmentName}-${suffix}'
    location: location
  }
}

module containerEnv 'modules/container-env.bicep' = {
  name: 'containerEnv'
  params: {
    name: 'cae-${namePrefix}'
    location: location
    logAnalyticsWorkspaceId: observability.outputs.workspaceId
  }
}

module api 'modules/api.bicep' = {
  name: 'api'
  params: {
    name: 'ca-${namePrefix}-api'
    location: location
    environmentId: containerEnv.outputs.environmentId
    image: apiImage
    appInsightsConnectionString: observability.outputs.appInsightsConnectionString
    keyVaultName: keyVault.outputs.name
  }
}

module postgres 'modules/postgres.bicep' = if (deployDatabase) {
  name: 'postgres'
  params: {
    name: 'psql-${namePrefix}-${suffix}'
    location: location
    adminUsername: postgresAdminUsername
    adminPassword: postgresAdminPassword
  }
}

@description('Public HTTPS endpoint of the API.')
output apiUrl string = api.outputs.url

@description('Key Vault name, for wiring application secrets in later phases.')
output keyVaultName string = keyVault.outputs.name
