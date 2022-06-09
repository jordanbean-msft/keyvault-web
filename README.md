# keyvault-web

![architecture](.img/architecture.png)

## Disclaimer

**THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.**

## Prerequisites

- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Dotnet CLI](https://docs.microsoft.com/en-us/dotnet/core/tools/)
- [.NET Framework 4.7.2](https://dotnet.microsoft.com/en-us/download/dotnet-framework)
- Azure subscription & resource group

## Deployment

### Deploy the infrastructure

```shell
az deployment group create -g rg-keyvault-web-ussc-dev --template-file ./infra/main.bicep --parameters ./infra/env/dev.parameters.json --parameters theKingOfAustriaSecretValue="Joseph the 2nd" theKingOfPrussiaSecretValue="Fredrick Wilhelm the 3rd" theKingOfEnglandSecretValue="Why the tyrant King George, of course!"
```

### Build/publish .NET Framework Web App & deploy to Azure

1.  Right-click on the project and select **Build**.

1.  Right-click on the project and select **Publish**.

1.  Click the **New** button.

1.  Select **Azure**, then **Next**. Select **Azure App Service (Windows)**, then **Next**.

1.  Select your **Azure subscription**, **Resource Group** and **App Service instance** (make sure and select the **wa-net-framework** App Service), then **Finish** and **Close**.

1.  Click **Publish** to push your app to the App Service.

### Create the certificate

```shell
.\create-certificate.ps1
```

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
