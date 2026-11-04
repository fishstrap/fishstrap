namespace Bloxstrap.Models.APIs.Roblox
{
    public class UserDetailsBatchRequest
    {
        [JsonPropertyName("userIds")]
        public List<long> UserIds { get; set; } = new();

        [JsonPropertyName("excludeBannedUsers")]
        public bool ExcludeBannedUsers { get; set; }
    }
}
