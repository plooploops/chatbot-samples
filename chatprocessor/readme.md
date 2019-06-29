# ChatProcessor (with Cognitive Services & Cosmos DB)

This project uses a `chatProcessor` project which is an Azure v2 Function app set to trigger on event hub message submission. The message is sent to Azure Cognitive Services for sentiment analysis, then the resulting sentiment score is sent to Cosmos DB along with the original message and some metadata.

The solution uses Azure Functions v2 with the new [Dependency Injection](https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection) style leveraging `Microsoft.Azure.Functions.Extensions` to inject two services inside of `startup.cs`:

1. TextProcessor
2. StorageProcessor

```c#
    builder.Services.AddSingleton<TextProcessor>();
    builder.Services.AddSingleton<StorageProcessor>();
```

## Projects

The following projects exist as part of this solution:

### CommonConfiguration

This project contains a static class and method to inject secrets to the required referenced projects such as Cosmos DB and Azure Cognitive Services. You must configure the local "secrets" environment in accordance with the standard .NET Core method for saving confidential information. This will keep critical secrets out of the repository.

Please visit this repository for more information: https://github.com/jasonshave/ConsoleSecrets and pay special attention to the [Configuration](#Configuration) section below.

### textAnalytics

This project simply takes a string input and sends it to the Azure Cognitive Services API and receives back a "Sentiment Score" as a `double`.

### eventHubTest

This console application project allows you to test message submission to the hub while debugging the Azure Function locally. The application asks for user input (the message to send) and provides feedback on whether or not it was successfully submitted.

### eventProcessorHost

This is a console application which was used to test the Event Hub Processor Host before the function app was created. Since the Azure Function trigger creates it's own Processor Host, this project can be removed or de-referenced.

### cosmosDbConnection

This project holds the configuration and establishment of connectivity to Cosmos DB. Once the Function App executes, and gets a sentiment score, we store this information into a collection. Please see the configuration section below for determining what settings to create where.

## Configuration

For local development, follow the [instructions](https://github.com/jasonshave/ConsoleSecrets) on configuring a local `secrets.json` file so you can test the Function App and ensure connectivity to Event Hubs and Cosmos DB. Once this configuration is complete, you will need to populate the `secrets.json` file and your tenant with the correct settings (identified below).

1. [Create a new Azure Event Hub](https://docs.microsoft.com/en-us/azure/event-hubs/event-hubs-create). This will give you a namespace and hub name. Obtain your `EventHubConnectionString` and `EventHubName` from this configuration.

2. [Create a Cognitive Services account](https://docs.microsoft.com/en-us/azure/cognitive-services/text-analytics/quickstarts/csharp) and obtain your `CognitiveServicesSubscriptionKey` and `CognitiveServicesEndpoint` URI.

3. [Create a Cosmos DB account and collection](https://docs.microsoft.com/en-us/azure/cosmos-db/sql-api-dotnetcore-get-started) to obtain the following values:

   - `CosmosEndpointUri`
   - `CosmosPrimaryKey`
   - `CosmosDatabaseName`
   - `CosmosCollectionName`

With this information available and populated in your `secrets.json` file, the solution can be compiled and tested by running the `eventHubTest` console application.

When deploying the Function App to Azure, the application configuration must be set up to consume the secrets using the same concrete `CommonSecrets` class as follows:

1. Log into the Azure portal and locate your Function App

2. Locate the **Application settings** of your Function App and populate the following settings:

   - Name = CommonSecrets:EventHubConnectionString, Value = [CONNECTION_STRING]
   - Name = CommonSecrets:EventHubName, Value = [YOUR_EV_HUB_NAME]
   - ...and so on for each of the required parameters

A static class called `CommonConfiguration` contains a method called `GetSecrets<T>` and binds it to a class called `CommonSecrets` containing a structure for saving configuration information.
