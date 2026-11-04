namespace Bloxstrap.Models.APIs.RealtimeMessaging
{
    public class PresenceNotification
    {
        public const string PresenceChangedType = "PresenceChanged";

        [JsonPropertyName("UserId")]
        public long UserId { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("PresenceReport")]
        public UserPresence? PresenceReport { get; set; }
    }
}
