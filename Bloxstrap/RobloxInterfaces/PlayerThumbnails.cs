namespace Bloxstrap.RobloxInterfaces
{
    public static class PlayerThumbnails
    {
        private const string Size = "48x48";

        private const int BatchSize = 100;

        private const int PoolAttempts = 90;

        private const int HighestUserId = 200_000_000;

        private static Uri BatchUrl => UrlBuilder.BuildApiUrl("thumbnails", "v1/batch");

        private static readonly SemaphoreSlim _poolLock = new(1, 1);

        private static readonly Random _random = new();

        private static IReadOnlyList<string>? _pool;

        public static async Task<Dictionary<string, string>> FetchByTokenAsync(IReadOnlyList<string> tokens)
        {
            const string LOG_IDENT = "PlayerThumbnails::FetchByTokenAsync";

            var icons = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (string[] chunk in tokens.Chunk(BatchSize))
            {
                var requests = new List<ThumbnailBatchRequest>();
                var byRequestId = new Dictionary<string, string>(StringComparer.Ordinal);

                for (int i = 0; i < chunk.Length; i++)
                {
                    string requestId = $"{i}:{chunk[i]}:AvatarHeadShot:{Size}:png:regular";

                    requests.Add(new ThumbnailBatchRequest { RequestId = requestId, Token = chunk[i], Size = Size });

                    byRequestId[requestId] = chunk[i];
                }

                try
                {
                    var message = new HttpRequestMessage(HttpMethod.Post, BatchUrl)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(requests), Encoding.UTF8, "application/json")
                    };

                    var response = await Http.SendJson<ApiArrayResponse<ThumbnailResponse>>(message);

                    foreach (ThumbnailResponse thumbnail in response.Data)
                    {
                        if (thumbnail.State != "Completed" || String.IsNullOrEmpty(thumbnail.ImageUrl))
                            continue;

                        if (byRequestId.TryGetValue(thumbnail.RequestId ?? String.Empty, out string? token))
                            icons[token] = thumbnail.ImageUrl;
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Failed to resolve player thumbnails");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }
            }

            return icons;
        }

        public static async Task<IReadOnlyList<string>> FetchPoolAsync()
        {
            const string LOG_IDENT = "PlayerThumbnails::FetchPoolAsync";

            if (_pool is not null)
                return _pool;

            await _poolLock.WaitAsync();

            try
            {
                if (_pool is not null)
                    return _pool;

                var ids = new HashSet<long>();

                while (ids.Count < PoolAttempts)
                    ids.Add(_random.Next(1, HighestUserId));

                var icons = new List<string>();

                foreach (long[] chunk in ids.Chunk(BatchSize))
                {
                    var response = await Http.GetJson<ApiArrayResponse<ThumbnailResponse>>(
                        UrlBuilder.BuildApiUrl("thumbnails", $"v1/users/avatar-headshot?userIds={String.Join(',', chunk)}&size={Size}&format=Png&isCircular=false"));

                    icons.AddRange(response.Data
                        .Where(x => x.State == "Completed" && !String.IsNullOrEmpty(x.ImageUrl))
                        .Select(x => x.ImageUrl!));
                }

                List<string> pool = icons.Distinct(StringComparer.Ordinal).ToList();

                _pool = pool;

                App.Logger.WriteLine(LOG_IDENT, $"Pooled {pool.Count} faces");

                return pool;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to pool filler thumbnails");
                App.Logger.WriteException(LOG_IDENT, ex);

                return Array.Empty<string>();
            }
            finally
            {
                _poolLock.Release();
            }
        }
    }
}
