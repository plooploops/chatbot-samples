using System;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using CommonConfiguration;
using Microsoft.Extensions.DependencyInjection;

namespace eventProcessorHost
{
    class Program
    {
        private static CommonSecrets secrets = BootstrapSecrets.GetConfiguration<CommonSecrets>(nameof(CommonSecrets));

        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            Console.WriteLine("Registering EventProcessor...");

            var storageConnectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", secrets.StorageAccountName, secrets.StorageAccountKey);

            var eventProcessorHost = new EventProcessorHost(
                secrets.EventHubName,
                PartitionReceiver.DefaultConsumerGroupName,
                secrets.EventHubConnectionString,
                storageConnectionString,
                secrets.StorageContainerName);

            // Registers the Event Processor Host and starts receiving messages
            await eventProcessorHost.RegisterEventProcessorAsync<SimpleEventProcessor>();

            Console.WriteLine("Receiving. Press ENTER to stop worker.");
            Console.ReadLine();

            // Disposes of the Event Processor Host
            await eventProcessorHost.UnregisterEventProcessorAsync();
        }
    }
}
