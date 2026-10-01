using Bloxstrap.Models.RobloxApi;

namespace Bloxstrap.Models.Entities
{
    public class UserDetails
    {
        private static List<UserDetails> _cache { get; set; } = new();

        public GetUserResponse Data { get; set; } = null!;

        public ThumbnailResponse Thumbnail { get; set; } = null!;

        public static async Task<Dictionary<long, UserDetails>> FetchBatch(List<long> userIds)
        {
            var users = new Dictionary<long, UserDetails>();
            var missing = new List<long>();

            foreach (long userId in userIds.Distinct())
            {
                var cached = _cache.FirstOrDefault(x => x.Data?.Id == userId);

                if (cached is not null)
                    users[userId] = cached;
                else
                    missing.Add(userId);
            }

            if (!missing.Any())
                return users;

            var payload = new UserDetailsBatchRequest { UserIds = missing };

            var usersResponse = await Http.SendJson<ApiArrayResponse<GetUserResponse>>(new HttpRequestMessage
            {
                RequestUri = UrlBuilder.BuildApiUrl("users", "v1/users"),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            });

            if (usersResponse is null)
                throw new InvalidHTTPResponseException("Roblox API for User Details returned invalid data");

            var thumbnailResponse = await Http.GetJson<ApiArrayResponse<ThumbnailResponse>>(
                UrlBuilder.BuildApiUrl("thumbnails", $"v1/users/avatar-headshot?userIds={String.Join(',', missing)}&size=180x180&format=Png&isCircular=false"));

            if (thumbnailResponse is null)
                throw new InvalidHTTPResponseException("Roblox API for Thumbnails returned invalid data");

            foreach (var user in usersResponse.Data)
            {
                var thumbnail = thumbnailResponse.Data.FirstOrDefault(x => x.TargetId == user.Id);

                if (thumbnail is null)
                    continue;

                var details = new UserDetails { Data = user, Thumbnail = thumbnail };

                users[user.Id] = details;
                _cache.Add(details);
            }

            return users;
        }

        public static async Task<UserDetails> Fetch(long id)
        {
            var cacheQuery = _cache.Where(x => x.Data?.Id == id);

            if (cacheQuery.Any())
                return cacheQuery.First();

            var userResponse = await Http.GetJson<GetUserResponse>(new Uri($"https://users.roblox.com/v1/users/{id}"));

            if (userResponse is null)
                throw new InvalidHTTPResponseException("Roblox API for User Details returned invalid data");

            // we can remove '-headshot' from the url if we want a full avatar picture
            var thumbnailResponse = await Http.GetJson<ApiArrayResponse<ThumbnailResponse>>(new Uri($"https://thumbnails.roblox.com/v1/users/avatar-headshot?userIds={id}&size=180x180&format=Png&isCircular=false"));

            if (thumbnailResponse is null || !thumbnailResponse.Data.Any())
                throw new InvalidHTTPResponseException("Roblox API for Thumbnails returned invalid data");

            var details = new UserDetails
            {
                Data = userResponse,
                Thumbnail = thumbnailResponse.Data.First()
            };

            _cache.Add(details);

            return details;
        }
    }
}