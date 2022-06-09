param appName string
param environment string
param region string
param location string = resourceGroup().location
@secure()
param theKingOfAustriaSecretValue string
@secure()
param theKingOfPrussiaSecretValue string
@secure()
param theKingOfEnglandSecretValue string

module names 'resource-names.bicep' = {
  name: 'resource-names'
  params: {
    appName: appName
    region: region
    env: environment
  }
}

module managedIdentityDeployment 'managed-identity.bicep' = {
  name: 'managed-identity-deployment'
  params: {
    location: location
    managedIdentityName: names.outputs.managedIdentityName
  }
}

module loggingDeployment 'logging.bicep' = {
  name: 'logging-deployment'
  params: {
    logAnalyticsWorkspaceName: names.outputs.logAnalyticsWorkspaceName
    location: location
    appInsightsName: names.outputs.appInsightsName
    appServiceNetCoreName: names.outputs.appServiceNetCoreName
    appServiceNetFrameworkName: names.outputs.appServiceNetFrameworkName
  }
}

module keyVaultDeployment 'key-vault.bicep' = {
  name: 'key-vault-deployment'
  params: {
    keyVaultName: names.outputs.keyVaultName
    location: location
    logAnalyticsWorkspaceName: loggingDeployment.outputs.logAnalyticsWorkspaceName
    managedIdentityName: managedIdentityDeployment.outputs.managedIdentityName
    theKingOfAustriaSecretName: names.outputs.theKingOfAustriaSecretName
    theKingOfAustriaSecretValue: theKingOfAustriaSecretValue
    theKingOfPrussiaSecretName: names.outputs.theKingOfPrussiaSecretName
    theKingOfPrussiaSecretValue: theKingOfPrussiaSecretValue
    theKingOfEnglandSecretName: names.outputs.theKingOfEnglandSecretName
    theKingOfEnglandSecretValue: theKingOfEnglandSecretValue
  }
}

module appServicePlanDeployment 'app-service-plan.bicep' = {
  name: 'app-service-plan-deployment'
  params: {
    appServicePlanName: names.outputs.appServicePlanName
    location: location
  }
}

module appServiceNetCoreDeployment 'app-service-net-core.bicep' = {
  name: 'app-service-net-core-deployment'
  params: {
    appInsightsName: loggingDeployment.outputs.appInsightsName
    appServiceNetCoreName: names.outputs.appServiceNetCoreName
    appServicePlanName: appServicePlanDeployment.outputs.appServicePlanName
    location: location
    logAnalyticsWorkspaceName: loggingDeployment.outputs.logAnalyticsWorkspaceName
    managedIdentityName: managedIdentityDeployment.outputs.managedIdentityName
  }
}

module appServiceNetFrameworkDeployment 'app-service-net-framework.bicep' = {
  name: 'app-service-net-framework-deployment'
  params: {
    appInsightsName: loggingDeployment.outputs.appInsightsName
    appServiceNetFrameworkName: names.outputs.appServiceNetFrameworkName
    appServicePlanName: appServicePlanDeployment.outputs.appServicePlanName
    location: location
    logAnalyticsWorkspaceName: loggingDeployment.outputs.logAnalyticsWorkspaceName
    managedIdentityName: managedIdentityDeployment.outputs.managedIdentityName
  }
}
