namespace Bloxstrap.Models.Persistable
{
    public class RecentServers
    {
        public List<RecentServer> Servers { get; set; } = new();
    }

    public class RecentServer
    {
        public long PlaceId { get; set; }

        public long UniverseId { get; set; }

        public string JobId { get; set; } = String.Empty;

        public DateTime JoinedAt { get; set; }

        public DateTime? LeftAt { get; set; }
    }
}
