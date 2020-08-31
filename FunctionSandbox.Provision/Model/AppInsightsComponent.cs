using System;
using Newtonsoft.Json;

namespace Provision
{

    public partial class AppInsightsComponent
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("etag")]
        public string Etag { get; set; }

        [JsonProperty("properties")]
        public Properties Properties { get; set; }
    }

    public partial class Properties
    {
        [JsonProperty("Ver")]
        public string Ver { get; set; }

        [JsonProperty("ApplicationId")]
        public string ApplicationId { get; set; }

        [JsonProperty("AppId")]
        public string AppId { get; set; }

        [JsonProperty("Application_Type")]
        public string ApplicationType { get; set; }

        [JsonProperty("Flow_Type")]
        public string FlowType { get; set; }

        [JsonProperty("Request_Source")]
        public string RequestSource { get; set; }

        [JsonProperty("InstrumentationKey")]
        public string InstrumentationKey { get; set; }

        [JsonProperty("ConnectionString")]
        public string ConnectionString { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("CreationDate")]
        public DateTimeOffset CreationDate { get; set; }

        [JsonProperty("TenantId")]
        public Guid TenantId { get; set; }

        [JsonProperty("provisioningState")]
        public string ProvisioningState { get; set; }

        [JsonProperty("SamplingPercentage")]
        public string SamplingPercentage { get; set; }

        [JsonProperty("RetentionInDays")]
        public long RetentionInDays { get; set; }

        [JsonProperty("IngestionMode")]
        public string IngestionMode { get; set; }

        [JsonProperty("publicNetworkAccessForIngestion")]
        public string PublicNetworkAccessForIngestion { get; set; }

        [JsonProperty("publicNetworkAccessForQuery")]
        public string PublicNetworkAccessForQuery { get; set; }
    }
}
