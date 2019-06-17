using AdaptiveCards;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBot1.Services
{
    public class AdaptiveCardService
    {
        private static async Task<string> GetCardText(string cardPath)
        {
            return await File.ReadAllTextAsync($@"{cardPath}");
        }

        public static AdaptiveCard GetAdaptiveCard(string json)
        {
            //placeholder to update the submit action data id
            if (json.Contains("{0}"))
            {
                json = json.Replace("{0}", Guid.NewGuid().ToString());
            }
            var ret = AdaptiveCard.FromJson(json);

            return ret.Card;
        }

        public static async Task<AdaptiveCard> GetAdaptiveCardByFileName(string filePath)
        {
            var json = await GetCardText(filePath);

            var ret = GetAdaptiveCard(json);
            return ret;
        }
    }
}
