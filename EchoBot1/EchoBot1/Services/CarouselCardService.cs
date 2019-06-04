using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBot1.Services
{
    public class CarouselCardService
    {
        public static  Activity GenerateCarouselCard(ITurnContext context, List<Attachment> attachments)
        {
            Activity reply = context.Activity.CreateReply();
            
            reply.Attachments.Clear();
            reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            reply.Attachments = attachments;

            return reply;
        }
    }
}
