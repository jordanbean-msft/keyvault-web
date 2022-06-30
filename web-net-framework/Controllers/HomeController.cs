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
            ViewData["the-king-of-austria"] = ConfigurationManager.AppSettings["the-king-of-austria"];
            ViewData["the-king-of-prussia"] = ConfigurationManager.AppSettings["the-king-of-prussia"];
            ViewData["the-king-of-england"] = ConfigurationManager.AppSettings["the-king-of-england"];

            return View();
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