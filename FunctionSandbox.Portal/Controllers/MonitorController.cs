using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Andy.AzureStorage.Tables;
using Andy.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Andy.Portal.Controllers
{
    public class MonitorController : Controller
    {
        private readonly ILogger<MonitorController> log;
        private readonly IHttpClientFactory clientFactory;

        public MonitorController(
            ILogger<MonitorController> logger,
            IHttpClientFactory clientFactory,
            IConfiguration config
        )
        {
            log = logger;
            this.clientFactory = clientFactory;
            Environment.SetEnvironmentVariable("AzureStorage", config.GetValue<string>("AzureStorage"));
            Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", config.GetValue<string>("APPINSIGHTS_INSTRUMENTATIONKEY"));
        }

        public async Task<IActionResult> Index()
        {
            var orchestrations = new OrchestrationEntity().FetchAll();
            var sb = new StringBuilder();
            foreach (var orchestration in orchestrations)
            {
                var cl = clientFactory.CreateClient();

                var statusRequest = new HttpRequestMessage(HttpMethod.Get, orchestration.StatusQueryGetUri);
                var statusResult = await cl.SendAsync(statusRequest);
                var status = OrchestrationStatus.FromJson(await statusResult.Content.ReadAsStringAsync());

                var reg = new RegistrationEntity().Fetch(status.name, status.instanceId);

                sb.Append($"<tr>");
                sb.Append($"<td>{reg?.UserName ?? "unknown"}</td>");
                sb.Append($"<td>{reg?.UserEmail ?? "unknown"}</td>");
                sb.Append($"<td>{HasVerified(reg?.EmailLastVerified ?? TableEntityBase.MinDate, "EmailVerified", orchestration)}</td>");
                sb.Append($"<td>{reg?.UserPhone ?? "unknown"}</td>");
                sb.Append($"<td>{HasVerified(reg?.PhoneLastVerified ?? TableEntityBase.MinDate, "SmsVerified", orchestration)}</td>");
                sb.Append($"<td>{status.customStatus}</td>");
                sb.Append($"<td>{status.runtimeStatus}</td>");
                sb.Append($"</tr>");
            }
            ViewBag.rows = sb.ToString();

            return View("Index");
        }

        private string HasVerified(DateTime dateVerified, string eventName, OrchestrationEntity orchestration)
        {
            var hasVerified = dateVerified > TableEntityBase.MinDate;
            if (hasVerified)
            {
                return dateVerified.ToString("g");
            }
            var url = orchestration.SendEventPostUri.Replace("{eventName}", eventName);
            return $"<b>no</b> <a href=\"javascript:fEvent('{url}', 'true');\"s>verify</a>";
        }

        // private static string HasVerified(DateTime date)
        // {
        //     return date > TableEntityBase.MinDate ? "yes" : "no";
        // }

    }
}