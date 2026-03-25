@description('The location for backend resources (SQL Server, etc.).')
param location string = 'japaneast'

@description('The location for Static Web App. Must be an SWA-supported region (japaneast is not supported).')
param swaLocation string = 'eastasia'

@description('The name of the Static Web App.')
param staticWebAppName string = 'menucraft-swa'

@description('The name of the SQL Server.')
param sqlServerName string = 'menucraft-sql'

@description('The name of the SQL Database.')
param sqlDatabaseName string = 'menucraft-db'

@description('The SQL administrator login.')
param sqlAdminLogin string

@secure()
@description('The SQL administrator password.')
param sqlAdminPassword string

// Static Web App (Free tier)
resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: staticWebAppName
  location: swaLocation
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    buildProperties: {
      appLocation: 'src/client'
      apiLocation: 'src/api'
      skipGithubActionWorkflowGeneration: true
    }
  }
}

// SQL Server
resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// SQL Database (Free tier)
resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 34359738368
    autoPauseDelay: 60
    minCapacity: json('0.5')
    useFreeLimit: true
    freeLimitExhaustionBehavior: 'AutoPause'
  }
}

// Allow Azure services to access SQL Server
resource sqlFirewallRule 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

output staticWebAppDefaultHostname string = staticWebApp.properties.defaultHostname
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
