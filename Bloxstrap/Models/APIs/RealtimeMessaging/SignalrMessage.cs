namespace Bloxstrap.Models.APIs.RealtimeMessaging
{
    public class SignalrMessage
    {
        [JsonPropertyName("type")]
        public int? Type { get; set; }

        [JsonPropertyName("target")]
        public string? Target { get; set; }

        [JsonPropertyName("arguments")]
        public string[]? Arguments { get; set; }
    }
}
