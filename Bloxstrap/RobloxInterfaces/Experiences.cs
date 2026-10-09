namespace Bloxstrap.RobloxInterfaces
{
    public static class Experiences
    {
        private const int BatchSize = 50;

        private const int FavoritesLimit = 50;

        private const int ContinueLimit = 24;

        private const long ContinueTopicId = 100000003;

        private static readonly string SearchSession = Guid.NewGuid().ToString();

        public static async Task<List<GameTile>> SearchAsync(string query)
        {
            var response = await Http.GetJson<OmniSearchResponse>(UrlBuilder.BuildApisUrl(
                $"search-api/omni-search?searchQuery={Uri.EscapeDataString(query)}&pageType=all&sessionId={SearchSession}"));

            var tiles = response.SearchResults
                .Where(x => x.ContentGroupType == "Game")
                .SelectMany(x => x.Contents)
                .Where(x => !x.IsSponsored && x.RootPlaceId != 0)
                .GroupBy(x => x.UniverseId)
                .Select(x => x.First())
                .Select(x => new GameTile
                {
                    UniverseId = x.UniverseId,
                    PlaceId = x.RootPlaceId,
                    Name = x.Name,
                    Playing = x.PlayerCount
                })
                .ToList();

            await PopulateIconsAsync(tiles);

            return tiles;
        }

        public static async Task<List<GameTile>> FavoritesAsync(long userId)
        {
            var response = await Http.GetJson<ApiPageResponse<FavoriteGameResponse>>(UrlBuilder.BuildApiUrl("games",
                $"v2/users/{userId}/favorite/games?limit={FavoritesLimit}&sortOrder=Desc"));

            var tiles = response.Data
                .Where(x => x.RootPlace is not null && x.RootPlace.Id != 0)
                .Select(x => new GameTile
                {
                    UniverseId = x.Id,
                    PlaceId = x.RootPlace!.Id,
                    Name = x.Name
                })
                .ToList();

            await Task.WhenAll(PopulatePlayingAsync(tiles), PopulateIconsAsync(tiles));

            return tiles;
        }

        public static async Task<List<GameTile>> ContinueAsync(IEnumerable<long> recentUniverses)
        {
            const string LOG_IDENT = "Experiences::ContinueAsync";

            var universeIds = new List<long>();

            if (await App.Cookies.EnsureLoadedAsync())
            {
                try
                {
                    universeIds = await ContinueFromRobloxAsync();
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Couldn't read the Continue sort, using local history instead");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }
            }

            if (universeIds.Count == 0)
                universeIds = recentUniverses.Where(x => x > 0).Distinct().ToList();

            universeIds = universeIds.Take(ContinueLimit).ToList();

            if (universeIds.Count == 0)
                return new List<GameTile>();

            List<GameTile> tiles = await TilesAsync(universeIds);

            await PopulateIconsAsync(tiles);

            return tiles;
        }

        private static async Task<List<long>> ContinueFromRobloxAsync()
        {
            const string LOG_IDENT = "Experiences::ContinueFromRobloxAsync";

            JsonElement response = await AccountRequests.PostJsonAsync<JsonElement>(
                UrlBuilder.BuildApisUrl("discovery-api/omni-recommendation"),
                new { pageType = "Home", sessionId = SearchSession });

            if (response.ValueKind != JsonValueKind.Object
                || !response.TryGetProperty("sorts", out JsonElement sorts)
                || sorts.ValueKind != JsonValueKind.Array)
            {
                App.Logger.WriteLine(LOG_IDENT, "The home recommendations had no sorts");
                return new List<long>();
            }

            var topics = new List<string>();

            foreach (JsonElement sort in sorts.EnumerateArray())
            {
                string topic = sort.TryGetProperty("topic", out JsonElement name) && name.ValueKind == JsonValueKind.String
                    ? name.GetString() ?? String.Empty
                    : String.Empty;

                bool continueSort = sort.TryGetProperty("topicId", out JsonElement id)
                    && id.ValueKind == JsonValueKind.Number
                    && id.TryGetInt64(out long topicId)
                    && topicId == ContinueTopicId;

                if (!continueSort && !topic.Equals("Continue", StringComparison.OrdinalIgnoreCase))
                {
                    topics.Add(topic);
                    continue;
                }

                if (!sort.TryGetProperty("recommendationList", out JsonElement list) || list.ValueKind != JsonValueKind.Array)
                    return new List<long>();

                return list.EnumerateArray()
                    .Where(x => x.ValueKind == JsonValueKind.Object
                        && x.TryGetProperty("contentType", out JsonElement type)
                        && type.ValueKind == JsonValueKind.String
                        && type.GetString() == "Game")
                    .Select(x => x.TryGetProperty("contentId", out JsonElement content)
                        && content.ValueKind == JsonValueKind.Number
                        && content.TryGetInt64(out long universe) ? universe : 0)
                    .Where(x => x > 0)
                    .Distinct()
                    .ToList();
            }

            App.Logger.WriteLine(LOG_IDENT, $"No Continue sort among: {String.Join(", ", topics)}");

            return new List<long>();
        }

        private static async Task<List<GameTile>> TilesAsync(List<long> universeIds)
        {
            var details = new Dictionary<long, GameDetailResponse>();

            foreach (long[] chunk in universeIds.Chunk(BatchSize))
            {
                Uri url = UrlBuilder.BuildApiUrl("games", $"v1/games?universeIds={String.Join(',', chunk)}");

                var response = App.Cookies.Loaded
                    ? await Http.AuthGetJson<ApiArrayResponse<GameDetailResponse>>(url)
                    : await Http.GetJson<ApiArrayResponse<GameDetailResponse>>(url);

                foreach (GameDetailResponse detail in response.Data)
                    details[detail.Id] = detail;
            }

            return universeIds
                .Where(details.ContainsKey)
                .Select(id => details[id])
                .Where(x => x.RootPlaceId != 0)
                .Select(x => new GameTile
                {
                    UniverseId = x.Id,
                    PlaceId = x.RootPlaceId,
                    Name = x.Name,
                    Playing = x.Playing
                })
                .ToList();
        }

        private static async Task PopulatePlayingAsync(List<GameTile> tiles)
        {
            const string LOG_IDENT = "Experiences::PopulatePlayingAsync";

            try
            {
                foreach (GameTile[] chunk in tiles.Chunk(BatchSize))
                {
                    Uri url = UrlBuilder.BuildApiUrl("games", $"v1/games?universeIds={String.Join(',', chunk.Select(x => x.UniverseId))}");

                    var response = App.Cookies.Loaded
                        ? await Http.AuthGetJson<ApiArrayResponse<GameDetailResponse>>(url)
                        : await Http.GetJson<ApiArrayResponse<GameDetailResponse>>(url);

                    foreach (GameDetailResponse detail in response.Data)
                    {
                        GameTile? tile = chunk.FirstOrDefault(x => x.UniverseId == detail.Id);

                        if (tile is not null)
                            tile.Playing = detail.Playing;
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to fetch player counts");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private static async Task PopulateIconsAsync(List<GameTile> tiles)
        {
            const string LOG_IDENT = "Experiences::PopulateIconsAsync";

            try
            {
                foreach (GameTile[] chunk in tiles.Chunk(BatchSize))
                {
                    var response = await Http.GetJson<ApiArrayResponse<ThumbnailResponse>>(UrlBuilder.BuildApiUrl("thumbnails",
                        $"v1/games/icons?universeIds={String.Join(',', chunk.Select(x => x.UniverseId))}&returnPolicy=PlaceHolder&size=256x256&format=Png&isCircular=false"));

                    foreach (ThumbnailResponse thumbnail in response.Data)
                    {
                        GameTile? tile = chunk.FirstOrDefault(x => x.UniverseId == thumbnail.TargetId);

                        if (tile is not null)
                            tile.IconUrl = thumbnail.ImageUrl;
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to fetch experience icons");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        public static void Join(GameTile tile)
        {
            const string LOG_IDENT = "Experiences::Join";

            App.Logger.WriteLine(LOG_IDENT, $"Joining {tile.Name} ({tile.PlaceId})");

            GameServers.Launch($"placeId={tile.PlaceId}");
        }
    }
}
