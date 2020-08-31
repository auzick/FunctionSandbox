using System;
using System.Net;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Andy.EmailClient
{
    public class SendGridEmailClient : IEmailClient
    {
        public string ApiKey { get; set; }
        public string FromAddress { get; set; }

        public SendGridEmailClient(string apiKey, string fromAddress)
        {
            ApiKey = apiKey;
            FromAddress = fromAddress;
        }

        public bool TrySendEmail(
            string toAddress,
            string toName,
            string subject,
            string textBody,
            string htmlBody,
            out Exception exception)
        {
            var client = new SendGridClient(ApiKey);
            var from = new EmailAddress(FromAddress, "Function Sandbox");
            var to = new EmailAddress(toAddress, toName);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, textBody, htmlBody);

            var response = Task.Run(async () =>
            {
                var result = await client.SendEmailAsync(msg);
                return result;
            }).Result;

            if (((int)response.StatusCode >= 200) && ((int)response.StatusCode <= 299))
            {
                exception = null;
                return true;
            }
            else
            {
                exception = new Exception($"SendGrid SendEmailAsync failed with status code {response.StatusCode.ToString()}");
                return false;
            }
        }
    }
}