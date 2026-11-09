namespace Bloxstrap.Models.APIs.RobloxParty
{
    public class Conversation
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = String.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = String.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "one_to_one";

        [JsonPropertyName("participant_user_ids")]
        public long[] Participants { get; set; } = Array.Empty<long>();

        [JsonPropertyName("unread_message_count")]
        public int UnreadMessagesCount { get; set; }

        [JsonPropertyName("preview_message")]
        public UserMessage? PreviewMessage { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
