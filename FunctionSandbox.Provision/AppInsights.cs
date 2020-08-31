using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Azure.Management.ApplicationInsights.Management.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Newtonsoft.Json.Linq;

namespace Provision
{
    // https://docs.microsoft.com/en-us/rest/api/application-insights/components/createorupdate
    // https://docs.microsoft.com/en-us/azure/templates/microsoft.insights/components
    public class AppInsights
    {
        public IAzureRestClient azureRestClient { get; private set; }

        public AppInsights(IAzureRestClient client)
        {
            this.azureRestClient = client;
        }

        public Dictionary<string, string> Tags { get; private set; }
        public AppInsights WithTags(Dictionary<string, string> tags)
        {
            this.Tags = tags;
            return this;
        }

        public string Name { get; private set; }
        public AppInsights WithName(string name)
        {
            this.Name = name;
            return this;
        }

        public string SubscriptionId { get; private set; }
        public AppInsights WithSubscriptionId(string id)
        {
            this.SubscriptionId = id;
            return this;
        }

        public IResourceGroup ResourceGroup { get; private set; }
        public AppInsights WithResourceGroup(IResourceGroup group)
        {
            this.ResourceGroup = group;
            return this;
        }

        public string AssociatedApp { get; private set; }
        public AppInsights WithAssociatedApp(string appName)
        {
            this.AssociatedApp = appName;
            return this;
        }

        public AppInsightsComponent Create()
        {
            // initialize tags
            var tags = new Dictionary<string, string>(Tags);
            if (!string.IsNullOrWhiteSpace(AssociatedApp))
            {
                // Add a tag to associate the App Insights instance with the app service
                // ARM syntax for this: "[concat('hidden-link:', resourceGroup().id, '/providers/Microsoft.Web/sites/', parameters('webSiteName'))]": "Resource",
                tags.Add($"hidden-link:{ResourceGroup.Id}/providers/Microsoft.Web/sites/{Name}-portal", "Resource");
            }

            // Create the resource

            var url = $"https://management.azure.com/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroup.Name}/providers/Microsoft.Insights/components/{Name}?api-version=2015-05-01";

            dynamic reqBody = new JObject();
            reqBody.Location = ResourceGroup.RegionName;
            reqBody.Kind = "web";
            reqBody.tags = JObject.FromObject(tags);
            dynamic properties = new JObject();
            properties.ApplicationType = "web";
            properties.FlowType = "Bluefield";
            properties.RequestSource = "rest";
            reqBody.Properties = properties;

            var response = azureRestClient.Put(url, reqBody);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"management.azure.com returned {response.StatusCode.ToString()}");
            }

            var responseBody = response.Content.ReadAsStringAsync().Result;
            return Newtonsoft.Json.JsonConvert.DeserializeObject<AppInsightsComponent>(responseBody);

        }

    }
}