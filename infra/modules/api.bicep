@description('Container App name.')
param name string

@description('Azure region.')
param location string

@description('Resource ID of the Container Apps managed environment.')
param environmentId string

@description('Container image, e.g. ghcr.io/dhruvprabhu05/wandersync-api:abc1234.')
param image string

@description('Application Insights connection string.')
param appInsightsConnectionString string

@description('Key Vault name. The app identity is granted read access to its secrets.')
param keyVaultName string

@description('Port the container listens on. Matches ASPNETCORE_URLS in the Dockerfile.')
param containerPort int = 8080

// Built-in role: Key Vault Secrets User.
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    environmentId: environmentId
    configuration: {
      ingress: {
        external: true
        targetPort: containerPort
        transport: 'auto'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      // ghcr.io public images need no registry credentials. Private ones would
      // add a registries[] block with a secret; see ADR 0005 on why not ACR.
    }
    template: {
      containers: [
        {
          name: 'api'
          image: image
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsightsConnectionString
            }
            {
              name: 'KEYVAULT_NAME'
              value: keyVaultName
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health/live'
                port: containerPort
              }
              initialDelaySeconds: 5
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health/ready'
                port: containerPort
              }
              initialDelaySeconds: 3
              periodSeconds: 10
            }
          ]
        }
      ]
      scale: {
        // Scales to zero when idle. On a student subscription this is the
        // difference between a few dollars a year and a few dollars a month.
        minReplicas: 0
        maxReplicas: 2
        rules: [
          {
            name: 'http'
            http: {
              metadata: {
                concurrentRequests: '20'
              }
            }
          }
        ]
      }
    }
  }
}

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource secretsAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, app.id, keyVaultSecretsUserRoleId)
  scope: vault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: app.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output url string = 'https://${app.properties.configuration.ingress.fqdn}'
output principalId string = app.identity.principalId
