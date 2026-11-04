namespace Bloxstrap.Models.APIs.RobloxParty
{
    public class ConversationsPage : Page
    {
        [JsonPropertyName("conversations")]
        public List<Conversation> Conversations { get; set; } = new();
    }
}
