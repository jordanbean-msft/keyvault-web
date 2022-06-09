# keyvault-web

## Create the certificate

```shell
.\create-certificate.ps1
```

## Build/publish & deploy to Azure

```shell
dotnet publish --configuration Release
```

```shell
Compress-Archive -DestinationPath ./app.zip -Update ./bin/Release/net6.0/publish
```

```shell
az webapp deployment source config-zip --resource-group rg-keyvault-web-ussc-dev --name wa-keyvault-web-ussc-dev --src ./app.zip
```
