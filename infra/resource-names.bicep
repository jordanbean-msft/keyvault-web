param appName string
param region string
param env string

output appInsightsName string = 'ai-${appName}-${region}-${env}'
output logAnalyticsWorkspaceName string = 'la-${appName}-${region}-${env}'
output appServicePlanName string = 'asp-${appName}-${region}-${env}'
output appServiceNetCoreName string = 'wa-net-core-${appName}-${region}-${env}'
output appServiceNetFrameworkName string = 'wa-net-framework-${appName}-${region}-${env}'
output keyVaultName string = 'kv-${appName}-${region}-${env}'
output managedIdentityName string = 'mi-${appName}-${region}-${env}'
output theKingOfAustriaSecretName string = 'the-king-of-austria'
output theKingOfPrussiaSecretName string = 'the-king-of-prussia'
output theKingOfEnglandSecretName string = 'the-king-of-england'
