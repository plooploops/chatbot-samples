using System;
using Microsoft.Azure.EventHubs;
using System.Text;
using System.Threading.Tasks;
using CommonConfiguration;

namespace eventHubTest
{
    class Program
    {
        private static EventHubClient eventHubClient;
        private static CommonSecrets secrets = BootstrapSecrets.GetConfiguration<CommonSecrets>(nameof(CommonSecrets));

        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            var connectionStringBuilder = new EventHubsConnectionStringBuilder(secrets.EventHubConnectionString)
            {
                EntityPath = secrets.EventHubName
            };

            eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());

            do
            {
                Console.WriteLine("Type the message you want to send...");
                var text = Console.ReadLine();
                await SendMessagesToEventHub(text);
            } while (AskUser("Do you want to send another message?"));

            await eventHubClient.CloseAsync();

            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
        }

        // Creates an event hub client and sends N messages to the event hub.
        private static async Task SendMessagesToEventHub(string messageToSend)
        {
            await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(messageToSend)));

            Console.WriteLine($"Message sent.");

            //for (var i = 0; i < numMessagesToSend; i++)
            //{
            //    try
            //    {
            //        var message = $"Message {i}";
            //        Console.WriteLine($"Sending message: {message}");
            //        await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(message)));
            //    }
            //    catch (Exception exception)
            //    {
            //        Console.WriteLine($"{DateTime.Now} > Exception: {exception.Message}");
            //    }

            //    await Task.Delay(10);
            //}

        }

        public static bool AskUser(string title)
        {
            ConsoleKey response;
            do
            {
                Console.Write($"{ title } [y/n] ");
                response = Console.ReadKey(false).Key;
                if (response != ConsoleKey.Enter)
                {
                    Console.WriteLine();
                }
            } while (response != ConsoleKey.Y && response != ConsoleKey.N);

            return (response == ConsoleKey.Y);
        }
    }
}
