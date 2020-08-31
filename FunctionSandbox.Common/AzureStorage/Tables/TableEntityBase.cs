using System;
using System.Text.Json.Serialization;
using Microsoft.Azure.Cosmos.Table;

// https://www.c-sharpcorner.com/article/azure-storage-tables/
// You should use the Microsoft Azure CosmosDB Table library for .NET in common Azure Storage tables and Azure Cosmos DB Table API scenarios.
// The package works with both the Azure Cosmos DB Table API and Azure Storage tables.

namespace Andy.AzureStorage.Tables
{
    public abstract class TableEntityBase : TableEntity
    {
        public static readonly DateTime MinDate = new DateTime(1601, 1, 1);

        [JsonIgnore]
        [IgnoreProperty]
        public CloudTable Table { get; private set; }

        public TableEntityBase()
        {
        }

        public TableEntityBase(string tableName)
        {
            Table = GetTable(
                tableName
            );
        }

        public T Save<T>() where T : ITableEntity
        {
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(this);
            TableResult result = Table.Execute(insertOrMergeOperation);
            var inserted = (T)result.Result;
            return inserted;
        }

        public T Fetch<T>(string partitionKey, string rowKey) where T : ITableEntity
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<T>(partitionKey, rowKey);
            TableResult result = Table.Execute(retrieveOperation);
            return (T)result.Result;
        }

        public TableResult Delete(string partitionKey, string rowKey)
        {
            var entity = new TableEntity(partitionKey, rowKey) { ETag = "*" };
            return Table.Execute(TableOperation.Delete(entity));
        }

        public TableResult Delete()
        {
            return Delete(this.PartitionKey, this.RowKey);
        }

        public static CloudTable GetTable(string tableName)
        {
            var cs = Environment.GetEnvironmentVariable("AzureStorage");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(cs);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient(
                new TableClientConfiguration()
                );
            tableClient.DefaultRequestOptions = new TableRequestOptions()
            {
                RetryPolicy = new LinearRetry(TimeSpan.FromMilliseconds(500), 6),
                LocationMode = LocationMode.PrimaryThenSecondary,
                MaximumExecutionTime = TimeSpan.FromSeconds(3)
            };
            CloudTable table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExists();
            return table;
        }

        protected DateTime EnsureMinDate(DateTime date)
        {
            return date < MinDate ? MinDate : date;
        }


    }
}