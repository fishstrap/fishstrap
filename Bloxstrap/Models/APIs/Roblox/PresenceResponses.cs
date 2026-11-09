namespace Bloxstrap.Models.APIs.Roblox
{
    public class FriendResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
    }

    public class UserPresencesResponse
    {
        [JsonPropertyName("userPresences")]
        public List<UserPresence> UserPresences { get; set; } = new();
    }

    public class UserPresence
    {
        [JsonPropertyName("userPresenceType")]
        public UserPresenceType UserPresenceType { get; set; }

        [JsonPropertyName("lastLocation")]
        public string? LastLocation { get; set; }

        [JsonPropertyName("placeId")]
        public long? PlaceId { get; set; }

        [JsonPropertyName("rootPlaceId")]
        public long? RootPlaceId { get; set; }

        [JsonPropertyName("gameId")]
        public string? GameId { get; set; }

        [JsonPropertyName("universeId")]
        public long? UniverseId { get; set; }

        [JsonPropertyName("userId")]
        public long UserId { get; set; }

        public bool IsInGame => UserPresenceType == UserPresenceType.InGame && UniverseId is > 0;
    }
}
