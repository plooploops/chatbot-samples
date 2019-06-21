using EchoBot1.Services;
using EchoBot1.Resources;
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
using System.Globalization;
using EchoBot1.Prompts;

namespace EchoBot1.Dialogs
{
    public class CardSelectHelperDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<SelectedLanguageState> _selectedLanguageStatePropertyAccessor;

        public static readonly string CardSelectHelperDialogId = "cardSelectHelperDialog";

        public CardSelectHelperDialog(string dialogId,
            IStatePropertyAccessor<SelectedLanguageState> selectedLanguagePropertyAccessor) : base(dialogId)
        {
            this._selectedLanguageStatePropertyAccessor = selectedLanguagePropertyAccessor;
            // ID of the child dialog that should be started anytime the component is started.
            this.InitialDialogId = dialogId;
 
            ChoicePrompt cp = new ChoicePrompt("choicePrompt");
            cp.Style = ListStyle.SuggestedAction;

            this.AddDialog(cp);
            this.AddDialog(new AdaptiveCardDialog("AdaptiveCardDialog"));
            this.AddDialog(new AltTextTestDialog("AltTextTestDialog"));

            // Define the conversation flow using the waterfall model.
            this.AddDialog(
                new WaterfallDialog(this.InitialDialogId, new WaterfallStep[]
                    {
                        async (stepContext, ct) =>
                        {
                            SelectedLanguageState selectedLanguage = await _selectedLanguageStatePropertyAccessor.GetAsync(
                                stepContext.Context,
                                () => new SelectedLanguageState() { SelectedLanguage = "en-us" },
                                ct);

                            var cultureInfo = LanguageService.LanguageChoiceMap.ContainsKey(selectedLanguage.SelectedLanguage) ? new CultureInfo(LanguageService.LanguageChoiceMap[selectedLanguage.SelectedLanguage]) : new CultureInfo("en-us");
                            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = cultureInfo;

                            return await stepContext.PromptAsync(
                                "choicePrompt",
                                new PromptOptions
                                {
                                    Choices = ChoiceFactory.ToChoices(new List<string> { CardSelectHelperDialogStrings.Option1, CardSelectHelperDialogStrings.Option2}),
                                    Prompt = MessageFactory.Text(CardSelectHelperDialogStrings.Prompt),
                                    RetryPrompt = MessageFactory.Text(CardSelectHelperDialogStrings.RetryPrompt)
                                },
                                ct
                            ).ConfigureAwait(false);
                        },
                        async (stepContext, ct) =>
                        {
                            var menuChoice = ((FoundChoice)stepContext.Result).Value;

                            SelectedLanguageState selectedLanguage = await _selectedLanguageStatePropertyAccessor.GetAsync(
                                stepContext.Context,
                                () => new SelectedLanguageState() { SelectedLanguage = "en-us" },
                                ct);

                            var cultureInfo = LanguageService.LanguageChoiceMap.ContainsKey(selectedLanguage.SelectedLanguage) ? new CultureInfo(LanguageService.LanguageChoiceMap[selectedLanguage.SelectedLanguage]) : new CultureInfo("en-us");
                            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = cultureInfo;

                            if (menuChoice == CardSelectHelperDialogStrings.Option1)
                            {
                                return await stepContext.BeginDialogAsync("AdaptiveCardDialog");
                            }
                            else if (menuChoice == CardSelectHelperDialogStrings.Option2)
                            {
                                return await stepContext.BeginDialogAsync("AltTextTestDialog");
                            }
                            else
                            {

                            }

                            return await stepContext.NextAsync().ConfigureAwait(false);
                        },
                        async (stepContext, ct) =>
                        {
                           return await stepContext.ReplaceDialogAsync(CardSelectHelperDialogId).ConfigureAwait(false);
                        }
                    }
                )
            );
        }
    }
}
