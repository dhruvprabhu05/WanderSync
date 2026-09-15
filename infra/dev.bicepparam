using 'main.bicep'

param environmentName = 'dev'
param location = 'eastus2'

// CI sets API_IMAGE to the image it just pushed, tagged with the commit SHA.
// The 'latest' fallback exists only so a local `az deployment group what-if`
// runs without ceremony.
param apiImage = readEnvironmentVariable('API_IMAGE', 'ghcr.io/dhruvprabhu05/wandersync-api:latest')

// Phase 3 flips this to true. Until then the API has no database and the
// student credit is not paying for an idle Flexible Server.
param deployDatabase = false

param postgresAdminUsername = 'wsadmin'
param postgresAdminPassword = readEnvironmentVariable('POSTGRES_ADMIN_PASSWORD', '')
