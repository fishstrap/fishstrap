using Bloxstrap.Enums.Overlay;
using Bloxstrap.Models.APIs.RealtimeMessaging;
using Bloxstrap.RobloxInterfaces;

namespace Bloxstrap.Integrations.OverlayModules
{
    public class FriendPresence : IDisposable
    {
        private static readonly TimeSpan ResyncInterval = TimeSpan.FromMinutes(10);

        private static readonly TimeSpan OfflineInterval = TimeSpan.FromSeconds(45);

        private static readonly TimeSpan FriendsRefresh = TimeSpan.FromMinutes(10);

        private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(15);

        private const int BatchSize = 50;

        private const int MaxPerUpdate = 3;

        public event EventHandler<FriendPresenceChange>? Changed;

        public event EventHandler? Updated;

        private readonly ActivityWatcher? _activityWatcher;

        private readonly RealtimeMessaging _messaging;

        private readonly object _lock = new();

        private readonly Dictionary<(long UserId, string Place), DateTime> _notified = new();

        private Dictionary<long, UserPresence> _last = new();

        private List<long> _friends = new();

        private HashSet<long> _friendIds = new();

        private DateTime _friendsFetched = DateTime.MinValue;

        private CancellationTokenSource? _cancellation;

        private bool _seeded;

        public IReadOnlyList<long> FriendIds => _friends.ToList();

        public IReadOnlyDictionary<long, UserPresence> Presences
        {
            get
            {
                lock (_lock)
                    return new Dictionary<long, UserPresence>(_last);
            }
        }

        public bool HasLooked => _seeded;

        public FriendPresence(ActivityWatcher? activityWatcher, RealtimeMessaging messaging)
        {
            _activityWatcher = activityWatcher;
            _messaging = messaging;
        }

        public void Start()
        {
            if (_cancellation is not null)
                return;

            _messaging.PresenceChanged += OnPresenceChanged;

            _cancellation = new CancellationTokenSource();

            CancellationToken token = _cancellation.Token;

            _ = Task.Run(() => RunAsync(token));
        }

        public void Stop()
        {
            _messaging.PresenceChanged -= OnPresenceChanged;

            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = null;

            _seeded = false;
        }

        private async Task RunAsync(CancellationToken token)
        {
            const string LOG_IDENT = "FriendPresence::RunAsync";

            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await PollAsync();
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine(LOG_IDENT, "Failed to check on friends");
                        App.Logger.WriteException(LOG_IDENT, ex);
                    }

                    await Task.Delay(_messaging.IsConnected ? ResyncInterval : OfflineInterval, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public async Task PollAsync()
        {
            const string LOG_IDENT = "FriendPresence::PollAsync";

            if (!await App.Cookies.EnsureLoadedAsync() || App.Cookies.CurrentUser is not AuthenticatedUser me)
                return;

            if (DateTime.UtcNow - _friendsFetched > FriendsRefresh)
            {
                var friends = await Http.GetJson<ApiArrayResponse<FriendResponse>>(
                    UrlBuilder.BuildApiUrl("friends", $"v1/users/{me.Id}/friends"));

                _friends = friends?.Data?.Select(x => x.Id).Where(x => x > 0).Distinct().ToList() ?? new List<long>();
                _friendIds = _friends.ToHashSet();
                _friendsFetched = DateTime.UtcNow;

                App.Logger.WriteLine(LOG_IDENT, $"Keeping an eye on {_friends.Count} friends");
            }

            if (!_friends.Any())
            {
                _seeded = true;
                Updated?.Invoke(this, EventArgs.Empty);
                return;
            }

            var now = new Dictionary<long, UserPresence>();

            foreach (long[] batch in _friends.Chunk(BatchSize))
            {
                var response = await AccountRequests.PostJsonAsync<UserPresencesResponse>(
                    UrlBuilder.BuildApiUrl("presence", "v1/presence/users"), new { userIds = batch });

                foreach (UserPresence presence in response?.UserPresences ?? new List<UserPresence>())
                    now[presence.UserId] = presence;
            }

            List<FriendPresenceChange> changes;

            lock (_lock)
            {
                changes = _seeded ? Compare(now) : new List<FriendPresenceChange>();

                _last = now;
                _seeded = true;
            }

            Publish(changes);
        }

        private void OnPresenceChanged(object? sender, IReadOnlyList<PresenceNotification> notifications)
        {
            List<FriendPresenceChange> changes;

            lock (_lock)
            {
                if (!_seeded)
                    return;

                var now = new Dictionary<long, UserPresence>(_last);
                bool changed = false;

                foreach (PresenceNotification notification in notifications)
                {
                    if (notification.Type != PresenceNotification.PresenceChangedType || notification.PresenceReport is not UserPresence presence)
                        continue;

                    long userId = presence.UserId > 0 ? presence.UserId : notification.UserId;

                    if (!_friendIds.Contains(userId))
                        continue;

                    presence.UserId = userId;
                    now[userId] = presence;
                    changed = true;
                }

                if (!changed)
                    return;

                changes = Compare(now);
                _last = now;
            }

            Publish(changes);
        }

        private void Publish(List<FriendPresenceChange> changes)
        {
            Updated?.Invoke(this, EventArgs.Empty);

            foreach (FriendPresenceChange change in changes.OrderBy(x => x.Kind).Take(MaxPerUpdate))
                Changed?.Invoke(this, change);
        }

        private List<FriendPresenceChange> Compare(Dictionary<long, UserPresence> now)
        {
            var changes = new List<FriendPresenceChange>();

            bool inGame = _activityWatcher?.InGame == true;
            string myServer = inGame ? _activityWatcher!.Data.JobId : String.Empty;
            long myUniverse = inGame ? _activityWatcher!.Data.UniverseId : 0;

            foreach ((long userId, UserPresence presence) in now)
            {
                if (!presence.IsInGame || String.IsNullOrWhiteSpace(presence.LastLocation))
                    continue;

                _last.TryGetValue(userId, out UserPresence? before);

                if (!String.IsNullOrEmpty(myServer)
                    && String.Equals(presence.GameId, myServer, StringComparison.OrdinalIgnoreCase)
                    && !String.Equals(before?.GameId, myServer, StringComparison.OrdinalIgnoreCase))
                {
                    Add(userId, FriendPresenceKind.JoinedYourServer, presence, myServer);
                    continue;
                }

                if (before?.IsInGame == true && before.UniverseId == presence.UniverseId)
                    continue;

                FriendPresenceKind kind = myUniverse != 0 && presence.UniverseId == myUniverse
                    ? FriendPresenceKind.PlayingYourGame
                    : FriendPresenceKind.StartedPlaying;

                Add(userId, kind, presence, presence.UniverseId.ToString()!);
            }

            return changes;

            void Add(long userId, FriendPresenceKind kind, UserPresence presence, string place)
            {
                if (_notified.TryGetValue((userId, place), out DateTime at) && DateTime.UtcNow - at < Cooldown)
                    return;

                _notified[(userId, place)] = DateTime.UtcNow;

                changes.Add(new FriendPresenceChange(userId, kind, presence.LastLocation!,
                    presence.PlaceId ?? 0, presence.RootPlaceId ?? 0, String.IsNullOrEmpty(presence.GameId) ? null : presence.GameId));
            }
        }

        public void Dispose()
        {
            Stop();

            GC.SuppressFinalize(this);
        }
    }
}
