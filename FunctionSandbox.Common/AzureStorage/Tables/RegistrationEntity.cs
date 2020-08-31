using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Andy.AzureStorage.Tables;
using Andy.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos.Table;

namespace Andy.AzureStorage.Tables
{
    public class RegistrationEntity : TableEntityBase
    {
        [IgnoreProperty]
        public string OrchestrationName => PartitionKey;
        [IgnoreProperty]
        public string InstanceId => RowKey;
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string UserPhone { get; set; }
        public string UserPassword { get; set; }
        public DateTime EmailLastVerified { get; set; }
        public DateTime PhoneLastVerified { get; set; }

        public RegistrationEntity() : base("Registrations")
        {
        }

        public RegistrationEntity(
            string orchestratorName,
            string instanceId,
            UserRegistration reg
        ) : this()
        {
            PartitionKey = orchestratorName;
            RowKey = instanceId;
            UserName = reg.UserName;
            UserEmail = reg.UserEmail;
            UserPassword = reg.UserPassword;
            UserPhone = reg.UserEmail;
            EmailLastVerified = EnsureMinDate(reg.EmailLastVerified);
            PhoneLastVerified = EnsureMinDate(reg.PhoneLastVerified);
        }

        public RegistrationEntity Save()
        {
            EmailLastVerified = EnsureMinDate(EmailLastVerified);
            PhoneLastVerified = EnsureMinDate(EmailLastVerified);
            return base.Save<RegistrationEntity>();
        }

        public RegistrationEntity Fetch(string partitionKey, string rowKey)
        {
            return base.Fetch<RegistrationEntity>(partitionKey, rowKey);
        }

    }
}