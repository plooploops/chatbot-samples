using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using chatProcessor.textAnalytics;
using cosmosDbConnection;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace chatProcessor
{
    public class ProcessChatMessage
    {
        private readonly TextProcessor textProcessor;
        private readonly StorageProcessor storageProcessor;

        public ProcessChatMessage(
            TextProcessor textProcessor, 
            StorageProcessor storageProcessor
            )
        {
            this.textProcessor = textProcessor;
            this.storageProcessor = storageProcessor;
        }

        [FunctionName("ProcessChatMessage")]
        public async Task ProcessChatMessageAsync([EventHubTrigger("[insert]", Connection = "EventHubConnectionAppSetting")] EventData[] events, ILogger log)
        {
            var exceptions = new List<Exception>();

            foreach (EventData eventData in events)
            {
                try
                {
                    double? score;
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                    // Replace these two lines with your processing logic.
                    log.LogInformation($"Processing message: {messageBody}");

                    try
                    {
                        //give the message body and get back a decimal sentiment score between 0.00 - 1.00.
                        score = await textProcessor.ProcessTextAsync(messageBody);

                        var document = new ChatbotSentimentDocument
                        {
                            ChatText = messageBody,
                            SentimentScore = score
                        };

                        //save Document to cosmosDb
                        await storageProcessor.SaveDocument<ChatbotSentimentDocument>(document);
                    }
                    catch (Exception e)
                    {
                        log.LogCritical($"There was a problem processing the sentiment score in the 'TextProcessor'. The error was: {e.Message}");
                    }
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    log.LogError($"There was a problem processing the message: {e.Message}");
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }
    }
}
