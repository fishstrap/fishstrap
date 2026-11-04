namespace Bloxstrap.Models.APIs.Roblox
{
    public class UserSettingsResponse
    {
        [JsonPropertyName("whoCanSeeMyOnlineStatus")]
        public UserSettingValue? OnlineStatus { get; set; }

        [JsonPropertyName("whoCanJoinMeInExperiences")]
        public UserSettingValue? JoinStatus { get; set; }
    }

    public class UserSettingValue
    {
        [JsonPropertyName("currentValue")]
        public string? CurrentValue { get; set; }

        [JsonPropertyName("options")]
        public List<UserSettingOption>? Options { get; set; }

        public IReadOnlyList<string> Available => Options?
            .Select(x => x.Option?.OptionValue)
            .OfType<string>()
            .ToList() ?? new List<string>();
    }

    public class UserSettingOption
    {
        [JsonPropertyName("option")]
        public UserSettingOptionValue? Option { get; set; }
    }

    public class UserSettingOptionValue
    {
        [JsonPropertyName("optionValue")]
        public string? OptionValue { get; set; }
    }
}
