namespace Bloxstrap.Models.APIs.Roblox
{
    public class BadgeResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = String.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("displayDescription")]
        public string? DisplayDescription { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("statistics")]
        public BadgeStatistics? Statistics { get; set; }
    }

    public class BadgeStatistics
    {
        [JsonPropertyName("pastDayAwardedCount")]
        public long PastDayAwardedCount { get; set; }

        [JsonPropertyName("awardedCount")]
        public long AwardedCount { get; set; }

        [JsonPropertyName("winRatePercentage")]
        public double WinRatePercentage { get; set; }
    }

    public class BadgeAwardedDate
    {
        [JsonPropertyName("badgeId")]
        public long BadgeId { get; set; }

        [JsonPropertyName("awardedDate")]
        public DateTime AwardedDate { get; set; }
    }
}
