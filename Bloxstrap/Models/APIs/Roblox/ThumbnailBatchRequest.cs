namespace Bloxstrap.Models.APIs.Roblox
{
    public class ThumbnailBatchRequest
    {
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; } = String.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "AvatarHeadShot";

        [JsonPropertyName("targetId")]
        public long TargetId { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; } = String.Empty;

        [JsonPropertyName("format")]
        public string Format { get; set; } = "png";

        [JsonPropertyName("size")]
        public string Size { get; set; } = "48x48";

        [JsonPropertyName("isCircular")]
        public bool IsCircular { get; set; }
    }
}
