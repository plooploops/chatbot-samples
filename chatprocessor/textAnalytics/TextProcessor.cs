using CommonConfiguration;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace chatProcessor.textAnalytics
{
    public class TextProcessor
    {
        private readonly ILogger<TextProcessor> log;
        private static CommonSecrets secrets = BootstrapSecrets.GetConfiguration<CommonSecrets>(nameof(CommonSecrets));

        public TextProcessor(ILogger<TextProcessor> log)
        {
            this.log = log;
        }

        public async Task<double?> ProcessTextAsync(string textToAnalyze)
        {
            var credentials = new ApiKeyServiceClientCredentials(secrets.CognitiveServicesSubscriptionKey);
            var textAnalyticsClient = new TextAnalyticsClient(credentials)
            {
                Endpoint = secrets.CognitiveServicesEndpoint
            };

            var score = await GetSentimentAnalysisAsync(textAnalyticsClient, textToAnalyze);
            log.LogInformation($"Sentiment score is: {score}.");

            return score; 
        }

        private static async Task<double?> GetSentimentAnalysisAsync(TextAnalyticsClient client, string textToAnalyze)
        {
            var inputDocuments = new MultiLanguageBatchInput(
                new List<MultiLanguageInput>
                {
                    new MultiLanguageInput("en", "1", textToAnalyze)
                });

            var result = await client.SentimentAsync(false, inputDocuments);
            return result.Documents[0].Score;
        }
    }
}
