namespace Bloxstrap.Models.APIs.Roblox
{
    public class GameServerResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = String.Empty;

        [JsonPropertyName("maxPlayers")]
        public int MaxPlayers { get; set; }

        [JsonPropertyName("playing")]
        public int Playing { get; set; }

        [JsonPropertyName("fps")]
        public double Fps { get; set; }

        [JsonPropertyName("ping")]
        public int Ping { get; set; }

        [JsonPropertyName("playerTokens")]
        public List<string> PlayerTokens { get; set; } = new();
    }
}
