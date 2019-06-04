using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBot1
{
    public class BotUserStateAccessors
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WelcomeUserStateAccessors"/> class.
        /// Contains the <see cref="UserState"/> and associated <see cref="IStatePropertyAccessor{T}"/>.
        /// </summary>
        /// <param name="userState">The state object that stores the counter.</param>
        public BotUserStateAccessors(UserState userState)
        {
            UserState = userState ?? throw new ArgumentNullException(nameof(userState));
        }

        /// <summary>
        /// Gets the <see cref="IStatePropertyAccessor{T}"/> name used for the <see cref="BotBuilderSamples.WelcomeUserState"/> accessor.
        /// </summary>
        /// <remarks>Accessors require a unique name.</remarks>
        /// <value>The accessor name for the WelcomeUser state.</value>
        public static string BotUserName { get; } = $"{nameof(BotUserStateAccessors)}.BotUserState";

        public static string DialogStateAccessorName { get; } = $"{nameof(BotUserStateAccessors)}.DialogState";
        public IStatePropertyAccessor<DialogState> DialogStateStateAccessor { get; internal set; }

        /// <summary>
        /// Gets or sets the <see cref="IStatePropertyAccessor{T}"/> for DidBotWelcome.
        /// </summary>
        /// <value>
        /// The accessor stores if the bot has welcomed the user or not.
        /// </value>
        public IStatePropertyAccessor<BotUserState> BotUserState { get; set; }

        public IConfiguration Configuration { get; set; }


        /// <summary>
        /// Gets the <see cref="UserState"/> object for the conversation.
        /// </summary>
        /// <value>The <see cref="UserState"/> object.</value>
        public UserState UserState { get; }
    }
}
