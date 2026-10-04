namespace Bloxstrap.RobloxInterfaces
{
    public static class PrivacySettings
    {
        private const string OnlineSetting = "whoCanSeeMyOnlineStatus";

        private const string JoinSetting = "whoCanJoinMeInExperiences";

        private static Uri SettingsUrl => UrlBuilder.BuildApiUrl("apis", "user-settings-api/v1/user-settings/settings-and-options");

        private static Uri UpdateUrl => UrlBuilder.BuildApiUrl("apis", "user-settings-api/v1/user-settings");

        public static readonly IReadOnlyList<string> OnlineLevels = new[] { "AllUsers", "FriendsFollowingAndFollowers", "FriendsAndFollowing", "Friends", "TrustedFriends", "NoOne" };

        public static readonly IReadOnlyList<string> JoinLevels = new[] { "All", "Followers", "Following", "Friends", "TrustedFriends", "NoOne" };

        public static async Task<PrivacyState> FetchAsync()
        {
            var response = await Http.AuthGetJson<UserSettingsResponse>(SettingsUrl);

            return new PrivacyState
            {
                Online = response.OnlineStatus?.CurrentValue,
                Join = response.JoinStatus?.CurrentValue,
                OnlineOptions = response.OnlineStatus?.Available ?? Array.Empty<string>(),
                JoinOptions = response.JoinStatus?.Available ?? Array.Empty<string>()
            };
        }

        public static int Rank(string? value)
        {
            if (value is null)
                return -1;

            int rank = OnlineLevels.ToList().IndexOf(value);

            return rank >= 0 ? rank : JoinLevels.ToList().IndexOf(value);
        }

        public static IReadOnlyList<(string Setting, string Value)> PlanOnlineVisibility(string online, string? join)
        {
            int onlineRank = OnlineLevels.ToList().IndexOf(online);

            if (onlineRank < 0)
                throw new ArgumentException($"Unknown online visibility '{online}'", nameof(online));

            var plan = new List<(string, string)>();

            int joinRank = Rank(join);

            if (joinRank >= 0 && joinRank < onlineRank)
            {
                bool joinNames = JoinLevels.Contains(join!);

                plan.Add((JoinSetting, joinNames ? JoinLevels[onlineRank] : OnlineLevels[onlineRank]));
            }

            plan.Add((OnlineSetting, online));

            return plan;
        }

        public static bool GameVisibilityAllowed(string join, string? online)
        {
            int onlineRank = Rank(online);

            return onlineRank < 0 || Rank(join) >= onlineRank;
        }

        public static async Task<bool> SetOnlineVisibilityAsync(string online, string? join)
        {
            const string LOG_IDENT = "PrivacySettings::SetOnlineVisibilityAsync";

            var plan = PlanOnlineVisibility(online, join);

            await App.Cookies.EnsureBrowserTrackerAsync();

            App.Logger.WriteLine(LOG_IDENT, $"Setting online visibility to {online} with joining at {join ?? "unreported"}: {String.Join(", then ", plan.Select(x => $"{x.Setting}={x.Value}"))}");

            foreach ((string setting, string value) in plan)
                await PostAsync(setting, value);

            return plan.Count > 1;
        }

        public static async Task SetGameVisibilityAsync(string join, string? online)
        {
            const string LOG_IDENT = "PrivacySettings::SetGameVisibilityAsync";

            if (!JoinLevels.Contains(join))
                throw new ArgumentException($"Unknown game visibility '{join}'", nameof(join));

            if (!GameVisibilityAllowed(join, online))
                throw new ArgumentException($"Game visibility '{join}' is wider than online visibility '{online}'", nameof(join));

            await App.Cookies.EnsureBrowserTrackerAsync();

            App.Logger.WriteLine(LOG_IDENT, $"Setting game visibility to {join} with online at {online ?? "unreported"}");

            await PostAsync(JoinSetting, join);
        }

        private static Task PostAsync(string setting, string value) =>
            AccountRequests.SendAsync(HttpMethod.Post, UpdateUrl, new Dictionary<string, string> { [setting] = value }, setting, value);
    }
}
