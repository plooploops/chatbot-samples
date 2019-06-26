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
using EchoBot1.Helper;

namespace EchoBot1.Dialogs
{
    public class HTMLToJSONDialog : ComponentDialog
    {

        public HTMLToJSONDialog(string dialogId) : base(dialogId)
        {
            // ID of the child dialog that should be started anytime the component is started.
            this.InitialDialogId = dialogId;

            this.AddDialog(new NumberPrompt<int>("numberPrompt"));

            // Define the conversation flow using the waterfall model.
            this.AddDialog(
                new WaterfallDialog(dialogId, new WaterfallStep[]
                    {
                        async (stepContext, ct) =>
                        {
                            return await stepContext.PromptAsync(
                                "numberPrompt",
                                new PromptOptions
                                {
                                    Prompt = MessageFactory.Text("[HTML to JSON Dialog] How many tables would you like to skip?"),
                                },
                                ct
                            ).ConfigureAwait(false);
                        },
                        async (stepContext, ct) =>
                        {
                            var userAnswer = (int) stepContext.Result;

                            if (userAnswer < 0)
                                userAnswer = 0;

                            string json = HTMLHelper.TestHTML(userAnswer);

                            await stepContext.Context.SendActivityAsync(json);

                            return await stepContext.NextAsync().ConfigureAwait(false);
                        }
                    }
                )
            );
        }
    }
}
