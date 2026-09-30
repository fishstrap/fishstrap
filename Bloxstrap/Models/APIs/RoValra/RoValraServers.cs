namespace Bloxstrap.Models.APIs.RoValra
{
    public class RoValraServers
    {
        [JsonPropertyName("servers")]
        public List<RoValraServer> Servers { get; set; } = null!;

        [JsonPropertyName("next_cursor")]
        public int? NextCursor { get; set; }
    }

    public class RoValraServer
    {
        [JsonPropertyName("server_id")]
        public string ServerId { get; set; } = string.Empty;

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("datacenter_id")]
        public long DatacenterId { get; set; }

        [JsonPropertyName("place_version")]
        public int? PlaceVersion { get; set; }

        [JsonPropertyName("first_seen")]
        public string? FirstSeen { get; set; }

        public DateTime? FirstSeenUtc => DateTime.TryParse(FirstSeen, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime seen) ? seen : null;
    }
}
