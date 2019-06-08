using EchoBot1.Dialogs;
using EchoBot1.Middleware;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBot1.Bootstrap
{
    public static class BootstrapBot
    {
        public static void AddConfiguredBot(this IServiceCollection services, IConfiguration configuration, ILoggerFactory loggerFactory, bool _isProduction)
        {
            var secretKey = configuration.GetSection("botFileSecret")?.Value;
            var botFilePath = configuration.GetSection("botFilePath")?.Value;

            var appId = configuration.GetSection("MicrosoftAppId").Value;
            var appPassword = configuration.GetSection("MicrosoftAppPassword").Value;

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

            IStorage dataStore = new MemoryStorage();

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
                ILogger logger = loggerFactory.CreateLogger<EchoBot1Bot>();

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
                    Configuration = configuration
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
        }
    }
}