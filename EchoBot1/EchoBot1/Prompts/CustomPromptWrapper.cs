using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace EchoBot1.Prompts
{
    //https://stackoverflow.com/questions/53009106/adaptive-card-response-from-a-waterfallstep-dialog-ms-bot-framework-v4
    public class CustomPromptWrapper : PromptWrapper<string>
    {
        public CustomPromptWrapper(string dialogId, PromptValidator<string> validator = null)
            : base(dialogId, validator)
        {
        }

        protected async override Task OnPromptAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, bool isRetry, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (isRetry && options.RetryPrompt != null)
            {
                await turnContext.SendActivityAsync(options.RetryPrompt, cancellationToken).ConfigureAwait(false);
            }
            else if (options.Prompt != null)
            {
                await turnContext.SendActivityAsync(options.Prompt, cancellationToken).ConfigureAwait(false);
            }
        }

        protected override Task<PromptRecognizerResult<string>> OnRecognizeAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var result = new PromptRecognizerResult<string>();
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var message = turnContext.Activity.AsMessageActivity();
                if (!string.IsNullOrEmpty(message.Text))
                {
                    result.Succeeded = true;
                    result.Value = message.Text;
                }
                /*Add handling for Value from adaptive card*/
                else if (message.Value != null)
                {
                    result.Succeeded = true;
                    result.Value = message.Value.ToString();
                }
            }

            return Task.FromResult(result);
        }
    }

}
