using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using Bloxstrap.Integrations;
using Bloxstrap.Integrations.OverlayModules;
using Bloxstrap.RobloxInterfaces;

namespace Bloxstrap.UI.ViewModels.Overlay.Controls
{
    public class RecentServersViewModel : NotifyPropertyChangedViewModel
    {
        private const int Shown = 12;

        private readonly ActivityWatcher? _activityWatcher;

        private readonly ServerBrowserViewModel _publicServers;

        private HashSet<string> _stillRunning = new(StringComparer.OrdinalIgnoreCase);

        private bool _lookedUp;

        private int _generation;

        public ObservableCollection<RecentServerItem> Items { get; } = new();

        public bool ShowEmptyState => !Items.Any();

        public string EmptyText => InGame ? Strings.Menu_Overlay_Servers_Recent_Empty : Strings.Menu_Overlay_Servers_NotInGame;

        private bool InGame => _activityWatcher?.InGame == true && _activityWatcher.Data.PlaceId != 0;

        public ICommand JoinCommand => new RelayCommand<RecentServerItem>(Join);

        public RecentServersViewModel(ActivityWatcher? activityWatcher, ServerBrowserViewModel publicServers)
        {
            _activityWatcher = activityWatcher;
            _publicServers = publicServers;

            _publicServers.Servers.CollectionChanged += (_, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                    UpdateStatuses();
            };
            _publicServers.PropertyChanged += OnPublicServersChanged;

            if (_activityWatcher is null)
                return;

            _activityWatcher.OnGameJoin += (_, _) =>
            {
                RecentServerLog.Joined(_activityWatcher.Data);

                App.Current.Dispatcher.InvokeAsync(async () => await ReloadAsync());
            };

            _activityWatcher.OnGameLeave += (_, _) =>
            {
                if (_activityWatcher.History.FirstOrDefault() is ActivityData left)
                    RecentServerLog.Left(left);

                App.Current.Dispatcher.InvokeAsync(async () => await ReloadAsync());
            };
        }

        private void OnPublicServersChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ServerBrowserViewModel.IsBusy))
                UpdateStatuses();
        }

        public async Task ReloadAsync()
        {
            const string LOG_IDENT = "RecentServersViewModel::ReloadAsync";

            int generation = ++_generation;

            Items.Clear();

            _stillRunning = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _lookedUp = false;

            if (InGame)
            {
                long placeId = _activityWatcher!.Data.PlaceId;
                string current = _activityWatcher.Data.JobId;

                var recent = RecentServerLog.For(placeId)
                    .Where(x => !x.JobId.Equals(current, StringComparison.OrdinalIgnoreCase))
                    .Take(Shown);

                foreach (RecentServer server in recent)
                    Items.Add(new RecentServerItem { PlaceId = server.PlaceId, JobId = server.JobId, JoinedAt = server.JoinedAt, LeftAt = server.LeftAt });
            }

            Refreshed();
            UpdateStatuses();

            if (!Items.Any())
                return;

            try
            {
                var running = await GameServers.StillRunningAsync(Items[0].PlaceId, Items.Select(x => x.JobId));

                if (generation != _generation)
                    return;

                _stillRunning = running;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to check which recent servers are still up");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            if (generation != _generation)
                return;

            _lookedUp = true;

            UpdateStatuses();
        }

        private void UpdateStatuses()
        {
            var listed = new Dictionary<string, GameServer>(StringComparer.OrdinalIgnoreCase);

            foreach (GameServer server in _publicServers.Servers)
                listed.TryAdd(server.JobId, server);

            foreach (RecentServerItem item in Items)
            {
                if (listed.TryGetValue(item.JobId, out GameServer? server))
                    item.SetStatus(RecentServerItem.ServerStatus.Running, server.Playing, server.MaxPlayers);
                else if (_stillRunning.Contains(item.JobId))
                    item.SetStatus(RecentServerItem.ServerStatus.Running);
                else if (_lookedUp && !_publicServers.IsBusy)
                    item.SetStatus(RecentServerItem.ServerStatus.Missing);
                else
                    item.SetStatus(RecentServerItem.ServerStatus.Checking);
            }
        }

        private void Join(RecentServerItem? item)
        {
            const string LOG_IDENT = "RecentServersViewModel::Join";

            if (item is null || !item.CanJoin)
                return;

            try
            {
                GameServers.Join(item.PlaceId, item.JobId);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to join {item.JobId}");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private void Refreshed()
        {
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(EmptyText));
        }
    }
}
