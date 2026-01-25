using Wpf.Ui.Controls;

namespace Bloxstrap.Models.APIs.Config
{
    public class RemoteDataBase
    {
        // alert
        [JsonPropertyName("alertEnabled")]
        public bool AlertEnabled { get; set; } = false!;

        [JsonPropertyName("alertContent")]
        public string AlertContent { get; set; } = null!;

        [JsonPropertyName("alertSeverity")]
        public InfoBarSeverity AlertSeverity { get; set; } = InfoBarSeverity.Warning;

        // flags
        [JsonPropertyName("killFlags")]
        public bool KillFlags { get; set; } = false;

        [JsonPropertyName("deeplinkUrl")]
        public string DeeplinkUrl { get; set; } = "https://fishstrap.app/joingame";

        // package maps
        [JsonPropertyName("packageMaps")]
        public PackageMaps PackageMaps { get; set; } = new();


        // packages that will be skipped during upgrades
        [JsonPropertyName("ignoredPackages")]
        public List<string> IgnoredPackages { get; set; } = new()
        {
            "RobloxPlayerInstaller.exe"
        };
    }
}