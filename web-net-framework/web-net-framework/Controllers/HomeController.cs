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
            string keyVaultName = ConfigurationManager.AppSettings["Authentication:KeyVaultName"];
            var kvUri = "https://" + keyVaultName + ".vault.azure.net";

            SecretClient client = null;

            if (bool.Parse(ConfigurationManager.AppSettings["IsHostedInAzure"]))
            {
                var x509Store = new X509Store(StoreName.My,
                                      StoreLocation.CurrentUser);

                x509Store.Open(OpenFlags.ReadOnly);

                var x509Certificate = x509Store.Certificates.Find(X509FindType.FindByThumbprint,
                                                                  ConfigurationManager.AppSettings["Authentication:AzureADCertificateThumbprint"],
                                                                  validOnly: false)
                                                            .OfType<X509Certificate2>()
                                                            .Single();

                client = new SecretClient(new Uri(kvUri),
                                              new ClientCertificateCredential(
                                                ConfigurationManager.AppSettings["Authentication:AzureADDirectoryId"],
                                                ConfigurationManager.AppSettings["Authentication:AzureADApplicationId"],
                                                x509Certificate));
            }
            else
            {
                client = new SecretClient(new Uri(kvUri),
                                              new DefaultAzureCredential(new DefaultAzureCredentialOptions
                                              {
                                                  ManagedIdentityClientId = ConfigurationManager.AppSettings["Authentication:ManagedIdentityClientId"]
                                              }));

            }

            var theKingOfAustriaSecret = await client.GetSecretAsync("the-king-of-austria");
            var theKingOfPrussiaSecret = await client.GetSecretAsync("the-king-of-prussia");
            var theKingOfEnglandSecret = await client.GetSecretAsync("the-king-of-england");

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