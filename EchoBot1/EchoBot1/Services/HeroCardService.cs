using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBot1.Services
{
    public class HeroCardService
    {
        /// <summary>
        /// Creates a <see cref="HeroCard"/>.
        /// </summary>
        /// <returns>A <see cref="HeroCard"/> the user can view and/or interact with.</returns>
        /// <remarks>Related types <see cref="CardImage"/>, <see cref="CardAction"/>,
        /// and <see cref="ActionTypes"/>.</remarks>
        public static HeroCard GetHeroCard(string title = null, string subtitle = null, string text = null, IList<CardImage> images = null, IList<CardAction> buttons = null, CardAction tap = null)
        {
            var heroCard = new HeroCard
            {
                Title = title,
                Subtitle = subtitle,
                Text = text,
                Images = images,
                Buttons = buttons,
                Tap = tap
            };

            return heroCard;
        }

        public static HeroCard GetHeroCard(string title = null, string subtitle = null, string text = null, string imageBaseUri = null, IList<string> images = null, IList<CardAction> buttons = null, CardAction tap = null)
        {
            var heroCard = new HeroCard
            {
                Title = title,
                Subtitle = subtitle,
                Text = text,
                Images = (images != null) ? images.Select(_ => new CardImage((String.IsNullOrEmpty(imageBaseUri) ? _ : imageBaseUri + _))).ToList() : null,
                Buttons = buttons,
                Tap = tap
            };

            return heroCard;
        }
    }
}
