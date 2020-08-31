using System;

namespace Andy.EmailClient
{
    public interface IEmailClient
    {
        bool TrySendEmail(string toAddress, string toName, string subject, string textBody, string htmlBody, out Exception exception);
    }
}