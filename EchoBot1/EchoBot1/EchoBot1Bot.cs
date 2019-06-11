// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EchoBot1.Dialogs;
using EchoBot1.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.Extensions.Localization;

namespace EchoBot1
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service.  Transient lifetime services are created
    /// each time they're requested. For each Activity received, a new instance of this
    /// class is created. Objects that are expensive to construct, or have a lifetime
    /// beyond the single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class EchoBot1Bot : IBot
    {
        private static List<string> supportedActions = new List<string> { "FAQs QnA Maker", "Azure Search", "Azure Search Facets" };
        private readonly ConversationStateAccessors _accessors;
        private readonly ILogger _logger;
        private readonly DialogSet _dialogSet;
        ConversationState _conversationState;

        private readonly BotUserStateAccessors _botUserStateAccessors;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="conversationState">The managed conversation state.</param>
        /// <param name="loggerFactory">A <see cref="ILoggerFactory"/> that is hooked to the Azure App Service provider.</param>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1#windows-eventlog-provider"/>
        public EchoBot1Bot(ConversationState conversationState, ILoggerFactory loggerFactory, ConversationStateAccessors conversationStateAccessors, BotUserStateAccessors statePropertyAccessor, DialogSet dialogSet)
        {
            _botUserStateAccessors = statePropertyAccessor ?? throw new System.ArgumentNullException("state accessor can't be null");
            _accessors = conversationStateAccessors;
            if (conversationState == null)
            {
                throw new System.ArgumentNullException(nameof(conversationState));
            }

            _conversationState = conversationState;

            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<EchoBot1Bot>();
            _logger.LogTrace("Turn start.");

            if(dialogSet == null)
            {
                throw new System.ArgumentException(nameof(dialogSet));
            }

            _dialogSet = dialogSet;
        }

        /// <summary>
        /// Every conversation turn for our Echo Bot will call this method.
        /// There are no dialogs used, since it's "single turn" processing, meaning a single
        /// request and response.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        /// <seealso cref="IMiddleware"/>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Create a dialog context for the turn in the dialog set which will be used to begin/continue the dialog flow
            var dialogContext = await this._dialogSet.CreateContextAsync(turnContext, cancellationToken);

            using (_logger.BeginScope("OnTurnAsync - ActivityType={ActivityType}", turnContext.Activity.Type))
            {
                if (turnContext.Activity.Type == ActivityTypes.Message)
                {
                    _logger.LogInformation("Handling a message, active dialog is: {ActiveDialogId}", dialogContext.ActiveDialog?.Id ?? "<NONE>");

                    CustomWrapperPromptState customWrapperPromptState = await _accessors.CustomWrapperPromptState.GetAsync(
                                turnContext,
                                () => new CustomWrapperPromptState() { Submitted = new Dictionary<string, string>() },
                                cancellationToken);

                    if (dialogContext.ActiveDialog != null)
                    {
                        if (customWrapperPromptState.Submitted.ContainsKey(turnContext.Activity.Conversation.Id) && 
                            customWrapperPromptState.Submitted[turnContext.Activity.Conversation.Id] != turnContext.Activity.Id)
                        {
                            await turnContext.SendActivityAsync("Looks like that's already been submitted.");

                            await dialogContext.BeginDialogAsync(MainMenuHelperDialog.MainMenuHelperDialogId, null, cancellationToken);
                        }
                        else
                        {
                            await dialogContext.ContinueDialogAsync(cancellationToken);
                        }
                    }
                    else
                    {
                        await dialogContext.BeginDialogAsync(MainMenuHelperDialog.MainMenuHelperDialogId, null, cancellationToken);
                    }
                }
                else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
                {
                    if (this.MemberJoined(turnContext.Activity))
                    {
                        _logger.LogInformation("Starting a new conversation...");

                        await turnContext.SendActivityAsync("Hey there! Welcome.");

                        await dialogContext.BeginDialogAsync(MainMenuHelperDialog.MainMenuHelperDialogId, null, cancellationToken);
                    }
                }

                _logger.LogInformation("Saving all changes to bot state...");

                // Always persist changes at the end of every turn, here this is the dialog state in the conversation state.
                await _conversationState.SaveChangesAsync(turnContext, cancellationToken: cancellationToken);

                _logger.LogInformation("Turn completed!");
            }
        }

        private bool MemberJoined(Activity activity)
        {
            return ((activity.MembersAdded.Count != 0 && (activity.MembersAdded[0].Id != activity.Recipient.Id)));
        }

        private static async Task SendSuggestedActionsAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var reply = turnContext.Activity.CreateReply();
            var actions = new List<CardAction>();

            supportedActions.ForEach(i => actions.Add(new CardAction() { Title = i, Type = ActionTypes.ImBack, Value = i }));
            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = actions
            };

            await turnContext.SendActivityAsync(reply, cancellationToken);
        }

        private static async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in turnContext.Activity.MembersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync("Welcome! What may I help you with my friend?", cancellationToken: cancellationToken);
                    await SendSuggestedActionsAsync(turnContext, cancellationToken);
                }
            }
        }
    }
}
