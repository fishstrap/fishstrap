namespace Bloxstrap.Models.APIs.RobloxParty.Events
{
    public class MessageEvent
    {
        [JsonPropertyName("Type")]
        public string Type { get; set; } = "MessageCreated";

        [JsonPropertyName("ChannelId")]
        public string ConversationId { get; set; } = String.Empty;

        [JsonPropertyName("IsTyping")]
        public bool? IsTyping { get; set; }
    }
}
