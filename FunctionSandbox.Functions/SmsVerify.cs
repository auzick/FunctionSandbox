// using System;
// using System.IO;
// using System.Threading.Tasks;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Azure.WebJobs;
// using Microsoft.Azure.WebJobs.Extensions.Http;
// using Microsoft.AspNetCore.Http;
// using Microsoft.Extensions.Logging;
// using Newtonsoft.Json;
// using Microsoft.Azure.WebJobs.Extensions.DurableTask;

// namespace Andy.Functions
// {
//     public static class SmsVerify
//     {
//         [FunctionName("SmsVerify")]
//         public static async Task<IActionResult> Run(
//             [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "smsverify/{instanceId}")] HttpRequest req,
//             string instanceId,
//             ILogger log,
//             [DurableClient] IDurableOrchestrationClient client
//         )
//         {
//             log.LogInformation("SmsVerify called.");
//             await client.RaiseEventAsync(instanceId, "SmsVerify", true);

//             string name = req.Query["name"];

//             string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
//             dynamic data = JsonConvert.DeserializeObject(requestBody);
//             name = name ?? data?.name;

//             string responseMessage = string.IsNullOrEmpty(name)
//                 ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
//                 : $"Hello, {name}. This HTTP triggered function executed successfully.";

//             return new OkObjectResult(responseMessage);
//         }
//     }
// }
