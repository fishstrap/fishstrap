namespace Bloxstrap.Models
{
    public class GameJoinData
    {
        public GameJoinType JoinType = GameJoinType.Unknown;

        public int? PlaceId { get; set; }
        public string? JobId { get; set; }
        public int? UserId { get; set; }
        public string? AccessCode { get; set; }
    }
}