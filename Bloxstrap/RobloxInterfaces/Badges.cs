namespace Bloxstrap.RobloxInterfaces
{
    public static class Badges
    {
        private const int PageSize = 100;

        private const int MaxPages = 5;

        private const int RemoveAttempts = 3;

        private static readonly TimeSpan RateLimitBackoff = TimeSpan.FromSeconds(2);

        public static async Task<List<Badge>> FetchAsync(long universeId, long userId)
        {
            var badges = new List<Badge>();

            if (universeId == 0)
                return badges;

            string? cursor = null;

            for (int page = 0; page < MaxPages; page++)
            {
                var response = await Http.GetJson<ApiPageResponse<BadgeResponse>>(
                    UrlBuilder.BuildApiUrl("badges", $"v1/universes/{universeId}/badges?limit={PageSize}&sortOrder=Asc&cursor={cursor}"));

                if (response is null)
                    break;

                foreach (BadgeResponse badge in response.Data)
                {
                    if (!badge.Enabled)
                        continue;

                    badges.Add(new Badge
                    {
                        Id = badge.Id,
                        Name = String.IsNullOrEmpty(badge.DisplayName) ? badge.Name : badge.DisplayName!,
                        Description = badge.DisplayDescription ?? badge.Description ?? String.Empty,
                        WinRatePercentage = badge.Statistics?.WinRatePercentage ?? 0,
                        PastDayAwardedCount = badge.Statistics?.PastDayAwardedCount ?? 0,
                        AwardedCount = badge.Statistics?.AwardedCount ?? 0
                    });
                }

                cursor = response.NextPageCursor;

                if (String.IsNullOrEmpty(cursor))
                    break;
            }

            await PopulateAwardedAsync(badges, userId);
            await PopulateIconsAsync(badges);

            return badges;
        }

        private static async Task PopulateAwardedAsync(List<Badge> badges, long userId)
        {
            const string LOG_IDENT = "Badges::PopulateAwardedAsync";

            if (userId == 0 || !badges.Any())
                return;

            if (!await App.Cookies.EnsureLoadedAsync())
            {
                App.Logger.WriteLine(LOG_IDENT, "No session to ask with, leaving badge progress unknown");
                return;
            }

            try
            {
                Dictionary<long, DateTime> awarded = await AwardedDatesAsync(userId, badges.Select(x => x.Id));

                foreach (Badge badge in badges)
                {
                    if (awarded.TryGetValue(badge.Id, out DateTime date))
                    {
                        badge.Awarded = true;
                        badge.AwardedDate = date;
                    }

                    badge.AwardedKnown = true;
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to fetch awarded dates");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        public static async Task RemoveAsync(long badgeId)
        {
            Uri url = UrlBuilder.BuildApiUrl("badges", $"v1/user/badges/{badgeId}");

            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    await AccountRequests.DeleteAsync(url);
                    return;
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests && attempt < RemoveAttempts)
                {
                    await Task.Delay(RateLimitBackoff * attempt);
                }
            }
        }

        public static async Task<Dictionary<long, DateTime>> AwardedDatesAsync(long userId, IEnumerable<long> badgeIds)
        {
            var awarded = new Dictionary<long, DateTime>();

            foreach (long[] chunk in badgeIds.Chunk(PageSize))
            {
                var response = await Http.AuthGetJson<ApiArrayResponse<BadgeAwardedDate>>(
                    UrlBuilder.BuildApiUrl("badges", $"v1/users/{userId}/badges/awarded-dates?badgeIds={String.Join(',', chunk)}"));

                if (response?.Data is null)
                    continue;

                foreach (BadgeAwardedDate date in response.Data)
                    awarded[date.BadgeId] = date.AwardedDate;
            }

            return awarded;
        }

        private static async Task PopulateIconsAsync(List<Badge> badges)
        {
            const string LOG_IDENT = "Badges::PopulateIconsAsync";

            if (!badges.Any())
                return;

            try
            {
                foreach (long[] chunk in badges.Select(x => x.Id).Chunk(PageSize))
                {
                    var response = await Http.GetJson<ApiArrayResponse<ThumbnailResponse>>(
                        UrlBuilder.BuildApiUrl("thumbnails", $"v1/badges/icons?badgeIds={String.Join(',', chunk)}&size=150x150&format=Png&isCircular=false"));

                    if (response?.Data is null)
                        continue;

                    foreach (ThumbnailResponse thumbnail in response.Data)
                    {
                        Badge? badge = badges.FirstOrDefault(x => x.Id == thumbnail.TargetId);

                        if (badge is not null)
                            badge.IconUrl = thumbnail.ImageUrl;
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to fetch badge icons");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }
    }
}
