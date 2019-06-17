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

namespace EchoBot1.Dialogs
{
    public class AltTextTestDialog : ComponentDialog
    {
        private string selectedDay { get; set; }
        public AltTextTestDialog(string dialogId) : base(dialogId)
        {
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
                            await stepContext.Context.SendActivityAsync(userAnswer);

                            return await stepContext.NextAsync().ConfigureAwait(false);
                        }
                    }
                )
            );
        }
    }
}
