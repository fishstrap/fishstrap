namespace Bloxstrap.Models.APIs.Roblox
{
    public class OmniSearchResponse
    {
        [JsonPropertyName("searchResults")]
        public List<OmniSearchGroup> SearchResults { get; set; } = new();
    }

    public class OmniSearchGroup
    {
        [JsonPropertyName("contentGroupType")]
        public string ContentGroupType { get; set; } = String.Empty;

        [JsonPropertyName("contents")]
        public List<OmniSearchContent> Contents { get; set; } = new();
    }

    public class OmniSearchContent
    {
        [JsonPropertyName("universeId")]
        public long UniverseId { get; set; }

        [JsonPropertyName("rootPlaceId")]
        public long RootPlaceId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = String.Empty;

        [JsonPropertyName("playerCount")]
        public long PlayerCount { get; set; }

        [JsonPropertyName("isSponsored")]
        public bool IsSponsored { get; set; }
    }

    public class FavoriteGameResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = String.Empty;

        [JsonPropertyName("rootPlace")]
        public FavoriteGamePlace? RootPlace { get; set; }
    }

    public class FavoriteGamePlace
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
    }
}
