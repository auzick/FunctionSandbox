using System.Text.Json;
using Andy.Model;
using PhoneNumbers;

namespace Andy
{
    public static class StringExtensions
    {
        public static UserRegistration ToUserRegistration(this string json)
        {
            return JsonSerializer.Deserialize<UserRegistration>(json);
        }
        public static string ToE164(this string number)
        {
            var util = PhoneNumberUtil.GetInstance();
            var pn = util.Parse(number, "US");
            return util.Format(pn, PhoneNumberFormat.E164);
        }

    }
}