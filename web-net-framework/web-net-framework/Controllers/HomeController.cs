using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using Azure;

namespace web_net_framework.Controllers
{
    struct SecretData
    {
        public string TheKingOfAustria { get; set; }
        public string TheKingOfPrussia { get; set; }
        public string TheKingOfEngland { get; set; }
    }

    public class HomeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            SecretData secretData = await GetSecrets();
            ViewData["the-king-of-austria"] = secretData.TheKingOfAustria;
            ViewData["the-king-of-prussia"] = secretData.TheKingOfPrussia;
            ViewData["the-king-of-england"] = secretData.TheKingOfEngland;

            return View();
        }

        private async Task<SecretData> GetSecrets()
        {
            string keyVaultName = ConfigurationManager.AppSettings["KeyVaultName"];
            var kvUri = "https://" + keyVaultName + ".vault.azure.net";

            SecretClient client = null;

            if (bool.Parse(ConfigurationManager.AppSettings["IsHostedOnPrem"]))
            {
                var x509Store = new X509Store(StoreName.My,
                                      StoreLocation.LocalMachine);

                x509Store.Open(OpenFlags.ReadOnly);

                X509Certificate2 x509Certificate;

                try
                {
                    x509Certificate = x509Store.Certificates.Find(X509FindType.FindByThumbprint,
                                                                  ConfigurationManager.AppSettings["Authentication:AzureADCertificateThumbprint"],
                                                                  validOnly: false)
                                                            .OfType<X509Certificate2>()
                                                            .Single();
                }
                catch(Exception ex)
                {
                    throw new Exception($"Unable to find certificate in cert:\\LocalMachine\\My with thumbprint: {ConfigurationManager.AppSettings["Authentication:AzureADCertificateThumbprint"]}", ex);
                }

                try
                {
                    client = new SecretClient(new Uri(kvUri),
                                                  new ClientCertificateCredential(
                                                    ConfigurationManager.AppSettings["Authentication:AzureADDirectoryId"],
                                                    ConfigurationManager.AppSettings["Authentication:AzureADApplicationId"],
                                                    x509Certificate));
                }
                catch(Exception ex)
                {
                    throw new Exception($"Unable to create SecretClient with Uri: {kvUri}, AzureADDirectoryId: {ConfigurationManager.AppSettings["Authentication:AzureADDirectoryId"]}, AzureADApplicationId: {ConfigurationManager.AppSettings["Authentication:AzureADApplicationId"]}, certificateThumbprint: {ConfigurationManager.AppSettings["Authentication:AzureADCertificateThumbprint"]}", ex);                }
            }
            else
            {
                client = new SecretClient(new Uri(kvUri),
                                              new DefaultAzureCredential(new DefaultAzureCredentialOptions
                                              {
                                                  ManagedIdentityClientId = ConfigurationManager.AppSettings["Authentication:ManagedIdentityClientId"]
                                              }));

            }

            Response<KeyVaultSecret> theKingOfAustriaSecret;
            Response<KeyVaultSecret> theKingOfPrussiaSecret;
            Response<KeyVaultSecret> theKingOfEnglandSecret;

            try
            {
                theKingOfAustriaSecret = await client.GetSecretAsync("the-king-of-austria");
                theKingOfPrussiaSecret = await client.GetSecretAsync("the-king-of-prussia");
                theKingOfEnglandSecret = await client.GetSecretAsync("the-king-of-england");
            }
            catch(Exception ex)
            {
                throw new Exception($"Unable to get secrets from Key Vault {kvUri}", ex);
            }

            SecretData returnData = new SecretData
            {
                TheKingOfAustria = theKingOfAustriaSecret.Value.Value,
                TheKingOfPrussia = theKingOfPrussiaSecret.Value.Value,
                TheKingOfEngland = theKingOfEnglandSecret.Value.Value
            };

            return returnData;
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}