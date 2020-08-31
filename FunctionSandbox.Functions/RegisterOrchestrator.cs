using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using Andy.AzureStorage.Tables;
using Andy.Model;

namespace Andy.Functions
{
    public class RegisterOrchestrator
    {
        private readonly TelemetryClient telemetry;
        private ILogger log;

        public RegisterOrchestrator(TelemetryClient telemetryClient)
        {
            this.telemetry = telemetryClient;
        }

        [FunctionName("RegisterOrchestrator")]
        public async Task Run(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger logger
        )
        {
            log = functionContext.CreateReplaySafeLogger(logger);
            var data = functionContext.GetInput<string>();

            //----------------------------------------------------------------
            // Verify Email

            log.LogInformation("Calling SendEmailActivity");
            functionContext.SetCustomStatus("SendingEmail");
            try
            {
                data = await functionContext.CallActivityAsync<string>(
                    "SendEmailActivity",
                    data
                );
            }
            catch (Exception)
            {
                log.LogInformation("SendEmailActivity failed; exiting.");
                return;
            }

            log.LogInformation("Waiting for Email verification");
            functionContext.SetCustomStatus("AwaitingEmailVerification");
            bool emailVerified = await functionContext.WaitForExternalEvent<bool>("EmailVerified");
            log.LogInformation("Email verification received");


            //----------------------------------------------------------------
            // Verify SMS

            log.LogInformation("Calling SendSmsActivity");
            functionContext.SetCustomStatus("SendingSMS");
            try
            {
                data = await functionContext.CallActivityAsync<string>(
                    "SendSmsActivity",
                    data
                );
            }
            catch (Exception)
            {
                log.LogInformation("SendSmsActivity failed; exiting.");
                return;
            }

            log.LogInformation("Waiting for SMS verification");
            functionContext.SetCustomStatus("AwaitingSmsVerification");
            bool smsVerified = await functionContext.WaitForExternalEvent<bool>("SmsVerified");
            log.LogInformation("Sms verification received");


            // -----------------------------------------------------
            // Clean up
            var reg = UserRegistration.FromJson(data);
            new OrchestrationEntity().Delete("RegisterOrchestrator", functionContext.InstanceId);
            new RegistrationEntity().Delete("RegisterOrchestrator", functionContext.InstanceId);
            log.LogInformation("Orchestrator done");

        }
    }
}