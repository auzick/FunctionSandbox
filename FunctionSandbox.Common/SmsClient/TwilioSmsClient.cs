using System;
using System.Text.Json;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using static Twilio.Rest.Api.V2010.Account.Call.FeedbackSummaryResource;

namespace Andy.SmsClient
{
    public class TwilioSmsClient : ISmsClient
    {
        public const int MaxSmsLength = 140;

        public string FromNumber { get; set; }

        public TwilioSmsClient()
        { }

        public TwilioSmsClient(string sid, string token, string from)
        {
            this.FromNumber = from;
            TwilioClient.Init(sid, token);
        }

        public bool TrySendSms(string to, string message, out Exception exception)
        {
            try
            {
                MessageResource result = MessageResource.Create(
                    @from: new Twilio.Types.PhoneNumber(FromNumber),
                    to: new Twilio.Types.PhoneNumber(to.ToE164()),
                    body: message.Length > MaxSmsLength ? message.Substring(0, MaxSmsLength) : message
                );
                if (result.Status == StatusEnum.Failed)
                {
                    exception = new Exception($"Twilio MessageResource.Create failed with error code {result.ErrorCode}");
                    return false;
                }
                exception = null;
                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
        }
    }
}