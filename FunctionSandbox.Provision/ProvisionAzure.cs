using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Storage.Fluent;
using System.Security.Cryptography;
// ApplicationInsights: No fluent for you

namespace Provision
{
    public interface IProvisionAzure
    {
        string Name { get; }
        string Location { get; }
        Dictionary<string, string> CommonTags { get; }

        IResourceGroup ResourceGroup { get; }
        IStorageAccount AppDataStorageAccount { get; }
        IAppServicePlan AppServicePlan { get; }
        IWebApp WebApp { get; }
        AppInsightsComponent WebAppInsights { get; }
        string WebAppHostName { get; }
        IStorageAccount FunctionStorageAccount { get; }
        AppInsightsComponent FunctionAppInsights { get; }
        IFunctionApp FunctionApp { get; }

        IServicePrincipal Principal { get; }
        ISettings Settings { get; }
        string RegisterFunctionUrl { get; }

        ProvisionAzure AddTag(string name, string value);
        ProvisionAzure Authenticate();
        ProvisionAzure CreateResources();
        ProvisionAzure WithDefaults();
        ProvisionAzure WithName(string name);
    }

    public class ProvisionAzure : IProvisionAzure
    {
        public string Name { get; private set; }
        public string Location { get; private set; }
        public Dictionary<string, string> CommonTags { get; private set; }

        public IResourceGroup ResourceGroup { get; private set; }
        public IStorageAccount AppDataStorageAccount { get; private set; }
        public IAppServicePlan AppServicePlan { get; private set; }

        public IWebApp WebApp { get; private set; }
        public AppInsightsComponent WebAppInsights { get; private set; }
        public string WebAppHostName => WebApp.HostNames.FirstOrDefault();

        public IStorageAccount FunctionStorageAccount { get; private set; }
        public AppInsightsComponent FunctionAppInsights { get; private set; }
        public IFunctionApp FunctionApp { get; private set; }
        public string FunctionAppHostName => FunctionApp.HostNames.FirstOrDefault();
        public string RegisterFunctionUrl { get; private set; }

        public ISettings Settings { get; private set; }
        public IServicePrincipal Principal { get; private set; }
        public IAzureRestClient AzureRestClient { get; private set; }

        //private AzureCredentials credentials;
        private IAzure azure;

        public ProvisionAzure(ISettings settings, IServicePrincipal principal, IAzureRestClient azureRestClient)
        {
            Principal = principal;
            Settings = settings;
            AzureRestClient = azureRestClient;
            CommonTags = new Dictionary<string, string>();
        }

        public ProvisionAzure WithName(string name)
        {
            Name = name;
            return this;
        }

        public ProvisionAzure WithDefaults()
        {
            Location = "East US";
            CommonTags.Add("environment", "development");
            return this;
        }

        public ProvisionAzure CreateResources()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                throw new MissingFieldException("The property 'Name' must be specified.");
            }

            if (azure.ResourceGroups.Contain(Name))
            {
                throw new Exception($"Resource group '{Name}' already exists");
            }

            Console.WriteLine($"Creating Azure resources");

            //Generate the key we'll use to build the function urls.
            var key = new byte[32];
            using (var generator = RandomNumberGenerator.Create())
            {
                generator.GetBytes(key);
            }
            string functionApiKey = Convert.ToBase64String(key);

            RegisterFunctionUrl = $"https://{Name}-functions.azurewebsites.net/api/register/?code={functionApiKey}";

            Console.Write($"Creating resource group '{Name}'... ");
            ResourceGroup = azure.ResourceGroups
                .Define(Name)
                .WithRegion(Location)
                .WithTags(CommonTags)
                .Create();
            Console.WriteLine("done");

