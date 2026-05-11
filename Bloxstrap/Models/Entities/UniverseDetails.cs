using Bloxstrap.RobloxInterfaces;

namespace Bloxstrap.Models.Entities
{
    /// <summary>
    /// Explicit loading. Load from cache before and after a fetch.
    /// </summary>
    public class UniverseDetails
    {
        private static List<UniverseDetails> _cache { get; set; } = new();

        public GameDetailResponse Data { get; set; } = null!;

        /// <summary>
        /// Returns data for a 128x128 icon
        /// </summary>
        public ThumbnailResponse Thumbnail { get; set; } = null!;

        public static UniverseDetails? LoadFromCache(long id)
        {
            var cacheQuery = _cache.Where(x => x.Data?.Id == id);

            if (cacheQuery.Any())
                return cacheQuery.First();

            return null;
        }

        public static Task FetchSingle(long id) => FetchBulk(id.ToString());

        public static async Task FetchBulk(string ids)
        {
            // some universes can't be viewed by logged out user (ex. 18+)
            // requesting multiple universe ids bypasses this
            if (!ids.Contains(','))
                ids += ",1";

            Uri gameDetailsUrl = UrlBuilder.BuildApiUrl("games", $"v1/games?universeIds={ids}");
            Uri thumbnailsUrl = UrlBuilder.BuildApiUrl("thumbnails", $"v1/games/icons?universeIds={ids}&returnPolicy=PlaceHolder&size=128x128&format=Png&isCircular=false");
            ApiArrayResponse<GameDetailResponse> gameDetailResponse;

            gameDetailResponse = await Http.GetJson<ApiArrayResponse<GameDetailResponse>>(gameDetailsUrl);

            if (!gameDetailResponse.Data.Any())
                throw new InvalidHTTPResponseException("Roblox API for Game Details returned invalid data");

            var universeThumbnailResponse = await Http.GetJson<ApiArrayResponse<ThumbnailResponse>>(thumbnailsUrl);

            if (!universeThumbnailResponse.Data.Any())
                throw new InvalidHTTPResponseException("Roblox API for Game Thumbnails returned invalid data");

            foreach (string strId in ids.Split(','))
            {
                long id = long.Parse(strId);

                _cache.Add(new UniverseDetails
                {
                    Data = gameDetailResponse.Data.Where(x => x.Id == id).First(),
                    Thumbnail = universeThumbnailResponse.Data.Where(x => x.TargetId == id).First(),
                });
            }
        }
    }
}
