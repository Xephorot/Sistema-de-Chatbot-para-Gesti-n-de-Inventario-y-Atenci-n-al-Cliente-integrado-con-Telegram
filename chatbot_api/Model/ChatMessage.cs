using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace chatbot_api.Model
{
    public class ChatMessage
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("chat_id")]
        public long ChatId { get; set; }

        [BsonElement("user_message")]
        public string UserMessage { get; set; } = string.Empty;

        [BsonElement("bot_response")]
        public string BotResponse { get; set; } = string.Empty;

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
