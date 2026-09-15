@description('Container Apps managed environment name.')
param name string

@description('Azure region.')
param location string

@description('Resource ID of the Log Analytics workspace to send container logs to.')
param logAnalyticsWorkspaceId string

// Referenced rather than passed in as a secret: listKeys stays inside this module,
// so the shared key never appears in a module output or in deployment history.
resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: last(split(logAnalyticsWorkspaceId, '/'))
}

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: name
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: workspace.properties.customerId
        sharedKey: workspace.listKeys().primarySharedKey
      }
    }
    zoneRedundant: false
  }
}

output environmentId string = environment.id
output defaultDomain string = environment.properties.defaultDomain
