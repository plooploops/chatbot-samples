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

namespace EchoBot1.Dialogs
{
    public class AdaptiveCardDialog : ComponentDialog
    {
        public AdaptiveCardDialog(string dialogId) : base(dialogId)
        {
            // ID of the child dialog that should be started anytime the component is started.
            this.InitialDialogId = dialogId;

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
                                Content = await AdaptiveCardService.GetAdaptiveCardByFileName(@".\Cards\sample-table.json")
                            };

                            var reply = stepContext.Context.Activity.CreateReply();
                            reply.Attachments.Add(attachment);

                            await stepContext.Context.SendActivityAsync(reply);

                            return await stepContext.NextAsync().ConfigureAwait(false);
                        }
                    }
                )
            );
        }
    }
}
