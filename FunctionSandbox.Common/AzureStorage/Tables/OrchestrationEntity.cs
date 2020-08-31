using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Andy.AzureStorage.Tables
{
    public class OrchestrationEntity : TableEntityBase
    {
        [IgnoreProperty]
        public string OrchestrationName => PartitionKey;
        [IgnoreProperty]
        public string InstanceId => RowKey;
        public DateTime StartTime { get; set; }
        public string StatusQueryGetUri { get; set; }
        public string SendEventPostUri { get; set; }
        public string TerminatePostUri { get; set; }
        public string RewindPostUri { get; set; }
        public string PurgeHistoryDeleteUri { get; set; }

        public OrchestrationEntity()
            : base("Orchestrations")
        {
        }

        public OrchestrationEntity(
            string orchestratorName,
            string instanceId
            ) : this()
        {
            PartitionKey = orchestratorName;
            RowKey = instanceId;
        }

        public OrchestrationEntity(
            string orchestratorName,
            DateTime startTime,
            HttpManagementPayload payload
            ) : this(orchestratorName, payload.Id)
        {
            StartTime = startTime;
            StatusQueryGetUri = payload.StatusQueryGetUri;
            SendEventPostUri = payload.SendEventPostUri;
            TerminatePostUri = payload.TerminatePostUri;
            RewindPostUri = payload.RewindPostUri;
            PurgeHistoryDeleteUri = payload.PurgeHistoryDeleteUri;
        }

        public OrchestrationEntity Save()
        {
            return base.Save<OrchestrationEntity>();
        }

        public OrchestrationEntity Fetch(string partitionKey, string rowKey)
        {
            return base.Fetch<OrchestrationEntity>(partitionKey, rowKey);
        }

        public List<OrchestrationEntity> FetchAll()
        {
            return Table.ExecuteQuery(new TableQuery<OrchestrationEntity>()).ToList();
        }

    }
}