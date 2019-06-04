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


namespace EchoBot1.Dialogs
{
    public class AzureSearchFacetsDialog : ComponentDialog
    {
        private string selectedDay { get; set; }
        public AzureSearchFacetsDialog(string dialogId) : base(dialogId)
        {
            const string imageBaseURI = @"https://andysa.blob.core.windows.net/search/OSL%202019/";
            // ID of the child dialog that should be started anytime the component is started.
            this.InitialDialogId = dialogId;

            this.AddDialog(new ChoicePrompt("navigatePrompt"));

            // Define the conversation flow using the waterfall model.
            this.AddDialog(
                new WaterfallDialog(dialogId, new WaterfallStep[]
                    {
                        async (stepContext, ct) =>
                        {
                            List<string> _dayChoices = new List<string>() {"Any"};

                            var facets = SearchService.FindFacets("day", null);
                            facets.Values.ToList().ForEach(i => i.ToList().ForEach(j => _dayChoices.Add(j.Value.ToString())));
                            //_dayChoices.AddRange(facets.Select(_ => _.Key));

                            return await stepContext.PromptAsync(
                                "navigatePrompt",
                                new PromptOptions
                                {
                                    Choices = ChoiceFactory.ToChoices(_dayChoices),
                                    Prompt = MessageFactory.Text("[Azure Search Facets Dialog] What day would you like to hear more information about the event?"),
                                    RetryPrompt = MessageFactory.Text("Please tell me which day you would like to hear more information?")
                                },
                                ct
                            ).ConfigureAwait(false);
                        },
                        async (stepContext, ct) =>
                        {
                            var userAnswer = ((FoundChoice)stepContext.Result).Value;
                            this.selectedDay = userAnswer;

                            await stepContext.Context.SendActivityAsync("what Genres would you like to see on" + userAnswer + "?");

                            List<string> _dayChoices = new List<string>() {};
                           string searchStr = "day:" + userAnswer;
                            if(userAnswer == "Any")
                            {
                                searchStr = null;
                                this.selectedDay = null;
                            }

                            var facets = SearchService.FindFacets("genre", searchStr);
                            facets.Values.ToList().ForEach(i => i.ToList().ForEach(j => _dayChoices.Add(j.Value.ToString())));
                            //_dayChoices.AddRange(facets.Select(_ => _.Key));

                            return await stepContext.PromptAsync(
                                "navigatePrompt",
                                new PromptOptions
                                {
                                    Choices = ChoiceFactory.ToChoices(_dayChoices),
                                    Prompt = MessageFactory.Text("[Azure Search Facets Dialog] Here are all the genres for " + userAnswer + "?"),
                                    RetryPrompt = MessageFactory.Text("Please tell me which genre you would like to hear information?")
                                },
                                ct
                            ).ConfigureAwait(false);
                        },
                        async (stepContext, ct) =>
                        {
                            var userAnswer = ((FoundChoice)stepContext.Result).Value;

                            await stepContext.Context.SendActivityAsync("Based on what you've told me, here are the results you're looking for!");

                            var perf = SearchService.FindPerformances(null, userAnswer, this.selectedDay);

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
