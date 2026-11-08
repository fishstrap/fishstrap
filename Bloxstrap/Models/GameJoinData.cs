namespace Bloxstrap.Models
{
    public class GameJoinData
    {
        public GameJoinType JoinType = GameJoinType.Unknown;

        public long? PlaceId { get; set; }
        public string? JobId { get; set; }
        public long? UserId { get; set; }
        public string? JoinOrigin;
        public string? AccessCode { get; set; }
        public string? LinkCode { get; set; }

        public PrivateServerData? PrivateServer => JoinType == GameJoinType.RequestPrivateGame && PlaceId is long placeId && !string.IsNullOrEmpty(AccessCode) ? new(placeId, AccessCode, LinkCode) : null;

        public string PlaceLauncherUrl { get; set; } = string.Empty;
    }
}