using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Provision
{
    public interface IServicePrincipal
    {
        Guid ClientId { get; set; }
        Guid ClientSecret { get; set; }
        Guid SubscriptionId { get; set; }
        Guid TenantId { get; set; }
        Uri ActiveDirectoryEndpointUrl { get; set; }
        Uri ResourceManagerEndpointUrl { get; set; }
        Uri ActiveDirectoryGraphResourceId { get; set; }
        Uri SqlManagementEndpointUrl { get; set; }
        Uri GalleryEndpointUrl { get; set; }
        Uri ManagementEndpointUrl { get; set; }
    }

    public class ServicePrincipal : IServicePrincipal
    {
        [JsonPropertyName("clientId")]
        public Guid ClientId { get; set; }

        [JsonPropertyName("clientSecret")]
        public Guid ClientSecret { get; set; }

        [JsonPropertyName("subscriptionId")]
        public Guid SubscriptionId { get; set; }

        [JsonPropertyName("tenantId")]
        public Guid TenantId { get; set; }

        [JsonPropertyName("activeDirectoryEndpointUrl")]
        public Uri ActiveDirectoryEndpointUrl { get; set; }

        [JsonPropertyName("resourceManagerEndpointUrl")]
        public Uri ResourceManagerEndpointUrl { get; set; }

        [JsonPropertyName("activeDirectoryGraphResourceId")]
        public Uri ActiveDirectoryGraphResourceId { get; set; }

        [JsonPropertyName("sqlManagementEndpointUrl")]
        public Uri SqlManagementEndpointUrl { get; set; }

        [JsonPropertyName("galleryEndpointUrl")]
        public Uri GalleryEndpointUrl { get; set; }

        [JsonPropertyName("managementEndpointUrl")]
        public Uri ManagementEndpointUrl { get; set; }

        public static ServicePrincipal Load()
        {
            var appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var authPath = Path.Combine(appPath, "azureauth.json");

            if (!File.Exists(authPath))
            {
                Console.WriteLine("An Azure Service Principal auth file is required.");
                Console.WriteLine("Using the Powershell AZ module (or from the Azure CLI):");
                Console.WriteLine("  1. Log in to correct Azure subscription azure (az login)");
                Console.WriteLine("  2. Make sure you are in the correct subscription (az account show)");
                Console.WriteLine("  3. Run this command:");
                Console.WriteLine("     az ad sp create-for-rbac --sdk-auth");
                Console.WriteLine("  4. Copy/paste the json response to the file 'azureauth.json' in this application's directory.");
                throw (new AccessViolationException("An Azure Service Principal auth file is required."));
            }

            return JsonSerializer.Deserialize<ServicePrincipal>(File.ReadAllText(authPath));

        }

    }
}