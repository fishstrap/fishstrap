namespace Bloxstrap.Models.APIs.RobloxParty
{
    public class UserMessagesPage : Page
    {
        [JsonPropertyName("messages")]
        public List<UserMessage> Messages { get; set; } = new();
    }
}
