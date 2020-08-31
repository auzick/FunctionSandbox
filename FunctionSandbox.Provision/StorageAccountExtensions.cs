using Microsoft.Azure.Management.Storage.Fluent;

namespace Provision
{
    public static class StorageAccountExtensions
    {
        public static string GetConnectionString(this IStorageAccount account)
        {
            return $"DefaultEndpointsProtocol=https;AccountName={account.Name};AccountKey={account.GetKeys()[0].Value};EndpointSuffix=core.windows.net";
        }
    }
}