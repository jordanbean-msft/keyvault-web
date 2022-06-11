using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Mvc;
using web_net_core.Models;

namespace web_net_core.Controllers;

public class HomeController : Controller
{
  private readonly ILogger<HomeController> _logger;
  private readonly IConfiguration _configuration;

  public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
  {
    _logger = logger;
    _configuration = configuration;
  }

  public async Task<IActionResult> Index()
  {
    ViewData["the-king-of-austria"] = _configuration["the-king-of-austria"];
    ViewData["the-king-of-prussia"] = _configuration["the-king-of-prussia"];

    string secret = await GetTheKingOfEngland();
    ViewData["the-king-of-england"] = secret;

    return View();
  }

  private async Task<string> GetTheKingOfEngland()
  {
    string keyVaultName = _configuration["Authentication:KeyVaultName"];
    string kvUri = $"https://{_configuration["KeyVaultName"]}.vault.azure.net/";

    SecretClient? client = null;

    if (bool.Parse(_configuration["IsHostedOnPrem"]))
    {
      var x509Store = new X509Store(StoreName.My,
                                    StoreLocation.LocalMachine);

      x509Store.Open(OpenFlags.ReadOnly);

      X509Certificate2 x509Certificate;

      try
      {
        x509Certificate = x509Store.Certificates.Find(X509FindType.FindByThumbprint,
                                                      _configuration["Authentication:AzureADCertificateThumbprint"],
                                                      validOnly: false)
                                                .OfType<X509Certificate2>()
                                                .Single();
      }
      catch (Exception ex)
      {
        throw new Exception($"Unable to find certificate in cert:\\LocalMachine\\My with thumbprint: {_configuration["Authentication:AzureADCertificateThumbprint"]}", ex);
      }
            try
            {
                client = new SecretClient(new Uri(kvUri),
                                              new ClientCertificateCredential(
                                                _configuration["Authentication:AzureADDirectoryId"],
                                                _configuration["Authentication:AzureADApplicationId"],
                                                x509Certificate));
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to create SecretClient with Uri: {kvUri}, AzureADDirectoryId: {_configuration["Authentication:AzureADDirectoryId"]}, AzureADApplicationId: {_configuration["Authentication:AzureADApplicationId"]}, certificateThumbprint: {_configuration["Authentication:AzureADCertificateThumbprint"]}", ex);
            }
        }
        else
        {
            client = new SecretClient(new Uri(kvUri),
                                          new DefaultAzureCredential(new DefaultAzureCredentialOptions
                                          {
                                              ManagedIdentityClientId = _configuration["Authentication:ManagedIdentityClientId"]
                                          }));

        }

        Response<KeyVaultSecret> secret;

    try
    {
      secret = await client.GetSecretAsync("the-king-of-england");
    }
    catch (Exception ex)
    {
      throw new Exception($"Unable to get secret \"the-king-of-england\" from Key Vault {kvUri}", ex);
    }

    return secret.Value.Value;
  }

  public IActionResult Privacy()
  {
    return View();
  }

  [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
  public IActionResult Error()
  {
    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
  }
}
