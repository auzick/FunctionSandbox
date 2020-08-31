using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Andy.Portal.Models;
using Andy.AzureStorage.Tables;
using System.Text;
using Andy.Model;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Andy.Portal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> log;
        private readonly IHttpClientFactory clientFactory;
        private readonly IConfiguration config;

        public HomeController(
            ILogger<HomeController> logger,
            IHttpClientFactory clientFactory,
            IConfiguration configRoot
        )
        {
            log = logger;
            this.clientFactory = clientFactory;
            this.config = configRoot;
        }

        public IActionResult Index()
        {
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Index(string userName, string userEmail, string userPhone, string userPassword)
        {
            var registration = new UserRegistration()
            {
                UserName = userName,
                UserEmail = userEmail,
                UserPhone = userPhone,
                UserPassword = userPassword
            };
            var cl = clientFactory.CreateClient();
            var url = $"https://{config["FunctionsHostname"]}/api/register/?code=OaOwQ1g52wM3nckgrlKWiXawtkKp14mmyX0zUKZndxnyg/D1YOpPQw==";
            var result = await cl.PostAsJsonAsync(url, registration);
            var content = await result.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<object>(content);
            var opt = new JsonSerializerOptions() { WriteIndented = true };
            var formatted = JsonSerializer.Serialize(json, opt);

            ViewBag.json = formatted;
            return View("Submitted");
        }

        [Route("VerifySms/{id}")]
        public async Task<IActionResult> VerifySms(string id)
        {
            var mgmtUrls = new OrchestrationEntity().Fetch("RegisterOrchestrator", id);
            var eventUrl = mgmtUrls.SendEventPostUri.Replace("{eventName}", "SmsVerified");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, eventUrl);
            request.Content = new StringContent("true", Encoding.UTF8, "application/json");

            var cl = clientFactory.CreateClient();
            var result = await cl.SendAsync(request);
            ViewBag.url = eventUrl;
            ViewBag.content = await result.Content.ReadAsStringAsync();
            ViewBag.resultCode = result.StatusCode;
            ViewBag.resultPhrase = result.ReasonPhrase;
            return View("VerifySms");
        }

        [Route("VerifyEmail/{id}")]
        public async Task<IActionResult> VerifyEmail(string id)
        {
            var mgmtUrls = new OrchestrationEntity().Fetch("RegisterOrchestrator", id);
            var eventUrl = mgmtUrls.SendEventPostUri.Replace("{eventName}", "EmailVerified");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, eventUrl);
            request.Content = new StringContent("true", Encoding.UTF8, "application/json");

            var cl = clientFactory.CreateClient();
            var result = await cl.SendAsync(request);

            ViewBag.url = eventUrl;
            ViewBag.content = await result.Content.ReadAsStringAsync();
            ViewBag.resultCode = result.StatusCode;
            ViewBag.resultPhrase = result.ReasonPhrase;
            return View("VerifyEmail");
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
}
