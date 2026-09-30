namespace Bloxstrap.Integrations.OverlayModules
{
    public static class RecentServerLog
    {
        private const int Capacity = 100;

        private static readonly object _lock = new();

        public static void Joined(ActivityData data)
        {
            if (data.ServerType != ServerType.Public || data.PlaceId == 0 || String.IsNullOrEmpty(data.JobId))
                return;

            Change(servers =>
            {
                servers.RemoveAll(x => x.JobId.Equals(data.JobId, StringComparison.OrdinalIgnoreCase));

                servers.Insert(0, new RecentServer
                {
                    PlaceId = data.PlaceId,
                    UniverseId = data.UniverseId,
                    JobId = data.JobId,
                    JoinedAt = DateTime.UtcNow
                });

                if (servers.Count > Capacity)
                    servers.RemoveRange(Capacity, servers.Count - Capacity);
            });
        }

        public static void Left(ActivityData data)
        {
            if (String.IsNullOrEmpty(data.JobId))
                return;

            Change(servers =>
            {
                RecentServer? server = servers.FirstOrDefault(x => x.JobId.Equals(data.JobId, StringComparison.OrdinalIgnoreCase));

                if (server is not null)
                    server.LeftAt = DateTime.UtcNow;
            });
        }

        public static List<RecentServer> For(long placeId)
        {
            lock (_lock)
            {
                Refresh();

                return App.RecentServers.Prop.Servers
                    .Where(x => x.PlaceId == placeId)
                    .OrderByDescending(x => x.JoinedAt)
                    .ToList();
            }
        }

        private static void Change(Action<List<RecentServer>> change)
        {
            const string LOG_IDENT = "RecentServerLog::Change";

            lock (_lock)
            {
                try
                {
                    Refresh();

                    change(App.RecentServers.Prop.Servers);

                    App.RecentServers.Save();
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Failed to remember a server");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }
            }
        }

        private static void Refresh()
        {
            bool onDisk = File.Exists(App.RecentServers.FileLocation);

            if (!App.RecentServers.Loaded ? onDisk : onDisk && App.RecentServers.HasFileOnDiskChanged())
                App.RecentServers.Load(false);
        }
    }
}
