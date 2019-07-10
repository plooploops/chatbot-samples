using System;

namespace CommonConfiguration
{
    public class CommonSecrets
    {
        public string EventHubConnectionString { get; set; }
        public string EventHubName { get; set; }
        public string StorageAccountName { get; set; }
        public string StorageContainerName { get; set; }
        public string StorageAccountKey { get; set; }
        public string CognitiveServicesSubscriptionKey { get; set; }
        public string CognitiveServicesEndpoint { get; set; }
        public string CosmosEndpointUri { get; set; }
        public string CosmosPrimaryKey { get; set; }
        public string CosmosDatabaseName { get; set; }
        public string CosmosCollectionName { get; set; }
    }
}
