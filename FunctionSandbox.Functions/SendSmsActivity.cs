using Microsoft.ApplicationInsights;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Andy.Model;
using Andy.SmsClient;
using System;
using System.Text.Json;

namespace Andy.Functions
{
    public class SendSmsActivity
    {
        private readonly TelemetryClient telemetry;
        private ILogger log;
        private ISmsClient smsClient;

        public SendSmsActivity(
            TelemetryClient telemetryClient,
            ISmsClient sms
        )
        {
            this.telemetry = telemetryClient;
            this.smsClient = sms;
        }

        [FunctionName("SendSmsActivity")]
        public string Run(
            [ActivityTrigger] IDurableActivityContext functionContext,
            ILogger logger
        )
        {
            log = logger;
            log.LogInformation($"SendSms activity running");

            var data = functionContext.GetInput<string>();
            var registration = JsonSerializer.Deserialize<UserRegistration>(data);

            var url = String.Format(
                System.Environment.GetEnvironmentVariable("SmsVerifyUrl"),
                functionContext.InstanceId
            );

            log.LogInformation($"Sending Sms");

            Exception sendEx = null;

            if (smsClient.TrySendSms(registration.UserPhone, $"Verify your phone number: {url}", out sendEx))
            {
                registration.PhoneLastVerified = DateTime.Now;
                telemetry.TrackEvent(
                    "RegistrationSmsSent",
                    new Dictionary<string, string>() {
                        { "UserName", registration.UserName },
                        { "UserPhone", registration.UserPhone }
                    }
                );
            }
            else
            {
                log.LogError(sendEx, $"SMS client threw an error failed. {sendEx.Message}");
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