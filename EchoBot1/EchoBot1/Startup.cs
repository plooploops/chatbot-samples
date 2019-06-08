// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using EchoBot1.Bootstrap;
using EchoBot1.Dialogs;
using EchoBot1.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EchoBot1
{
    /// <summary>
    /// The Startup class configures services and the request pipeline.
    /// </summary>
    public class Startup
    {
        private ILoggerFactory _loggerFactory;
        private readonly bool _isProduction;

        public Startup(IHostingEnvironment env)
        {
            _isProduction = env.IsProduction();
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> specifies the contract for a collection of service descriptors.</param>
        /// <seealso cref="IStatePropertyAccessor{T}"/>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/dependency-injection"/>
        /// <seealso cref="https://docs.microsoft.com/en-us/azure/bot-service/bot-service-manage-channels?view=azure-bot-service-4.0"/>
        public void ConfigureServices(IServiceCollection services)
        {
            var secretKey = Configuration.GetSection("botFileSecret")?.Value;
            var botFilePath = Configuration.GetSection("botFilePath")?.Value;

            var appId = Configuration.GetSection("MicrosoftAppId").Value;
            var appPassword = Configuration.GetSection("MicrosoftAppPassword").Value;

            if (!_isProduction)
            {
                if (!File.Exists(botFilePath))
                {
                    throw new FileNotFoundException($"The .bot configuration file was not found. botFilePath: {botFilePath}");
                }

                // Loads .bot configuration file and adds a singleton that your Bot can access through dependency injection.
                BotConfiguration botConfig = new BotConfiguration();
                try
                {
                    botConfig = BotConfiguration.Load(botFilePath, secretKey);
                }
                catch
                {
                    var msg = @"Error reading bot file. Please ensure you have valid botFilePath and botFileSecret set for your environment.
        - You can find the botFilePath and botFileSecret in the Azure App Service application settings.
        - If you are running this bot locally, consider adding a appsettings.json file with botFilePath and botFileSecret.
        - See https://aka.ms/about-bot-file to learn more about .bot file its use and bot configuration.
        ";
                    throw new InvalidOperationException(msg);
                }

                services.AddSingleton(sp => botConfig ?? throw new InvalidOperationException($"The .bot configuration file could not be loaded. botFilePath: {botFilePath}"));

                // Retrieve current endpoint.
                var environment = _isProduction ? "production" : "development";
                var service = botConfig.Services.FirstOrDefault(s => s.Type == "endpoint" && s.Name == environment);
                if (service == null && _isProduction)
                {
                    // Attempt to load development environment
                    service = botConfig.Services.Where(s => s.Type == "endpoint" && s.Name == "development").FirstOrDefault();
                }

                if (!(service is EndpointService endpointService))
                {
                    throw new InvalidOperationException($"The .bot file does not contain an endpoint with name '{environment}'.");
                }

                appId = endpointService.AppId;
                appPassword = endpointService.AppPassword;
            }
            // Memory Storage is for local bot debugging only. When the bot
            // is restarted, everything stored in memory will be gone.
            IStorage dataStore = new MemoryStorage();

            // For production bots use the Azure Blob or
            // Azure CosmosDB storage providers. For the Azure
            // based storage providers, add the Microsoft.Bot.Builder.Azure
            // Nuget package to your solution. That package is found at:
            // https://www.nuget.org/packages/Microsoft.Bot.Builder.Azure/
            // Un-comment the following lines to use Azure Blob Storage
            // // Storage configuration name or ID from the .bot file.
            // const string StorageConfigurationId = "<STORAGE-NAME-OR-ID-FROM-BOT-FILE>";
            // var blobConfig = botConfig.FindServiceByNameOrId(StorageConfigurationId);
            // if (!(blobConfig is BlobStorageService blobStorageConfig))
            // {
            //    throw new InvalidOperationException($"The .bot file does not contain an blob storage with name '{StorageConfigurationId}'.");
            // }
            // // Default container name.
            // const string DefaultBotContainer = "<DEFAULT-CONTAINER>";
            // var storageContainer = string.IsNullOrWhiteSpace(blobStorageConfig.Container) ? DefaultBotContainer : blobStorageConfig.Container;
            // IStorage dataStore = new Microsoft.Bot.Builder.Azure.AzureBlobStorage(blobStorageConfig.ConnectionString, storageContainer);

            // Create and add conversation state.
            var conversationState = new ConversationState(dataStore);
            services.AddSingleton(conversationState);

            var userState = new UserState(dataStore);
            services.AddSingleton(userState);

            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<BotFrameworkOptions>>().Value;
                if (options == null)
                {
                    throw new InvalidOperationException("BotFrameworkOptions must be configured prior to setting up the State Accessors");
                }

                var userStateAccessors = sp.GetRequiredService<BotUserStateAccessors>();
                var dialogStateAccessor = userStateAccessors.DialogStateStateAccessor;

                var dialogSet = new DialogSet(dialogStateAccessor);

                var conversationStateAccessor = sp.GetRequiredService<ConversationStateAccessors>();

                conversationStateAccessor.CounterState = conversationState.CreateProperty<CounterState>(ConversationStateAccessors.CounterStateName);
                conversationStateAccessor.SelectedLanguage = conversationState.CreateProperty<SelectedLanguageState>(ConversationStateAccessors.SelectedLanguageName);

                dialogSet.Add(new MainMenuHelperDialog(conversationStateAccessor.SelectedLanguage));

                return dialogSet;
            });

            services.AddBot<EchoBot1Bot>(options =>
            {
                options.CredentialProvider = new SimpleCredentialProvider(appId, appPassword);

                // Catches any errors that occur during a conversation turn and logs them to currently
                // configured ILogger.
                ILogger logger = _loggerFactory.CreateLogger<EchoBot1Bot>();

                options.OnTurnError = async (context, exception) =>
                {
                    logger.LogError($"Exception caught : {exception}");
                    await context.SendActivityAsync("Sorry, it looks like something went wrong.");
                };

                // Locale Middleware (sets UI culture based on Activity.Locale)
                options.Middleware.Add(new SetLocaleMiddleware("en-us"));
                // show typing
                options.Middleware.Add(new ShowTypingMiddleware());
            });

            services.AddSingleton<BotUserStateAccessors>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<BotFrameworkOptions>>().Value;
                if (options == null)
                {
                    throw new InvalidOperationException("BotFrameworkOptions must be configured prior to setting up the State Accessors");
                }

                // Create the custom state accessor.
                // State accessors enable other components to read and write individual properties of state.
                var accessors = new BotUserStateAccessors(userState)
                {
                    BotUserState = userState.CreateProperty<BotUserState>(BotUserStateAccessors.BotUserName),
                    DialogStateStateAccessor = conversationState.CreateProperty<DialogState>(BotUserStateAccessors.DialogStateAccessorName),
                    Configuration = this.Configuration
                };

                return accessors;
            });

            services.AddSingleton<ConversationStateAccessors>(sp =>
            {
                return new ConversationStateAccessors(conversationState)
                {
                    CounterState = conversationState.CreateProperty<CounterState>(ConversationStateAccessors.CounterStateName),
                };
            });

            // to-do (clean-up services into a bootstrapper)
            //services.AddConfiguredBot(Configuration, _loggerFactory, _isProduction);

            services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
                .AddAzureAD(options => Configuration.Bind("AzureAd", options));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseBotFramework();
        }
    }
}
