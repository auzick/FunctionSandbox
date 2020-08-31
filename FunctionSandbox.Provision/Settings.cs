using System.Reflection;
using System.IO;
using System.Text.Json;

namespace Provision
{
    public interface ISettings
    {
        string ResourceGroupName { get; set; }
        string TwilioAccountSid { get; set; }
        string TwilioAuthToken { get; set; }
        string TwilioFromNumber { get; set; }
        string SendgridApiKey { get; set; }
        string EmailFromAddress { get; set; }

    }

    public class Settings : ISettings
    {
        public string ResourceGroupName { get; set; }
        public string TwilioAccountSid { get; set; }
        public string TwilioAuthToken { get; set; }
        public string TwilioFromNumber { get; set; }
        public string SendgridApiKey { get; set; }
        public string EmailFromAddress { get; set; }

        public static Settings LoadFromFile()
        {
            var appPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var settingsPath = Path.Combine(appPath, "provisionSettings.json");
            if (!File.Exists(settingsPath))
            {
                var settings = new Settings();
                File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings));
                return settings;
            }
            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<Settings>(json);
        }

        public void Save()
        {
            var appPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var settingsPath = Path.Combine(appPath, "provisionSettings.json");
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(this));
        }

        public Settings()
        {
        }

    }
}