            try
            {
                var appDataStorageAccountName = ScrubToAlpha($"{Name}storage");
                Console.Write($"Creating storage account '{appDataStorageAccountName}'... ");
                AppDataStorageAccount = azure.StorageAccounts
                    .Define(appDataStorageAccountName)
                    .WithRegion(Location)
                    .WithExistingResourceGroup(ResourceGroup)
                    .WithTags(CommonTags)
                    .Create();
                Console.WriteLine("done");

                Console.Write($"Creating app service plan '{Name}-appservice'... ");
                AppServicePlan = azure.AppServices.AppServicePlans
                    .Define($"{Name}-appservice")
                    .WithRegion(ResourceGroup.Region)
                    .WithExistingResourceGroup(ResourceGroup)
                    .WithFreePricingTier()
                    .WithTags(CommonTags)
                    .Create();
                Console.WriteLine("done");

                // Note: Azure Fluent does not support Application Insights yet.
                // AppInsights is a class in this project that kinda mimics how Azure Fluent works.
                Console.Write($"Creating application insights '{Name}-portal-insights'... ");
                WebAppInsights = new AppInsights(AzureRestClient)
                    .WithName($"{Name}-portal-insights")
                    .WithResourceGroup(ResourceGroup)
                    .WithSubscriptionId(Principal.SubscriptionId.ToString())
                    .WithTags(CommonTags)
                    .WithAssociatedApp($"{Name}-portal")
                   .Create();
                Console.WriteLine($"done, instrumentation key is {WebAppInsights.Properties.InstrumentationKey}");

                Console.Write($"Creating web app '{Name}-portal'... ");
                WebApp = azure.AppServices.WebApps
                    .Define($"{Name}-portal")
                    .WithExistingWindowsPlan(AppServicePlan)
                    .WithExistingResourceGroup(AppServicePlan.ResourceGroupName)
                    .WithTags(CommonTags)
                    .WithAppSetting("APPINSIGHTS_INSTRUMENTATIONKEY", WebAppInsights.Properties.InstrumentationKey)
                    .WithAppSetting("AzureStorage", AppDataStorageAccount.GetConnectionString())
                    .WithAppSetting("ASPNETCORE_ENVIRONMENT", "DEBUG")
                    // The URL of the function that kicks off the registration process.
                    .WithAppSetting("RegisterFunctionUrl", RegisterFunctionUrl)
                    // Running the app from a package zip, already uploaded to Azure storage in the project pipeline.
                    // If you create your own pipeline, see azure-pipelines.yml for an example how to do this.
                    // See https://docs.microsoft.com/en-us/azure/app-service/deploy-run-package
                    .WithAppSetting("WEBSITE_RUN_FROM_PACKAGE", "https://functionsandboxrepo.blob.core.windows.net/deploy/FunctionSandbox.Portal.zip")
                    .Create();
                Console.WriteLine("done");

                // Install the Application Insights extension                
                // Not sure this is needed. 
                // I've seen references that imply it is necessary for App Insights to track stuff automagically:
                //   https://winterdom.com/2017/08/01/aiarm
                //   https://github.com/tomasr/webapp-appinsights
                // However, this keeps throwing a 400 error with no explanation.
                // Tried it manually using https://docs.microsoft.com/en-us/rest/api/appservice/webapps/installsiteextension#code-try-0
                // and it throws a 400 there too.
                // Interestingly, this "Microsoft.ApplicationInsights.AzureWebSites" extension does not appear when listing site extensions 
                // for an existing web app. https://docs.microsoft.com/en-us/rest/api/appservice/webapps/listsiteextensions#code-try-0)
                // Maybe it's been added as part of a standard web app in version ~3?
                Console.Write($"Installing extension 'Microsoft.ApplicationInsights.AzureWebSites' on '{WebApp.Name}'... ");
                try
                {
                    new SiteExtension(AzureRestClient)
                        .WithAssociatedApp(WebApp.Name)
                        .WithExtensionName("Microsoft.ApplicationInsights.AzureWebSites")
                        .WithResourceGroup(ResourceGroup)
                        .WithSubscriptionId(Principal.SubscriptionId.ToString())
                        .WithTags(CommonTags)
                        .Create();
                    Console.WriteLine("done");
                }
                catch (Exception ex)
                {
                    ConsoleColor.Red.WriteLine($"failed, {ex.Message}");
                }

                var functionStorageAccountName = ScrubToAlpha($"{Name}storage");
                Console.Write($"Creating storage account '{functionStorageAccountName}'... ");
                FunctionStorageAccount = azure.StorageAccounts
                    .Define(functionStorageAccountName)
                    .WithRegion(Location)
                    .WithExistingResourceGroup(AppServicePlan.ResourceGroupName)
                    .Create();
                Console.WriteLine("done");

                // Note: Azure Fluent does not support Application Insights yet.
                // AppInsights is a class in this project that kinda mimics how Azure Fluent works.
                Console.Write($"Creating application insights '{Name}-functions-insights'... ");
                FunctionAppInsights = new AppInsights(AzureRestClient)
                    .WithName($"{Name}-functions-insights")
                    .WithResourceGroup(ResourceGroup)
                    .WithSubscriptionId(Principal.SubscriptionId.ToString())
                    .WithTags(CommonTags)
                    .WithAssociatedApp($"{Name}-functions")
                    .Create();
                Console.WriteLine($"done, instrumentation key is {FunctionAppInsights.Properties.InstrumentationKey}");

                Console.Write($"Creating function app '{Name}-functions'... ");
                FunctionApp = azure.AppServices.FunctionApps
                    .Define($"{Name}-functions")
                    .WithExistingAppServicePlan(AppServicePlan)
                    .WithExistingResourceGroup(AppServicePlan.ResourceGroupName)
                    .WithExistingStorageAccount(FunctionStorageAccount)
                    .WithRuntimeVersion("~3")
                    .WithNetFrameworkVersion(NetFrameworkVersion.V4_6)
                    // Running the app from a package zip, already uploaded to Azure storage in the project pipeline.
                    // If you create your own pipeline, see azure-pipelines.yml for an example how to do this.
                    // See https://docs.microsoft.com/en-us/azure/app-service/deploy-run-package
                    .WithAppSetting("WEBSITE_RUN_FROM_PACKAGE", "https://functionsandboxrepo.blob.core.windows.net/deploy/FunctionSandbox.Functions.zip")
                    .WithAppSetting("AzureWebJobsStorage", FunctionStorageAccount.GetConnectionString())
                    .WithAppSetting("APPINSIGHTS_INSTRUMENTATIONKEY", FunctionAppInsights.Properties.InstrumentationKey)
                    .WithAppSetting("FUNCTIONS_WORKER_RUNTIME", "dotnet")
                    .WithAppSetting("AzureStorage", AppDataStorageAccount.GetConnectionString())
                    .WithAppSetting("TwilioAccountSid", Settings.TwilioAccountSid)
                    .WithAppSetting("TwilioAuthToken", Settings.TwilioAuthToken)
                    .WithAppSetting("TwilioFromNumber", Settings.TwilioFromNumber)
                    .WithAppSetting("SendgridApiKey", Settings.SendgridApiKey)
                    .WithAppSetting("EmailFromAddress", Settings.EmailFromAddress)
                    .WithAppSetting("SmsVerifyUrl", "https://" + WebAppHostName + "/VerifySms/{0}")
                    .WithAppSetting("EmailVerifyUrl", "https://" + WebAppHostName + "/VerifyEmail/{0}")
                    // Necessary because the FunctionApp APIs that deal with keys (like FunctionApp.GetMasterKey()) 
                    // won't work with the default (blob) storage. 
                    // See https://github.com/Azure/azure-functions-host/wiki/Changes-to-Key-Management-in-Functions-V2
                    // This affects both ARM and Fluent.
                    .WithAppSetting("AzureWebJobsSecretStorageType", "Files")
                    .Create();
                Console.WriteLine("done");

                Console.Write($"Adding the function key to 'StartRegister'... ");
                FunctionApp.AddFunctionKey("StartRegister", "PortalKey", functionApiKey);
                Console.WriteLine("done");

                // Console.Write($"Updating WebApp settings... ");
                // // Update the WebApp with a setting so it has the "Register function" URL.
                // // I'm not real thrilled about this. I'd rather add an appsetting with a pre-generated key
                // // to the WebApp at the same time I'm creating it, and then add that same key to the function
                // // in this function app using: 
                // //    FunctionApp.AddFunctionKey(functionName, keyName, keyValue)
                // // But to do that, the function would have to have already been deployed.
                // // Instead, we're using the master key... not a really secure thing.
                // var masterKey = FunctionApp.GetMasterKey();
                // RegisterFunctionUrl = $"https://{FunctionAppHostName}/api/register/?code={masterKey}";
                // WebApp.Update()
                //     .WithAppSetting("RegisterFunctionUrl", RegisterFunctionUrl)
                //     .Apply();
                // Console.WriteLine("done");
            }
            catch (Exception ex)
            {
                ConsoleColor.Red.WriteLine(ex.Message);
                Console.WriteLine("Rolling back...");
                azure.ResourceGroups.DeleteByName(ResourceGroup.Name);
                throw ex;
            }

