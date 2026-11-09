namespace Bloxstrap.Models.APIs.Roblox
{
    public class ApiPageResponse<T>
    {
        [JsonPropertyName("previousPageCursor")]
        public string? PreviousPageCursor { get; set; }

        [JsonPropertyName("nextPageCursor")]
        public string? NextPageCursor { get; set; }

        [JsonPropertyName("data")]
        public List<T> Data { get; set; } = new();
    }
}
