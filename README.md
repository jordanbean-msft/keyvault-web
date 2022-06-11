# keyvault-web

This repo shows how to retrieve secrets stored in an Azure Key Vault using a .NET Framework & .NET Core application. It demonstrates both pulling the secrets via middleware at startup time and dynamically when a page is loaded.

## Onprem deployment

![architecture-onprem](.img/architecture-onprem.png)

Onprem, you need to provide a way for the running application to access the Key Vault securly. This is accomplished via service principal provisioned in Azure Active Directory. This service principal is then granted access to the Key Vault. The application authenticates to Azure Active Directory using a X.509 certificate so that it can use the service principal to access the Key Vault.

**Note**: The application is written to pull the certificate from the `cert:\LocalMachine\My` certificate store on the onprem server. If this is not the right location to get your certificate from, you will need to modify the code to pull the certificate from the correct location.

You will need to modify code similar to the below code to pull from the right store for your deployment.

```cs
var x509Store = new X509Store(StoreName.My,
                              StoreLocation.LocalMachine);
```

## Azure deployment

![architecture-azure](.img/architecture-azure.png)

In Azure, the process can be simplified by using a Managed Identity. The Managed Identity is granted access to the Key Vault & is assigned to the App Service so code running in the App Service can use it. The deployment script will set the Managed Identity client ID for you as part of deployment. This will override the values specified in the configuration files.

**Note**: This repo intentionally doesn't access the secrets through the Azure App Service configuration so that it is portable between onprem & Azure. If you are only targeting Azure, you can store the secrets in the [App Service configuration](https://docs.microsoft.com/en-us/azure/app-service/app-service-key-vault-references?tabs=azure-cli).

