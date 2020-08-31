using Microsoft.ApplicationInsights;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Andy.Model;
using Andy.EmailClient;
using System;
using System.Text.Json;

namespace Andy.Functions
{
    public class SendEmailActivity
    {
        private readonly TelemetryClient telemetry;
        private ILogger log;
        private IEmailClient emailClient;

        public SendEmailActivity(
            TelemetryClient telemetryClient,
            IEmailClient email
        )
        {
            this.telemetry = telemetryClient;
            this.emailClient = email;
        }

        [FunctionName("SendEmailActivity")]
        public string Run(
            [ActivityTrigger] IDurableActivityContext functionContext,
            ILogger logger
        )
        {
            log = logger;
            log.LogInformation($"SendEmail activity running");

            var data = functionContext.GetInput<string>();
            var registration = JsonSerializer.Deserialize<UserRegistration>(data);

            var url = String.Format(
                System.Environment.GetEnvironmentVariable("EmailVerifyUrl"),
                functionContext.InstanceId
            );


            string textBody = $"Please use thus url to verify your email address: {url}";
            string htmlBody = $"<p>Please <a href='{url}'>verify your email<a></p>";

            log.LogInformation($"Sending Email");
            if (emailClient.TrySendEmail(
                registration.UserEmail,
                registration.UserName,
                $"Please verify your email address {functionContext.InstanceId}",
                textBody,
                htmlBody,
                out Exception sendEx
            ))
            {
                log.LogInformation(sendEx, $"Email sent");
                registration.EmailLastVerified = DateTime.Now;
                telemetry.TrackEvent(
                    "RegistrationEmailSent",
                    new Dictionary<string, string>() {
                        { "UserName", registration.UserName },
                        { "UserEmail", registration.UserEmail }
                    }
                );
            }
            else
            {
                log.LogError(sendEx, $"Email client threw an error failed. {sendEx.Message}");
                telemetry.TrackException(
                    sendEx,
                    new Dictionary<string, string>() {
                        { "UserName", registration.UserName },
                        { "UserPhone", registration.UserPhone }
                    }
                );
                throw sendEx;
            }

            return registration.ToString();
        }

    }
}