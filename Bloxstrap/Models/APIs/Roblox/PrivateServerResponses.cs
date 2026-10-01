namespace Bloxstrap.Models.APIs.Roblox
{
    public class PrivateServerEntry
    {
        [JsonPropertyName("vipServerId")]
        public long VipServerId { get; set; }

        [JsonPropertyName("accessCode")]
        public string? AccessCode { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("maxPlayers")]
        public int MaxPlayers { get; set; }

        [JsonPropertyName("playing")]
        public int? Playing { get; set; }

        [JsonPropertyName("owner")]
        public PrivateServerUser? Owner { get; set; }
    }

    public class PrivateServerUser
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }
    }

    public class PrivateServerDetails
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("link")]
        public string? Link { get; set; }

        [JsonPropertyName("game")]
        public PrivateServerGame? Game { get; set; }

        [JsonPropertyName("subscription")]
        public PrivateServerSubscription? Subscription { get; set; }

        [JsonPropertyName("permissions")]
        public PrivateServerPermissions? Permissions { get; set; }
    }

    public class PrivateServerGame
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class PrivateServerSubscription
    {
        [JsonPropertyName("price")]
        public long? Price { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("expired")]
        public bool Expired { get; set; }

        [JsonPropertyName("expirationDate")]
        public DateTime? ExpirationDate { get; set; }
    }

    public class PrivateServerPermissions
    {
        [JsonPropertyName("clanAllowed")]
        public bool ClanAllowed { get; set; }

        [JsonPropertyName("enemyClanId")]
        public long? EnemyClanId { get; set; }

        [JsonPropertyName("friendsAllowed")]
        public bool FriendsAllowed { get; set; }

        [JsonPropertyName("users")]
        public List<PrivateServerUser>? Users { get; set; }
    }
}
