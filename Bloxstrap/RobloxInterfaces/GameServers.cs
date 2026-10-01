using Bloxstrap.AppData;
using Bloxstrap.Enums.Overlay;
using Bloxstrap.Models.APIs.RoValra;

namespace Bloxstrap.RobloxInterfaces
{
    public static class GameServers
    {
        private const int PageSize = 100;

        private const int MaxStatsPages = 5;

        private const int MaxRegionPages = 3;

        private const int MaxFaces = 5;

        private const int DetailsBatchSize = 50;

        private const int DetailsReach = 100;

        private static readonly Uri DatacentersUrl = new("https://apis.rovalra.com/v1/datacenters/list");

        private static List<ServerRegion>? _regions;

        public static async Task<List<ServerRegion>> FetchRegionsAsync()
        {
            const string LOG_IDENT = "GameServers::FetchRegionsAsync";

            if (_regions is not null)
                return _regions;

            var regions = new List<ServerRegion> { AllRegions() };

            try
            {
                var datacenters = await Http.GetJson<List<RoValraDatacenter>>(DatacentersUrl);

                regions.AddRange(datacenters
                    .Select(x => x.Location?.Country)
                    .Where(x => !String.IsNullOrEmpty(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(code => new ServerRegion { Code = code!, Name = RegionName(code!) })
                    .OrderBy(x => x.Name, StringComparer.Ordinal));
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to fetch datacenter regions");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            _regions = regions;

            return _regions;
        }

        public static ServerRegion AllRegions() => new() { Code = String.Empty, Name = Strings.Menu_Overlay_Servers_AllRegions };

        private static string RegionName(string code)
        {
            try
            {
                return new RegionInfo(code).EnglishName;
            }
            catch (ArgumentException)
            {
                return code.ToUpperInvariant();
            }
        }

        public static async Task<List<GameServer>> FetchAsync(long placeId, string currentJobId, Order order, bool excludeFull, ServerRegion? region)
        {
            var servers = new List<GameServer>();

            if (placeId == 0)
                return servers;

            List<GameServerResponse> live = await FetchLiveAsync(placeId);

            var stats = new Dictionary<string, GameServerResponse>(StringComparer.OrdinalIgnoreCase);

            foreach (GameServerResponse server in live)
            {
                if (!String.IsNullOrEmpty(server.Id))
                    stats[server.Id] = server;
            }

            if (region is null || region.IsAll)
            {
                servers.AddRange(stats.Values.Select(x => new GameServer
                {
                    JobId = x.Id,
                    Playing = x.Playing,
                    MaxPlayers = x.MaxPlayers,
                    Fps = x.Fps,
                    PlayerTokens = x.PlayerTokens,
                    IsCurrent = x.Id.Equals(currentJobId, StringComparison.OrdinalIgnoreCase)
                }));
            }
            else
            {
                foreach (RoValraServer server in await FetchRegionIndexAsync(placeId, region.Code))
                {
                    stats.TryGetValue(server.ServerId, out GameServerResponse? stat);

                    servers.Add(new GameServer
                    {
                        JobId = server.ServerId,
                        Playing = stat?.Playing,
                        MaxPlayers = stat?.MaxPlayers,
                        Fps = stat?.Fps,
                        PlayerTokens = stat?.PlayerTokens ?? new List<string>(),
                        StartedAt = server.FirstSeenUtc,
                        PlaceVersion = server.PlaceVersion,
                        IsCurrent = server.ServerId.Equals(currentJobId, StringComparison.OrdinalIgnoreCase)
                    });
                }
            }

            if (excludeFull)
                servers.RemoveAll(x => x.IsFull && !x.IsCurrent);

            servers = order == Order.Ascending
                ? servers.OrderBy(x => x.HasStats ? 0 : 1).ThenBy(x => x.Playing ?? 0).ToList()
                : servers.OrderBy(x => x.HasStats ? 0 : 1).ThenByDescending(x => x.Playing ?? 0).ToList();

            return servers;
        }

        public static async Task PopulateIconsAsync(IReadOnlyCollection<GameServer> servers)
        {
            const string LOG_IDENT = "GameServers::PopulateIconsAsync";

            if (servers.Count == 0)
                return;

            try
            {
                List<string> tokens = servers
                    .SelectMany(x => x.PlayerTokens.Take(MaxFaces))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                Dictionary<string, string> real = tokens.Count > 0
                    ? await PlayerThumbnails.FetchByTokenAsync(tokens)
                    : new Dictionary<string, string>();

                var resolved = servers.Select(x => (Server: x, Faces: Faces(x, real))).ToList();

                IReadOnlyList<string> pool = resolved.Any(x => x.Faces.Count == 0)
                    ? await PlayerThumbnails.FetchPoolAsync()
                    : Array.Empty<string>();

                foreach ((GameServer server, List<string> faces) in resolved)
                {
                    server.PlayerIcons.Clear();
                    server.PlayerIcons.AddRange(faces.Count > 0 ? faces : Filler(server, pool));
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to resolve server thumbnails");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private static List<string> Faces(GameServer server, Dictionary<string, string> resolved) =>
            server.PlayerTokens
                .Take(MaxFaces)
                .Select(token => resolved.TryGetValue(token, out string? url) ? url : null)
                .Where(x => !String.IsNullOrEmpty(x))
                .Select(x => x!)
                .ToList();

        private static IEnumerable<string> Filler(GameServer server, IReadOnlyList<string> pool)
        {
            if (pool.Count == 0)
                return Array.Empty<string>();

            int count = Math.Clamp(server.Playing ?? MaxFaces, 1, MaxFaces);
            int offset = (server.JobId.GetHashCode() & Int32.MaxValue) % pool.Count;

            return Enumerable.Range(0, count).Select(i => pool[(offset + i) % pool.Count]);
        }

        public static async Task PopulateDetailsAsync(long placeId, IReadOnlyList<GameServer> servers)
        {
            const string LOG_IDENT = "GameServers::PopulateDetailsAsync";

            var wanted = servers
                .Take(DetailsReach)
                .Where(x => (x.StartedAt is null || x.PlaceVersion is null) && !String.IsNullOrEmpty(x.JobId))
                .ToList();

            if (placeId == 0 || wanted.Count == 0)
                return;

            var byId = new Dictionary<string, GameServer>(StringComparer.OrdinalIgnoreCase);

            foreach (GameServer server in wanted)
                byId.TryAdd(server.JobId, server);

            async Task FetchBatch(GameServer[] batch)
            {
                try
                {
                    var response = await Http.GetJson<RoValraServers>(DetailsUrl(placeId, batch.Select(x => x.JobId)));

                    foreach (RoValraServer detail in response?.Servers ?? new List<RoValraServer>())
                    {
                        if (!byId.TryGetValue(detail.ServerId, out GameServer? server))
                            continue;

                        server.StartedAt ??= detail.FirstSeenUtc;
                        server.PlaceVersion ??= detail.PlaceVersion;
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Failed to fetch details for {batch.Length} servers");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }
            }

            await Task.WhenAll(wanted.Chunk(DetailsBatchSize).Select(FetchBatch));
        }

        private static Uri DetailsUrl(long placeId, IEnumerable<string> jobIds) =>
            new($"https://apis.rovalra.com/v1/servers/details?place_id={placeId}&server_ids={String.Join(',', jobIds)}");

        public static async Task<HashSet<string>> StillRunningAsync(long placeId, IEnumerable<string> jobIds)
        {
            var running = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ids = jobIds.Where(x => !String.IsNullOrEmpty(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (placeId == 0 || ids.Count == 0)
                return running;

            foreach (string[] batch in ids.Chunk(DetailsBatchSize))
            {
                var response = await Http.GetJson<RoValraServers>(DetailsUrl(placeId, batch));

                foreach (RoValraServer server in response?.Servers ?? new List<RoValraServer>())
                    running.Add(server.ServerId);
            }

            return running;
        }

        public static async Task<(string? JobId, bool AlreadyClosest)> FindClosestAsync(long placeId, string currentJobId)
        {
            var response = await Http.AuthGetJson<ApiPageResponse<GameServerResponse>>(
                UrlBuilder.BuildApiUrl("games", $"v2/games/{placeId}/servers/Public?cursor=&sortOrder=Desc&excludeFullGames=true&orderBy=BestLatency"));

            GameServerResponse? best = response?.Data?.FirstOrDefault(x => !String.IsNullOrEmpty(x.Id) && x.Playing < x.MaxPlayers);

            if (best is null)
                return (null, false);

            if (best.Id.Equals(currentJobId, StringComparison.OrdinalIgnoreCase))
                return (null, true);

            return (best.Id, false);
        }

        public static GameServer? PickHopTarget(IEnumerable<GameServer> servers)
        {
            var open = servers.Where(x => !x.IsCurrent && !x.IsFull).ToList();

            var candidates = open.Where(x => x.HasStats && x.Playing > 0).ToList();

            if (candidates.Count == 0)
                candidates = open;

            return candidates.Count == 0 ? null : candidates[Random.Shared.Next(candidates.Count)];
        }

        private static async Task<List<GameServerResponse>> FetchLiveAsync(long placeId)
        {
            const string LOG_IDENT = "GameServers::FetchLiveAsync";

            var servers = new List<GameServerResponse>();
            string? cursor = null;

            try
            {
                for (int page = 0; page < MaxStatsPages; page++)
                {
                    var response = await Http.GetJson<ApiPageResponse<GameServerResponse>>(
                        UrlBuilder.BuildApiUrl("games", $"v1/games/{placeId}/servers/Public?limit={PageSize}&sortOrder=Desc&cursor={cursor}"));

                    if (response is null)
                        break;

                    servers.AddRange(response.Data);

                    cursor = response.NextPageCursor;

                    if (String.IsNullOrEmpty(cursor))
                        break;
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to fetch the server list");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            return servers;
        }

        private static async Task<List<RoValraServer>> FetchRegionIndexAsync(long placeId, string region)
        {
            const string LOG_IDENT = "GameServers::FetchRegionIndexAsync";

            var index = new List<RoValraServer>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int? cursor = null;

            try
            {
                for (int page = 0; page < MaxRegionPages; page++)
                {
                    string query = $"https://apis.rovalra.com/v1/servers/region?place_id={placeId}&region={region}";

                    if (cursor is not null)
                        query += $"&cursor={cursor}";

                    var response = await Http.GetJson<RoValraServers>(new Uri(query));

                    if (response?.Servers is null)
                        break;

                    foreach (RoValraServer server in response.Servers)
                    {
                        if (!String.IsNullOrEmpty(server.ServerId) && seen.Add(server.ServerId))
                            index.Add(server);
                    }

                    cursor = response.NextCursor;

                    if (cursor is null)
                        break;
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to fetch the {region} server index for {placeId}");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            return index;
        }

        public static void Join(long placeId, string jobId)
        {
            const string LOG_IDENT = "GameServers::Join";

            App.Logger.WriteLine(LOG_IDENT, $"Joining {placeId}/{jobId}");

            Launch($"placeId={placeId}&gameInstanceId={jobId}");
        }

        public static void Launch(string query) =>
            Process.Start(new RobloxPlayerData().ExecutablePath, $"roblox://experiences/start?{query}");

        public static Uri GamePage(long placeId) => UrlBuilder.BuildApiUrl("www", $"games/{placeId}");
    }
}
