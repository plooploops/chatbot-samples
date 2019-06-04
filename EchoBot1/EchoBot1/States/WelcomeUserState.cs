using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBot1
{
    /// The state object is used to keep track of various state related to a user in a conversation.
    /// In this example, we are tracking if the bot has replied to customer first interaction.
    public class WelcomeUserState
    {
        /// <summary>
        /// Gets or sets whether the user has been welcomed in the conversation.
        /// </summary>
        /// <value>The user has been welcomed in the conversation.</value>
        public bool DidBotWelcomeUser { get; set; } = false;

        /// <summary>
        /// Check if bot presented options to user
        /// </summary>
        public bool DidBotPresentOptionsToUser { get; set; } = false;
    }
}
