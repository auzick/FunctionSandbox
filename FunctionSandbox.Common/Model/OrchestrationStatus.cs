using System;
using System.Text.Json;

namespace Andy.Model
{
    public class OrchestrationStatus
    {
        public string name { get; set; }
        public string instanceId { get; set; }
        public string runtimeStatus { get; set; }
        public string input { get; set; }
        public string customStatus { get; set; }
        public object output { get; set; }
        public DateTime createdTime { get; set; }
        public DateTime lastUpdatedTime { get; set; }

        public override string ToString() =>
            JsonSerializer.Serialize(this);

        public static OrchestrationStatus FromJson(string json) =>
            JsonSerializer.Deserialize<OrchestrationStatus>(json);

    }
}