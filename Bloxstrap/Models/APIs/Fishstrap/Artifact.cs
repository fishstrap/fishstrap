namespace Bloxstrap.Models.APIs.Fishstrap
{
    public class Artifact
    {
        [JsonPropertyName("hash")]
        public string? Hash { get; set; }
        [JsonPropertyName("branch")]
        public string? Branch { get; set; }
        [JsonPropertyName("url")]
        public string? Url { get; set; }
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}