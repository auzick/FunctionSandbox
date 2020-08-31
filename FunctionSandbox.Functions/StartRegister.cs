using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.ApplicationInsights;
using System.Collections.Generic;
using Andy.Model;
using System.Text.Json;
using Andy.AzureStorage.Tables;

namespace Andy.Functions
{
    public class StartRegister
    {
        private readonly TelemetryClient telemetry;
        private ILogger log;

        public StartRegister(TelemetryClient telemetryClient)
        {
            this.telemetry = telemetryClient;
        }

        [FunctionName("StartRegister")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "register/")] HttpRequest req,
            ILogger logger,
            [DurableClient] IDurableOrchestrationClient starter
        )
        {
            this.log = logger;
            log.LogInformation("Received registration start request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = UserRegistration.FromJson(requestBody);


            telemetry.TrackEvent(
                "RegistrationStarted",
                new Dictionary<string, string>() {
                    { "UserName", data.UserName },
                    { "UserEmail", data.UserEmail },
                    { "UserPhone", data.UserPhone }
                }
            );

            var instanceId = Guid.NewGuid().ToString();

            new RegistrationEntity(
                "RegisterOrchestrator",
                instanceId,
                data
            ).Save();

            await starter.StartNewAsync<string>(
                "RegisterOrchestrator",
                instanceId,
                data.ToString()
                );

            var managementPayload = starter.CreateHttpManagementPayload(instanceId);

            log.LogInformation("Saving entity");

            new OrchestrationEntity(
                "RegisterOrchestrator",
                DateTime.Now,
                managementPayload
            ).Save();

            return new ContentResult()
            {
                Content = JsonSerializer.Serialize(managementPayload),
                ContentType = "application/json",
                StatusCode = StatusCodes.Status202Accepted
            };

        }
    }
}
