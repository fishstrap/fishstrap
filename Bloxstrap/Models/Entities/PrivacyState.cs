namespace Bloxstrap.Models.Entities
{
    public class PrivacyState
    {
        public PrivacyLevel? Online { get; init; }

        public PrivacyLevel? Join { get; init; }

        public IReadOnlyList<PrivacyLevel> OnlineOptions { get; init; } = Array.Empty<PrivacyLevel>();

        public IReadOnlyList<PrivacyLevel> JoinOptions { get; init; } = Array.Empty<PrivacyLevel>();
    }
}
