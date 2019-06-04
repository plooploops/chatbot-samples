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
using System.Resources;
using EchoBot1.Resources;
using System.Globalization;

namespace EchoBot1.Dialogs
{
    public class ChooseLanguageDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<SelectedLanguageState> _selectedLanguageStatePropertyAccessor;

        public ChooseLanguageDialog(string dialogId, IStatePropertyAccessor<SelectedLanguageState> selectedLanguageStatePropertyAccessor) : base(dialogId)
        {
            _selectedLanguageStatePropertyAccessor = selectedLanguageStatePropertyAccessor ?? throw new ArgumentNullException(nameof(selectedLanguageStatePropertyAccessor));
            // ID of the child dialog that should be started anytime the component is started.
            this.InitialDialogId = dialogId;

            this.AddDialog(new ChoicePrompt("chooseLanguagePrompt"));

            // Define the conversation flow using the waterfall model.
            this.AddDialog(
                new WaterfallDialog(dialogId, new WaterfallStep[]
                    {
                        async (stepContext, ct) =>
                        {
                            SelectedLanguageState selectedLanguage = await _selectedLanguageStatePropertyAccessor.GetAsync(
                                stepContext.Context, 
                                () => new SelectedLanguageState() { SelectedLanguage = "en-us" }, 
                                ct);

                            List<string> _langageChoices = new List<string>{ ChooseLanguageDialogStrings.Option1, ChooseLanguageDialogStrings.Option2 };

                            return await stepContext.PromptAsync(
                                "chooseLanguagePrompt",
                                new PromptOptions
                                {
                                    Choices = ChoiceFactory.ToChoices(_langageChoices),
                                    Prompt = MessageFactory.Text(ChooseLanguageDialogStrings.Prompt),
                                    RetryPrompt = MessageFactory.Text(ChooseLanguageDialogStrings.RetryPrompt)
                                },
                                ct
                            ).ConfigureAwait(false);
                        },
                        async (stepContext, ct) =>
                        {
                            var userAnswer = ((FoundChoice)stepContext.Result).Value;

                            var cultureInfo = LanguageService.LanguageChoiceMap.ContainsKey(userAnswer) ? new CultureInfo(LanguageService.LanguageChoiceMap[userAnswer]) : new CultureInfo("en-us");
                            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = cultureInfo;
                            stepContext.Context.Activity.Locale = cultureInfo.Name;

                            await _selectedLanguageStatePropertyAccessor.SetAsync(
                                stepContext.Context,
                                new SelectedLanguageState
                                {
                                    SelectedLanguage = userAnswer,
                                },
                                ct);

                            await stepContext.Context.SendActivityAsync(ChooseLanguageDialogStrings.Step2);

                            return await stepContext.NextAsync().ConfigureAwait(false);
                        }
                    }
                )
            );
        }
    }
}
