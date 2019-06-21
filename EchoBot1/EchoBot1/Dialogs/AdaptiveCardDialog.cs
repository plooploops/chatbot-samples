using EchoBot1.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using AdaptiveCards;
using EchoBot1.Prompts;
using Microsoft.Azure.EventHubs;
using System.Text;
using System.Threading.Tasks;
using EchoBot1.Helper;

namespace EchoBot1.Dialogs
{
    public class AdaptiveCardDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<CustomWrapperPromptState> _customWrapperPromptStatePropertyAccessor;
        private static EventHubClient eventHubClient;
        private const string EventHubConnectionString = "Endpoint=sb://<yournamespace>.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=<yourkey>";
        private const string EventHubName = "<yourhubname>";
        private string selectedDay { get; set; }
        public AdaptiveCardDialog(string dialogId, IStatePropertyAccessor<CustomWrapperPromptState> customWrapperPromptStatePropertyAccessor) : base(dialogId)
        {
            this._customWrapperPromptStatePropertyAccessor = customWrapperPromptStatePropertyAccessor;
            // ID of the child dialog that should be started anytime the component is started.
            this.InitialDialogId = dialogId;
            this.AddDialog(new CustomPromptWrapper("customPromptWrapper"));
            // Define the conversation flow using the waterfall model.
            this.AddDialog(
                new WaterfallDialog(dialogId, new WaterfallStep[]
                    {
                        async (stepContext, ct) =>
                        {
                            await stepContext.Context.SendActivityAsync("[Adaptive Card Dialog] - Adaptive Card Test!");

                            Attachment attachment = new Attachment()
                            {
                                ContentType = AdaptiveCard.ContentType,
                                Content = await AdaptiveCardService.GetAdaptiveCardByFileName(@".\Cards\multiple-input-submit.json")
                            };

                            var reply = stepContext.Context.Activity.CreateReply();
                            reply.Attachments.Add(attachment);

                            return await stepContext.PromptAsync(
                                "customPromptWrapper",
                                new PromptOptions
                                {
                                    Prompt = reply
                                },
                                ct
                            ).ConfigureAwait(false);
                        },
                        async (stepContext, ct) =>
                        {
                            //https://stackoverflow.com/questions/53009106/adaptive-card-response-from-a-waterfallstep-dialog-ms-bot-framework-v4
                            var userAnswer = (string) stepContext.Result;

                            try
                            {
                                var connectionStringBuilder = new EventHubsConnectionStringBuilder(EventHubConnectionString)
                                {
                                    EntityPath = EventHubName
                                };
                                eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());

                                Console.WriteLine($"Sending message: {userAnswer}");
                                await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(userAnswer)));
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine($"{DateTime.Now} > Exception: {exception.Message}");
                            }

                            await stepContext.Context.SendActivityAsync(userAnswer);

                            CustomWrapperPromptState customWrapperPromptState = await _customWrapperPromptStatePropertyAccessor.GetAsync(
                                stepContext.Context,
                                () => new CustomWrapperPromptState() { Submitted = new Dictionary<string, string>() },
                                ct);

                            var res = JObject.Parse(userAnswer);
                            customWrapperPromptState.Submitted.TryAdd(res.GetValue("id").ToString(), stepContext.Context.Activity.Id);

                            await _customWrapperPromptStatePropertyAccessor.SetAsync(
                                stepContext.Context,
                                customWrapperPromptState,
                                ct);

                            return await stepContext.NextAsync().ConfigureAwait(false);
                        }
                    }
                )
            );
        }
    }
}
