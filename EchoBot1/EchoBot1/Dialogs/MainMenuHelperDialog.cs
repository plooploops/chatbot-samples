﻿using EchoBot1.Resources;
using EchoBot1.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBot1.Dialogs
{
    public class MainMenuHelperDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<SelectedLanguageState> _selectedLanguageStatePropertyAccessor;
        private readonly IStatePropertyAccessor<CustomWrapperPromptState> _customWrapperPromptStatePropertyAccessor;

        public static readonly string MainMenuHelperDialogId = "mainMenuHelperDialog";

        public MainMenuHelperDialog(IStatePropertyAccessor<SelectedLanguageState> selectedLanguagePropertyAccessor, IStatePropertyAccessor<CustomWrapperPromptState> customWrapperPromptStateAccessor) : base(MainMenuHelperDialogId)
        {
            this._selectedLanguageStatePropertyAccessor = selectedLanguagePropertyAccessor;
            this._customWrapperPromptStatePropertyAccessor = customWrapperPromptStateAccessor;
            this.InitialDialogId = MainMenuHelperDialogId;

            ChoicePrompt cp = new ChoicePrompt("choicePrompt");
            cp.Style = ListStyle.SuggestedAction;

            this.AddDialog(cp);
            this.AddDialog(new QnAMakerDialog("QnADialog"));
            this.AddDialog(new AzureSearchDialog("AzureSearchDialog", selectedLanguagePropertyAccessor));
            this.AddDialog(new AzureSearchFacetsDialog("AzureSearchFacetsDialog"));
            this.AddDialog(new CardSelectHelperDialog("CardSelectHelperDialog", _selectedLanguageStatePropertyAccessor, customWrapperPromptStateAccessor));
            this.AddDialog(new ChooseLanguageDialog("ChooseLanguageDialog", selectedLanguagePropertyAccessor));
            this.AddDialog(new HTMLToJSONDialog("htmlToJsonDialog"));

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
                                    Choices = ChoiceFactory.ToChoices(new List<string> { MainMenuHelperDialogStrings.Option1, MainMenuHelperDialogStrings.Option2, MainMenuHelperDialogStrings.Option3, MainMenuHelperDialogStrings.Option4, MainMenuHelperDialogStrings.Option5, MainMenuHelperDialogStrings.Option6 }),
                                    Prompt = MessageFactory.Text(MainMenuHelperDialogStrings.Prompt),
                                    RetryPrompt = MessageFactory.Text(MainMenuHelperDialogStrings.RetryPrompt)
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

                            if (menuChoice == MainMenuHelperDialogStrings.Option1)
                            {
                                return await stepContext.BeginDialogAsync("QnADialog");
                            }
                            else if (menuChoice == MainMenuHelperDialogStrings.Option2)
                            {
                                return await stepContext.BeginDialogAsync("AzureSearchDialog");
                            }
                            else if (menuChoice == MainMenuHelperDialogStrings.Option3)
                            {
                                return await stepContext.BeginDialogAsync("AzureSearchFacetsDialog");
                            }
                            else if (menuChoice == MainMenuHelperDialogStrings.Option4)
                            {
                                return await stepContext.BeginDialogAsync("CardSelectHelperDialog");
                            }
                            else if (menuChoice == MainMenuHelperDialogStrings.Option5)
                            {
                                return await stepContext.BeginDialogAsync("ChooseLanguageDialog");
                            }
                            else if (menuChoice == MainMenuHelperDialogStrings.Option6)
                            {
                                return await stepContext.BeginDialogAsync("htmlToJsonDialog");
                            }
                            else
                            {

                            }

                            return await stepContext.NextAsync().ConfigureAwait(false);
                        },
                        async (stepContext, ct) =>
                        {
                           return await stepContext.ReplaceDialogAsync(MainMenuHelperDialogId).ConfigureAwait(false);
                        }
                    }
                )
            );
        }
    }
}
