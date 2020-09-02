using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine.DragonFruit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Provision
{
    class Program
    {
        private static IServiceProvider _serviceProvider;
        private static bool _writeConfigs = false;

        private static Settings _settings;

        /// <summary>Provisions Azure resources for the FunctionDemo app.</summary>
        /// <param name="name">The resource group name to create</param>
        /// <param name="twilioAccountSid">Account SID for Twilio https://support.twilio.com/hc/en-us/articles/223136027-Auth-Tokens-and-How-to-Change-Them</param>
        /// <param name="twilioAuthToken">Authorization token for Twilio https://support.twilio.com/hc/en-us/articles/223136027-Auth-Tokens-and-How-to-Change-Them</param>
        /// <param name="twilioFromNumber">Twilio managed number for sending SMS</param>
        /// <param name="sendgridApiKey">SendGrid API key</param>
        /// <param name="emailFromAddress">From address for sending emails.</param>
        /// <param name="quiet">Run quietly?</param>
        /// <param name="writeConfigs">Write config files?</param>
        static void Main(
            string name = "",
            string twilioAccountSid = "",
            string twilioAuthToken = "",
            string twilioFromNumber = "",
            string sendgridApiKey = "",
            string emailFromAddress = "",
            bool quiet = false,
            bool writeConfigs = false
            )
        {
            _settings = Settings.LoadFromFile();
            if (!string.IsNullOrWhiteSpace(name)) { _settings.ResourceGroupName = name; }
            if (!string.IsNullOrWhiteSpace(twilioAccountSid)) { _settings.TwilioAccountSid = twilioAccountSid; }
            if (!string.IsNullOrWhiteSpace(twilioAuthToken)) { _settings.TwilioAuthToken = twilioAuthToken; }
            if (!string.IsNullOrWhiteSpace(twilioFromNumber)) { _settings.TwilioFromNumber = twilioFromNumber; }
            if (!string.IsNullOrWhiteSpace(sendgridApiKey)) { _settings.SendgridApiKey = sendgridApiKey; }
            if (!string.IsNullOrWhiteSpace(emailFromAddress)) { _settings.EmailFromAddress = emailFromAddress; }

            if (!quiet) { PromptSettings(); }
            _settings.Save();
            if (string.IsNullOrWhiteSpace(_settings.ResourceGroupName)) { throw new ArgumentNullException("Resource group name must be specified."); }

            _serviceProvider = ConfigureServices();

            var logger = _serviceProvider.GetService<ILoggerFactory>()
                .CreateLogger<Program>();

            var provisioner = _serviceProvider.GetService<IProvisionAzure>()
                .WithName(_settings.ResourceGroupName)
                .WithDefaults()
                .Authenticate()
                .CreateResources()
                ;

            if (_writeConfigs) WriteConfigs(provisioner);

            DisposeServices();

        }

        private static void WriteConfigs(ProvisionAzure provisioner)
        {
            //var solutionDir = Directory.GetParent(Directory.GetCurrentDirectory());
            var solutionDir = Directory.GetCurrentDirectory();

            var pDir = Path.Combine(solutionDir.ToString(), "FunctionSandbox.Portal");
            if (!Directory.Exists(pDir))
            {
                ConsoleColor.Red.WriteLine($"Cannot write portal config. The directory '{pDir}' does not exist."); ;
            }
            else
            {
                var configFile = Path.Combine(pDir, "appsettings.Development.json");
                dynamic config = new JObject();
                if (File.Exists(configFile))
                {
                    config = JsonConvert.DeserializeObject(File.ReadAllText(configFile));
                }
                config.APPINSIGHTS_INSTRUMENTATIONKEY = provisioner.FunctionAppInsights.Properties.InstrumentationKey;
                config.AzureStorage = provisioner.AppDataStorageAccount.GetConnectionString();
                config.FunctionsHostname = provisioner.FunctionAppHostName;
                config.RegisterFunctionUrl = provisioner.RegisterFunctionUrl;
                var updatedSettings = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configFile, updatedSettings);
                Console.WriteLine($"Wrote portal config to '{configFile}'");
            }

            var fDir = Path.Combine(solutionDir.ToString(), "FunctionSandbox.Functions");
            if (!Directory.Exists(fDir))
            {
                ConsoleColor.Red.WriteLine($"Cannot write function config. The directory '{fDir}' does not exist."); ;
            }
            else
            {
                var configFile = Path.Combine(fDir, "local.settings.json");
                dynamic config = new JObject();
                if (File.Exists(configFile))
                {
                    config = JsonConvert.DeserializeObject(File.ReadAllText(configFile));
                }
                config.Values.AzureWebJobsStorage = provisioner.FunctionStorageAccount.GetConnectionString();
                config.Values.APPINSIGHTS_INSTRUMENTATIONKEY = provisioner.FunctionAppInsights.Properties.InstrumentationKey;
                config.Values.AzureStorage = provisioner.AppDataStorageAccount.GetConnectionString();
                config.Values.TwilioAccountSid = _settings.TwilioAccountSid;
                config.Values.TwilioAuthToken = _settings.TwilioAuthToken;
                config.Values.TwilioFromNumber = _settings.TwilioFromNumber;
                config.Values.SmsVerifyUrl = $"https://{provisioner.WebAppHostName}/VerifySms/{{0}}";
                config.Values.SENDGRIP_API_KEY = _settings.SendgridApiKey;
                config.Values.EmailFromAddress = _settings.EmailFromAddress;
                config.Values.EmailVerifyUrl = $"https://{provisioner.WebAppHostName}/VerifyEmail/{{0}}";
                var updatedSettings = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configFile, updatedSettings);
                Console.WriteLine($"Wrote function app config to '{configFile}'");
            }


        }

        private static IServiceProvider ConfigureServices()
        {
            var sp = ServicePrincipal.Load();
            return new ServiceCollection()
                .AddLogging(b => b.AddConsole())
                .AddSingleton<ISettings>(s => _settings)
                .AddSingleton<IServicePrincipal>(p => ServicePrincipal.Load())
                .AddSingleton<IAzureRestClient>(p =>
                    new AzureRestClient(
                        p.GetRequiredService<IServicePrincipal>()
                    ).Authenticate())
                .AddSingleton<IProvisionAzure>(p =>
                    new ProvisionAzure(
                        p.GetRequiredService<ISettings>(),
                        p.GetRequiredService<IServicePrincipal>(),
                        p.GetRequiredService<IAzureRestClient>()
                        )
                    )
                .BuildServiceProvider();
        }

        private static void DisposeServices()
        {

            if (_serviceProvider == null)
            {
                return;
            }
            if (_serviceProvider is IDisposable)
            {
                ((IDisposable)_serviceProvider).Dispose();
            }
        }

        private static void PromptSettings()
        {
            _settings.ResourceGroupName = Prompt("Resource group name", _settings.ResourceGroupName);
            _settings.TwilioAccountSid = Prompt("Twilio account SID", _settings.TwilioAccountSid);
            _settings.TwilioAuthToken = Prompt("Twilio auth token", _settings.TwilioAuthToken);
            _settings.TwilioFromNumber = Prompt("Twilio number for sending SMS", _settings.TwilioFromNumber);
            _settings.SendgridApiKey = Prompt("SendGrid API key", _settings.SendgridApiKey);
            _settings.EmailFromAddress = Prompt("Email from address", _settings.EmailFromAddress);
            _writeConfigs = Prompt("Write config files?", _writeConfigs);
        }

        private static string Prompt(string prompt, string defaultValue)
        {
            var result = string.Empty;
            while (string.IsNullOrWhiteSpace(result))
            {
                Console.Write(prompt);
                if (!string.IsNullOrWhiteSpace(defaultValue)) { ConsoleColor.DarkGray.Write($" ({defaultValue})"); }
                Console.Write($": ");
                var response = Console.ReadLine();
                result = string.IsNullOrWhiteSpace(response) ? defaultValue : response;
            }
            return result;
        }

        private static bool Prompt(string prompt, bool? defaultValue)
        {
            bool? result = null;
            while (result == null)
            {
                Console.Write(prompt);
                if (defaultValue != null) { ConsoleColor.DarkGray.Write($" ({defaultValue.ToString()})"); }
                Console.Write($": ");
                var response = Console.ReadLine();
                if (response == "y" || response == "yes") { response = "true"; }
                if (response == "n" || response == "no") { response = "false"; }
                if (string.IsNullOrWhiteSpace(response)) { response = defaultValue.ToString(); }
                if (bool.TryParse(response, out bool validResponse))
                {
                    result = validResponse;
                }
            }
            return (bool)result;
        }

    }
}
