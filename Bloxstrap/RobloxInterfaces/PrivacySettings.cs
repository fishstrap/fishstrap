namespace Bloxstrap.RobloxInterfaces
{
    public static class PrivacySettings
    {
        private const string OnlineSetting = "whoCanSeeMyOnlineStatus";

        private const string JoinSetting = "whoCanJoinMeInExperiences";

        private static readonly Uri SettingsUrl = new("https://apis.roblox.com/user-settings-api/v1/user-settings/settings-and-options");

        private static readonly Uri UpdateUrl = new("https://apis.roblox.com/user-settings-api/v1/user-settings");

        public static readonly IReadOnlyList<string> OnlineLevels = new[] { "AllUsers", "FriendsFollowingAndFollowers", "FriendsAndFollowing", "Friends", "TrustedFriends", "NoOne" };

        private static readonly IReadOnlyList<string> JoinLevels = new[] { "All", "Followers", "Following", "Friends", "TrustedFriends", "NoOne" };

        public static async Task<(string? Online, string? Join)> FetchAsync()
        {
            var response = await Http.AuthGetJson<UserSettingsResponse>(SettingsUrl);

            return (response.OnlineStatus?.CurrentValue, response.JoinStatus?.CurrentValue);
        }

        private static int Rank(string? value)
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

        private static Task PostAsync(string setting, string value) =>
            AccountRequests.SendAsync(HttpMethod.Post, UpdateUrl, new Dictionary<string, string> { [setting] = value }, setting, value);
    }
}
