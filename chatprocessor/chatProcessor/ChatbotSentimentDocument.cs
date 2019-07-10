using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace chatProcessor
{
    class ChatbotSentimentDocument
    {
        public string ConversationId = Guid.NewGuid().ToString();
        public string ChatText { get; set; }
        public double? SentimentScore { get; set; }
        public DateTime CreatedDate = DateTime.UtcNow;
    }
}
