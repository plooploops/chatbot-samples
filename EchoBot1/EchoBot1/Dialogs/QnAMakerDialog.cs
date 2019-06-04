using EchoBot1.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBot1.Dialogs
{
    public class QnAMakerDialog : ComponentDialog
    {
        public QnAMakerDialog(string dialogId) : base(dialogId)
        {
            // ID of the child dialog that should be started anytime the component is started.
            this.InitialDialogId = dialogId;

            this.AddDialog(new TextPrompt("textPrompt"));

            // Define the conversation flow using the waterfall model.
            this.AddDialog(
                new WaterfallDialog(dialogId, new WaterfallStep[]
                    {
                        async (stepContext, ct) =>
                        {
                            return await stepContext.PromptAsync(
                                "textPrompt",
                                new PromptOptions
                                {
                                    Prompt = MessageFactory.Text("[QnA Maker Dialog] What do you want to know about this event?"),
                                },
                                ct
                            ).ConfigureAwait(false);
                        },
                        async (stepContext, ct) =>
                        {
                            var userAnswer = (string) stepContext.Result;

                            //Ask QNA Maker here.
                            var qnaAnswer = await QnAService.PostQuestion(userAnswer);

                            JObject json = JObject.Parse(qnaAnswer);
                            string answer = (json["answers"] as JArray)[0]["answer"].Value<string>();

                            await stepContext.Context.SendActivityAsync(answer);

                            return await stepContext.NextAsync().ConfigureAwait(false);
                        }
                    }
                )
            );
        }
    }
}
