# keyvault-web

This repo shows how to retrieve secrets stored in an Azure Key Vault using a .NET Framework & .NET Core application. It demonstrates both pulling the secrets via middleware at startup time and dynamically when a page is loaded.

## Onprem deployment

![architecture-onprem](.img/architecture-onprem.png)

Onprem, you need to provide a way for the running application to access the Key Vault securly. This is accomplished via service principal provisioned in Azure Active Directory. This service principal is then granted access to the Key Vault. The application authenticates to Azure Active Directory using a X.509 certificate so that it can use the service principal to access the Key Vault.

## Azure deployment

![architecture-azure](.img/architecture-azure.png)

In Azure, the process can be simplified by using a Managed Identity. The Managed Identity is granted access to the Key Vault & is assigned to the App Service so code running in the App Service can use it.

**Note**: This repo intentionally doesn't access the secrets through the Azure App Service configuration so that it is portable between onprem & Azure. If you are only targeting Azure, you can store the secrets in the [App Service configuration](https://docs.microsoft.com/en-us/azure/app-service/app-service-key-vault-references?tabs=azure-cli).

## Disclaimer

**THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.**

## .NET Framework

The .NET Framework version of this application pulls the secrets from Key Vault when the web page is loaded. It does not have a middleware that pulls the secrets at startup time. You can install a common middleware like [OWIN]() to provide a middleware, but this is not required.

If you look at the `./web-net-framework/Web.config` file, you can see the following app settings which will allow the application to authenticate with Azure Active Directory so it can use the service principal to access the Key Vault.

```xml
<appSettings>
  ...
  <add key="Authentication:KeyVaultName" value="kv-keyvault-web-ussc-dev" />
  <add key="Authentication:AzureADApplicationId" value="9bfd1049-3cfe-4466-a684-2b5fb636b03e" />
  <add key="Authentication:AzureADCertificateThumbprint" value="A17D4362FBF40049BB4AA7EB465D082358C7878A" />
  <add key="Authentication:AzureADDirectoryId" value="72f988bf-86f1-41af-91ab-2d7cd011db47" />
  <add key="Authentication:ManagedIdentityClientId" value="" />
  <add key="IsHostedOnPrem" value="false" />
</appSettings>
```

## .NET Core

## Prerequisites

- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Dotnet CLI](https://docs.microsoft.com/en-us/dotnet/core/tools/)
- [.NET Framework 4.7.2](https://dotnet.microsoft.com/en-us/download/dotnet-framework)
- Azure subscription & resource group
- Onprem server

## Deployment

### Deploy the infrastructure

```shell
az deployment group create -g rg-keyvault-web-ussc-dev --template-file ./infra/main.bicep --parameters ./infra/env/dev.parameters.json --parameters theKingOfAustriaSecretValue="Joseph the 2nd" theKingOfPrussiaSecretValue="Fredrick Wilhelm the 3rd" theKingOfEnglandSecretValue="Why the tyrant King George, of course!"
```

### Create the certificate

```shell
./create-certificate.ps1
```

### Create the app registration

### Build/publish .NET Framework Web App & deploy to Azure

1.  Update the `./web-net-framework/Web.config` file with the your values.

    - `KeyVaultName` - the name of your Key Vault
    - `Authentication:AzureADApplicationId` - the application ID (client ID) of your service principal
    - `Authentication:AzureADCertificateThumbprint` - the thumbprint of the certificate installed on the machine that will be used to authenticate with Azure Active Directory
    - `Authentication:AzureADDirectoryId` - the tenant ID where your service principal is instantiated

1.  Right-click on the project and select **Build**.

1.  Right-click on the project and select **Publish**.

1.  Click the **New** button.

1.  Select **Azure**, then **Next**. Select **Azure App Service (Windows)**, then **Next**.

1.  Select your **Azure subscription**, **Resource Group** and **App Service instance** (make sure and select the **wa-net-framework** App Service), then **Finish** and **Close**.

1.  Click **Publish** to push your app to the App Service.

### Grant the app registration access to Key Vault

### Build/publish .NET Core Web App & deploy to Azure

```shell
dotnet publish --configuration Release
```

```shell
Compress-Archive -DestinationPath ./app.zip -Update ./bin/Release/net6.0/publish
```

```shell
az webapp deployment source config-zip --resource-group rg-keyvault-web-ussc-dev --name wa-keyvault-web-ussc-dev --src ./app.zip
```
