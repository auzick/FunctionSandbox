using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Azure.Management.ApplicationInsights.Management.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Newtonsoft.Json.Linq;

namespace Provision
{
    // https://docs.microsoft.com/en-us/rest/api/appservice/webapps/installsiteextension
    public class SiteExtension
    {
        public IAzureRestClient azureRestClient { get; private set; }

        public SiteExtension(IAzureRestClient client)
        {
            this.azureRestClient = client;
        }

        public Dictionary<string, string> Tags { get; private set; }
        public SiteExtension WithTags(Dictionary<string, string> tags)
        {
            this.Tags = tags;
            return this;
        }

        public string ExtensionName { get; private set; }
        public SiteExtension WithExtensionName(string name)
        {
            this.ExtensionName = name;
            return this;
        }

        public string SubscriptionId { get; private set; }
        public SiteExtension WithSubscriptionId(string id)
        {
            this.SubscriptionId = id;
            return this;
        }

        public IResourceGroup ResourceGroup { get; private set; }
        public SiteExtension WithResourceGroup(IResourceGroup group)
        {
            this.ResourceGroup = group;
            return this;
        }

        public string AssociatedApp { get; private set; }
        public SiteExtension WithAssociatedApp(string appName)
        {
            this.AssociatedApp = appName;
            return this;
        }

        public void Create()
        {
            var url = $"https://management.azure.com/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroup.Name}/providers/Microsoft.Web/sites/{AssociatedApp}/siteextensions/{ExtensionName}?api-version=2019-08-01";

            var response = azureRestClient.Put(url, new StringContent(""));
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"management.azure.com returned {response.StatusCode.ToString()}");
            }

            var responseBody = response.Content.ReadAsStringAsync().Result;

        }
    }
}