**Note**: You could still use the [certificate-based authentication](https://docs.microsoft.com/en-us/azure/app-service/configure-ssl-certificate?tabs=apex%2Cportal) method to allow your application to authenticate with Azure AD instead of using Managed Identity. Managed Identity makes the process simpler, but is not required.

## Disclaimer

**THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.**

## .NET Framework

The .NET Framework version of this application pulls the secrets from Key Vault when the web page is loaded. It does not have a middleware that pulls the secrets at startup time. You can install a common middleware like [OWIN](https://docs.microsoft.com/en-us/aspnet/aspnet/overview/owin-and-katana/) to provide a middleware, but this is not required. If you install & use OWIN, you can add similar code in the Startup.cs file to pull the secrets at startup time.

The following NuGet packages are required to access Key Vault using .NET Framework (these will install some additional dependencies):

- Azure.Identity
- Azure.Security.KeyVault.Secrets

If you look at the `./web-net-framework/Web.config` file, you can see the following app settings which will allow the application to authenticate with Azure Active Directory so it can use the service principal to access the Key Vault.

```xml
<appSettings>
  ...
  <add key="KeyVaultName" value="kv-keyvault-web-ussc-dev" />
  <add key="Authentication:AzureADApplicationId" value="9bfd1049-3cfe-4466-a684-2b5fb636b03e" />
  <add key="Authentication:AzureADCertificateThumbprint" value="A17D4362FBF40049BB4AA7EB465D082358C7878A" />
  <add key="Authentication:AzureADDirectoryId" value="72f988bf-86f1-41af-91ab-2d7cd011db47" />
  <add key="Authentication:ManagedIdentityClientId" value="" />
  <add key="IsHostedOnPrem" value="true" />
</appSettings>
```

Here is what the running application should look like if it was able to successfully authenticate with Azure Active Directory & pull secrets from Key Vault.

![web-net-framework](.img/web-net-framework.png)

### Onprem authentication with certificate

In the `./web-net-framework/Controllers/HomeController.cs`, we pull the certificate from the local store, authenticate with Azure AD, then pull the secrets that are needed.

```cs
string keyVaultName = ConfigurationManager.AppSettings["KeyVaultName"];
var kvUri = "https://" + keyVaultName + ".vault.azure.net";

SecretClient client = null;

var x509Store = new X509Store(StoreName.My,
                      StoreLocation.LocalMachine);

x509Store.Open(OpenFlags.ReadOnly);

X509Certificate2 x509Certificate;

// Get the certificate from the store based upon the thumbprint
x509Certificate = x509Store.Certificates.Find(X509FindType.FindByThumbprint,
                                              ConfigurationManager.AppSettings["Authentication:AzureADCertificateThumbprint"],
                                              validOnly: false)
                                        .OfType<X509Certificate2>()
                                        .Single();

// Authenticate with Azure AD, passing in the certificate & indicating with service principal to authenticate with (specified via the AzureADApplicationId)
client = new SecretClient(new Uri(kvUri),
                              new ClientCertificateCredential(
                                ConfigurationManager.AppSettings["Authentication:AzureADDirectoryId"],
                                ConfigurationManager.AppSettings["Authentication:AzureADApplicationId"],
                                x509Certificate));

// Get the secrets
theKingOfAustriaSecret = await client.GetSecretAsync("the-king-of-austria");
```

### App Service with Managed Identity

Using the Managed Identity associated with the App Service, it much simpler to authenticate with Azure AD.

```cs
 client = new SecretClient(new Uri(kvUri),
                            new DefaultAzureCredential(new DefaultAzureCredentialOptions
                            {
                                ManagedIdentityClientId = ConfigurationManager.AppSettings["Authentication:ManagedIdentityClientId"]
                            }));
```

## .NET Core

Similarly, the .NET Core version of this application pulls the Key Vault secrets when the web page is loaded. However, because .NET Core already has a middleware installed, most of the secrets are pulled at startup time. Only 1 secret is pulled at page load time to demonstrate how to pull secrets dynamically.

If you look at the `./web-net-core/appsettings.json` file, you can see the following app settings which will allow the application to authenticate with Azure Active Directory so it can use the service principal to access the Key Vault.

```json
"KeyVaultName": "kv-keyvault-web-ussc-dev",
"Authentication": {
  "AzureADApplicationId": "9bfd1049-3cfe-4466-a684-2b5fb636b03e",
  "AzureADCertificateThumbprint": "539cd5afadb7b25b85cf90a78c261074a6db6445",
  "AzureADDirectoryId": "72f988bf-86f1-41af-91ab-2d7cd011db47",
  "ManagedIdentityClientId": ""
},
"IsHostedOnPrem": "true"
```

Here is what the running application should look like if it was able to successfully authenticate with Azure Active Directory & pull secrets from Key Vault.

![web-net-core](.img/web-net-core.png)

### Startup.cs code to pull all secrets as configuration values

The middleware allows us to pull all secrets from Key Vault at startup time and store them as configuration values that can be used throughout the application (look at the `./web-net-core/Program.cs` file).

```cs
builder.Configuration.AddAzureKeyVault(new Uri(kvUri), new ClientCertificateCredential(
                                          builder.Configuration["Authentication:AzureADDirectoryId"],
                                          builder.Configuration["Authentication:AzureADApplicationId"],
                                          x509Certificate));
```

## Prerequisites

- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Dotnet CLI](https://docs.microsoft.com/en-us/dotnet/core/tools/)
- [.NET Framework 4.7.2](https://dotnet.microsoft.com/en-us/download/dotnet-framework)
- Azure subscription & resource group
- On-prem server

## Deployment

### Create the certificate

It is recommeded that you get a signed certificate from your company's certificate authority. However, if you cannot, you can generate a self-signed certificate locally.

Run this script as **Administrator** on the onprem server where you are running the application.

```shell
./create-certificate.ps1
```

This script will generate a self-signed certificate, install it in the `cert:\LocalMachine\My` certificate store and write the public key `.cer` file to the current user's Desktop. You will need to upload this file to the Azure AD service principal so the local application can use it to authenticate.

### Create the app registration/service principal

You will need a service principal in Azure Active Directory to be the identity that your application uses to access the Key Vault.

1.  Navigate to the [Azure portal](https://portal.azure.com) and sign in.

1.  Click on the `Azure Active Directory` link in the left-hand navigation menu.

1.  Click on the `App registrations` blade.

1.  Click on the `New registration` button.

1.  Give it a name & a redirect uri where your application will be listening (http://localhost as an example for running locally).

![register-application](.img/register-application.png)

1.  Click on the `Certificates & secrets` blade.

1.  Upload the `.cer` certificate file that was created in the previous step. Save the `Thumbprint` value for configuring the application.

![upload-cert](.img/upload-cert.png)

1.  On the `Overview` blade, copy the `Application ID (client ID)` and the `Directory (tenant) ID` values for configuring the application.

![aad-overview](.img/aad-overview.png)

### Deploy the infrastructure

1.  Modify the `./infra/env/dev.parameters.json` file as needed. Make sure and update the `azureADApplicationId` with the `Application ID (client ID)` value from the previous step.

```shell
az deployment group create -g rg-keyvault-web-ussc-dev --template-file ./infra/main.bicep --parameters ./infra/env/dev.parameters.json --parameters theKingOfAustriaSecretValue="Joseph the 2nd" theKingOfPrussiaSecretValue="Fredrick Wilhelm the 3rd" theKingOfEnglandSecretValue="Why the tyrant King George, of course!"
```

The script will output the name of the Key Vault & the URLs to the web apps.

### Grant the app registration access to Key Vault

1.  Navigate to the [Azure portal](https://portal.azure.com) and sign in.

1.  Select the `Key Vault` created in the previous step.

1.  Click on the `Access policies` blade.

1.  Click on the `Add Access Policy` button.

1.  Add `Get` and `List` `Secret permissions`.

![get-list-permissions](.img/get-list-permissions.png)

1.  Click on the `Select principal` button.

1.  Search for your newly created service principal (from the previous step), select it and click the `Select` button. Click on the `Add` button.

![keyVault-add-access-policy](.img/keyVault-add-access-policy.png)

1.  Click on the `Save` button.

1.  Repeat these steps for the Managed Identity created in the previous step.

![keyVault-access-policies](.img/keyVault-access-policies.png)

### Configure onprem server to use certificate

You will likely need to configure the onprem server to be able to use the certificate from the local certificate store.

1.  Login to the onprem server as `Administrator`.

1.  Open the `Computer certificate store` and select the store where you have provisioned your certificate (`Personal` in this example).

1.  Right-click on the provisioned certificate and select `All Tasks->Manage Private Keys`.

![certificate-manage-private-keys](.img/certificate-manage-private-keys.png)

1.  Click on the `Add` button and select the account that the IIS app pool is running under (`IIS_ISRS` in this example).

1.  Select `Full control` as the permissions and click `OK`.

### Build/publish .NET Framework Web App & deploy to onprem server

1.  Open the `./web-net-framework/web-net-framework.sln` file in Visual Studio.

1.  Update the `./web-net-framework/Web.config` file with the your values.

    - `KeyVaultName` - the name of your Key Vault
    - `Authentication:AzureADApplicationId` - the application ID (client ID) of your service principal
    - `Authentication:AzureADCertificateThumbprint` - the thumbprint of the certificate installed on the machine that will be used to authenticate with Azure Active Directory
    - `Authentication:AzureADDirectoryId` - the tenant ID where your service principal is instantiated
    - `Authentication:ManagedIdentityClientId` - this value doesn't need to be set when running onprem (since there is no managed identity to use)
    - `IsHostedOnPrem` - set this value to `true`

**Note**: You can also set these values in IIS and override the values in the `./web-net-framework/Web.config` file.

![iis-application-settings](.img/iis-application-settings.png)

![iis-application-settings-thumbprint](.img/iis-application-settings-thumbprint.png)

1.  Right-click on the project and select **Build**.

1.  Right-click on the project and select **Publish**.

**Note**: The following instructions assume you are using Web Deploy for the onprem IIS server. You could also manually copy your application to the onprem server.

1.  Click the **New** button.

1.  Select `Web Server (IIS)` as the publish type. Click `Next`.

1.  Choose `Web Deploy` as the specific target. Click `Next`.

1.  Enter the credentials for the onprem server. Click `Next`. and `Finish`.

1.  Click `Publish` to push your code to the onprem server.

### Build/publish .NET Core Web App & deploy to onprem server

1.  Open the `./web-net-core/web-net-core.csproj` file in Visual Studio.

1.  Update the `./web-net-core/appsettings.json` file with the your values.

    - `KeyVaultName` - the name of your Key Vault
    - `Authentication:AzureADApplicationId` - the application ID (client ID) of your service principal
    - `Authentication:AzureADCertificateThumbprint` - the thumbprint of the certificate installed on the machine that will be used to authenticate with Azure Active Directory
    - `Authentication:AzureADDirectoryId` - the tenant ID where your service principal is instantiated
    - `Authentication:ManagedIdentityClientId` - this value doesn't need to be set when running onprem (since there is no managed identity to use)
    - `IsHostedOnPrem` - set this value to `true`

**Note**: You can also set these values in IIS and override the values in the `./web-net-core/appsettings.json` file.

![iis-application-settings](.img/iis-application-settings.png)

![iis-application-settings-thumbprint](.img/iis-application-settings-thumbprint.png)

1.  Right-click on the project and select **Build**.

1.  Right-click on the project and select **Publish**.

**Note**: The following instructions assume you are using Web Deploy for the onprem IIS server. You could also manually copy your application to the onprem server.

1.  Click the **New** button.

1.  Select `Web Server (IIS)` as the publish type. Click `Next`.

1.  Choose `Web Deploy` as the specific target. Click `Next`.

1.  Enter the credentials for the onprem server. Click `Next`. and `Finish`.

1.  Click `Publish` to push your code to the onprem server.

### Build/publish .NET Framework Web App & deploy to Azure

1.  Open the `./web-net-framework/web-net-framework.sln` file in Visual Studio.

1.  You don't need to modify the values of the `./web-net-framework/Web.config` file since the values will be set in the App Service Configuration settings automatically by the Infrastructure as Code Bicep scripts.

1.  Right-click on the project and select **Build**.

1.  Right-click on the project and select **Publish**.

1.  Click the **New** button.

1.  Select **Azure**, then **Next**. Select **Azure App Service (Windows)**, then **Next**.

1.  Select your **Azure subscription**, **Resource Group** and **App Service instance** (make sure and select the **wa-net-framework** App Service), then **Finish** and **Close**.

1.  Click **Publish** to push your app to the App Service.

### Build/publish .NET Core Web App & deploy to Azure

1. Navigate to the `./web-net-core` directory on the command line (or use Visual Studio).

1. Build the application & create a publish package.

```shell
dotnet publish --configuration Release
```

1.  Zip up the publish package.

```shell
Compress-Archive -DestinationPath ./app.zip -Update ./bin/Release/net6.0/publish
```

1.  Deploy your zip package to Azure.

```shell
az webapp deployment source config-zip --resource-group rg-keyvault-web-ussc-dev --name wa-keyvault-web-ussc-dev --src ./app.zip
```
