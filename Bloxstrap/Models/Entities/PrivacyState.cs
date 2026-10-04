namespace Bloxstrap.Models.Entities
{
    public class PrivacyState
    {
        public string? Online { get; init; }

        public string? Join { get; init; }

        public IReadOnlyList<string> OnlineOptions { get; init; } = Array.Empty<string>();

        public IReadOnlyList<string> JoinOptions { get; init; } = Array.Empty<string>();
    }
}
