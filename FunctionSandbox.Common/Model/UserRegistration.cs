using System;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Cosmos.Table.Protocol;
using System.Text.Json;
using Andy.AzureStorage.Tables;

namespace Andy.Model
{
    public class UserRegistration
    {

        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string UserPhone { get; set; }
        public string UserPassword { get; set; }
        public DateTime EmailLastVerified { get; set; }
        public DateTime PhoneLastVerified { get; set; }


        public UserRegistration()
        {
            EmailLastVerified = TableEntityBase.MinDate;
            PhoneLastVerified = TableEntityBase.MinDate;
        }

        public override string ToString() =>
            JsonSerializer.Serialize(this);

        public static UserRegistration FromJson(string json) =>
            JsonSerializer.Deserialize<UserRegistration>(json);

    }
}