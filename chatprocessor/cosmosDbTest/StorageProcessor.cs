using CommonConfiguration;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace cosmosDbConnection
{
    public class StorageProcessor
    {
        private readonly ILogger<StorageProcessor> log;
        private DocumentClient client;
        private static CommonSecrets secrets = BootstrapSecrets.GetConfiguration<CommonSecrets>(nameof(CommonSecrets));

        public StorageProcessor(ILogger<StorageProcessor> log)
        {
            this.log = log;
        }

        public async Task SaveDocument<T>(T document)
        {
            try
            {
                client = new DocumentClient(new Uri(secrets.CosmosEndpointUri), secrets.CosmosPrimaryKey);

                await client.CreateDatabaseIfNotExistsAsync(new Database { Id = secrets.CosmosDatabaseName });
                await client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(secrets.CosmosDatabaseName), new DocumentCollection { Id = secrets.CosmosCollectionName });
                await CreateSentimentDocumentIfNotExists(secrets.CosmosDatabaseName, secrets.CosmosCollectionName, document);

                log.LogInformation($"Saved the document!");
            }
            catch (Exception e)
            {
                log.LogCritical($"There was a problem saving the document. The error was: {e.Message}");
            }
        }

        private async Task CreateSentimentDocumentIfNotExists<T>(string databaseName, string collectionName, T document)
        {
            await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), document);
        }
    }
}
