using System;
using Microsoft.ApplicationInsights;

namespace Andy.SmsClient
{
    public interface ISmsClient
    {
        bool TrySendSms(string to, string message, out Exception error);
    }
}