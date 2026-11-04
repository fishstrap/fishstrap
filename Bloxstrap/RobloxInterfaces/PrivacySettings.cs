namespace Bloxstrap.RobloxInterfaces
{
    public static class PrivacySettings
    {
        private const string OnlineSetting = "whoCanSeeMyOnlineStatus";

        private const string JoinSetting = "whoCanJoinMeInExperiences";

        private static Uri SettingsUrl => UrlBuilder.BuildApisUrl("user-settings-api/v1/user-settings/settings-and-options");

        private static Uri UpdateUrl => UrlBuilder.BuildApisUrl("user-settings-api/v1/user-settings");

        private static readonly IReadOnlyDictionary<PrivacyLevel, string> OnlineValues = new Dictionary<PrivacyLevel, string>
        {
            [PrivacyLevel.Everyone] = "AllUsers",
            [PrivacyLevel.FriendsFollowingAndFollowers] = "FriendsFollowingAndFollowers",
            [PrivacyLevel.FriendsAndFollowing] = "FriendsAndFollowing",
            [PrivacyLevel.Friends] = "Friends",
            [PrivacyLevel.TrustedFriends] = "TrustedFriends",
            [PrivacyLevel.NoOne] = "NoOne"
        };

        private static readonly IReadOnlyDictionary<PrivacyLevel, string> JoinValues = new Dictionary<PrivacyLevel, string>
        {
            [PrivacyLevel.Everyone] = "All",
            [PrivacyLevel.FriendsFollowingAndFollowers] = "Followers",
            [PrivacyLevel.FriendsAndFollowing] = "Following",
            [PrivacyLevel.Friends] = "Friends",
            [PrivacyLevel.TrustedFriends] = "TrustedFriends",
            [PrivacyLevel.NoOne] = "NoOne"
        };

        public static async Task<PrivacyState> FetchAsync()
        {
            var response = await Http.AuthGetJson<UserSettingsResponse>(SettingsUrl);

            return new PrivacyState
            {
                Online = Parse(OnlineSetting, response.OnlineStatus?.CurrentValue),
                Join = Parse(JoinSetting, response.JoinStatus?.CurrentValue),
                OnlineOptions = Levels(OnlineSetting, response.OnlineStatus?.Available),
                JoinOptions = Levels(JoinSetting, response.JoinStatus?.Available)
            };
        }

        private static PrivacyLevel? Parse(string setting, string? value)
        {
            const string LOG_IDENT = "PrivacySettings::Parse";

            if (value is null)
                return null;

            foreach (var values in new[] { OnlineValues, JoinValues })
            {
                foreach ((PrivacyLevel level, string name) in values)
                {
                    if (name == value)
                        return level;
                }
            }

            App.Logger.WriteLine(LOG_IDENT, $"Unknown value '{value}' for {setting}");

            return null;
        }

        private static IReadOnlyList<PrivacyLevel> Levels(string setting, IReadOnlyList<string>? values) =>
            values?.Select(x => Parse(setting, x)).OfType<PrivacyLevel>().Distinct().ToList() ?? new List<PrivacyLevel>();

        public static IReadOnlyList<(string Setting, string Value)> PlanOnlineVisibility(PrivacyLevel online, PrivacyLevel? join)
        {
            var plan = new List<(string, string)>();

            if (join is PrivacyLevel current && current < online)
                plan.Add((JoinSetting, JoinValues[online]));

            plan.Add((OnlineSetting, OnlineValues[online]));

            return plan;
        }

        public static bool GameVisibilityAllowed(PrivacyLevel join, PrivacyLevel? online) => online is null || join >= online;

        public static async Task<bool> SetOnlineVisibilityAsync(PrivacyLevel online, PrivacyLevel? join)
        {
            const string LOG_IDENT = "PrivacySettings::SetOnlineVisibilityAsync";

            var plan = PlanOnlineVisibility(online, join);

            App.Logger.WriteLine(LOG_IDENT, $"Setting online visibility to {online} with joining at {join?.ToString() ?? "unreported"}: {String.Join(", then ", plan.Select(x => $"{x.Setting}={x.Value}"))}");

            foreach ((string setting, string value) in plan)
                await PostAsync(setting, value);

            return plan.Count > 1;
        }

        public static async Task SetGameVisibilityAsync(PrivacyLevel join, PrivacyLevel? online)
        {
            const string LOG_IDENT = "PrivacySettings::SetGameVisibilityAsync";

            if (!GameVisibilityAllowed(join, online))
                throw new ArgumentException($"Game visibility {join} is wider than online visibility {online}", nameof(join));

            App.Logger.WriteLine(LOG_IDENT, $"Setting game visibility to {join} with online at {online?.ToString() ?? "unreported"}");

            await PostAsync(JoinSetting, JoinValues[join]);
        }

        private static Task PostAsync(string setting, string value) =>
            AccountRequests.SendAsync(HttpMethod.Post, UpdateUrl, new Dictionary<string, string> { [setting] = value }, setting, value);
    }
}
