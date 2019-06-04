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
using System.Threading;

namespace EchoBot1.Dialogs
{
    public class AzureSearchDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<SelectedLanguageState> _selectedLanguageStatePropertyAccessor;

        public AzureSearchDialog(string dialogId, IStatePropertyAccessor<SelectedLanguageState> selectedLanguageStatePropertyAccessor) : base(dialogId)
        {
            _selectedLanguageStatePropertyAccessor = selectedLanguageStatePropertyAccessor ?? throw new ArgumentNullException(nameof(selectedLanguageStatePropertyAccessor));

            const string imageBaseURI = @"https://andysa.blob.core.windows.net/search/OSL%202019/";
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
                                    Prompt = MessageFactory.Text("[Azure Search Dialog] What band would you like to search for?"),
                                },
                                ct
                            ).ConfigureAwait(false);
                        },
                        async (stepContext, ct) =>
                        {
                            var userAnswer = (string) stepContext.Result;

                            await stepContext.Context.SendActivityAsync("Here is the show you're looking for!");

                            var perf = SearchService.FindPerformances(userAnswer, null, null);

                            List<HeroCard> cards = new List<HeroCard>();
                            foreach(BO.Performance p in perf)
                            {
                                cards.Add(HeroCardService.GetHeroCard(
                                    p.BandName,
                                    p.Description,
                                    null,
                                    imageBaseURI,
                                    new List<string>() { p.Image },
                                    null,
                                    null
                                    ));
                            }

                            var reply = CarouselCardService.GenerateCarouselCard(
                                stepContext.Context,
                                cards.Select(_=> _.ToAttachment()).ToList<Attachment>());

                            await stepContext.Context.SendActivityAsync(reply);

                            return await stepContext.NextAsync().ConfigureAwait(false);
                        }
                    }
                )
            );
        }
    }
}