            Console.WriteLine("Finished creating Azure resources.");

            return this;
        }

        private static readonly Regex alphaRegex = new Regex("[^a-zA-Z0-9]");
        private string ScrubToAlpha(string text) => alphaRegex.Replace(text, String.Empty);

        public ProvisionAzure Authenticate()
        {
            var credentials = SdkContext.AzureCredentialsFactory
                .FromServicePrincipal(
                    Principal.ClientId.ToString(),
                    Principal.ClientSecret.ToString(),
                    Principal.TenantId.ToString(),
                    AzureEnvironment.AzureGlobalCloud);

            try
            {
                azure = Microsoft.Azure.Management.Fluent.Azure.Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithDefaultSubscription();
            }
            catch (Microsoft.IdentityModel.Clients.ActiveDirectory.AdalServiceException)
            {
                Console.WriteLine("The credentials in the file 'azureauth.json' are invalid. The client secret may have expired.");
                Console.WriteLine("Using the Powershell AZ module (or from the Azure CLI):");
                Console.WriteLine("  1. Log in to correct Azure subscription azure (az login)");
                Console.WriteLine("  2. Make sure you are in the correct subscription (az account show)");
                Console.WriteLine("  3. Run this command:");
                Console.WriteLine("     az ad sp create-for-rbac --sdk-auth");
                Console.WriteLine("  4. Copy/paste the json response to the file 'azureauth.json' in this application's directory.");
            }
            return this;
        }

        public ProvisionAzure AddTag(string name, string value)
        {
            CommonTags.Add(name, value);
            return this;
        }

        // private void WriteLineRed(string text)
        // {
        //     var color = Console.ForegroundColor;
        //     Console.ForegroundColor = ConsoleColor.Red;
        //     Console.WriteLine(text);
        //     Console.ForegroundColor = color;
        // }

    }
}