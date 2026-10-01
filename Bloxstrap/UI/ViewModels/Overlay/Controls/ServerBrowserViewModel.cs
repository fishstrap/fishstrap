using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using CommunityToolkit.Mvvm.Input;

using Bloxstrap.Enums.Overlay;
using Bloxstrap.Integrations;
using Bloxstrap.RobloxInterfaces;

namespace Bloxstrap.UI.ViewModels.Overlay.Controls
{
    public class ServerBrowserViewModel : NotifyPropertyChangedViewModel
    {
        private readonly ActivityWatcher? _activityWatcher;

        public ObservableCollection<GameServer> Servers { get; } = new();

        public IEnumerable<Order> Orders { get; } = Enum.GetValues<Order>();

        public ObservableCollection<ServerRegion> Regions { get; } = new();

        private ServerRegion? _selectedRegion;

        public ServerRegion? SelectedRegion
        {
            get => _selectedRegion;
            set
            {
                if (_selectedRegion == value)
                    return;

                _selectedRegion = value;

                OnPropertyChanged(nameof(SelectedRegion));

                _ = LoadAsync();
            }
        }

        private Order _selectedOrder = Order.Descending;

        public Order SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                if (_selectedOrder == value)
                    return;

                _selectedOrder = value;

                OnPropertyChanged(nameof(SelectedOrder));

                _ = LoadAsync();
            }
        }

        private bool _excludeFull;

        public bool ExcludeFull
        {
            get => _excludeFull;
            set
            {
                if (_excludeFull == value)
                    return;

                _excludeFull = value;

                OnPropertyChanged(nameof(ExcludeFull));

                _ = LoadAsync();
            }
        }

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                _isBusy = value;

                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(CanRefresh));
                OnPropertyChanged(nameof(CanHop));
            }
        }

        public bool CanRefresh => !IsBusy;

        public bool ShowEmptyState => !IsBusy && !Servers.Any();

        public string EmptyText
        {
            get
            {
                if (!InGame)
                    return Strings.Menu_Overlay_Servers_NotInGame;

                if (SelectedRegion is not null && !SelectedRegion.IsAll)
                    return String.Format(Strings.Menu_Overlay_Servers_EmptyRegion, SelectedRegion.Name);

                return Strings.Menu_Overlay_Servers_Empty;
            }
        }

        private readonly DispatcherTimer _uptimeTimer = new() { Interval = TimeSpan.FromSeconds(1) };

        private readonly FlashMessage _flash;

        public string SummaryText
        {
            get
            {
                if (_flash.Text is not null)
                    return _flash.Text;

                return Servers.Any()
                    ? String.Format(Strings.Menu_Overlay_Servers_Summary, Servers.Count)
                    : String.Empty;
            }
        }

        private bool InGame => _activityWatcher?.InGame == true && _activityWatcher.Data.PlaceId != 0;

        public ICommand RefreshCommand => new RelayCommand(async () => await LoadAsync());

        public ICommand JoinCommand => new RelayCommand<GameServer>(Join);

        public ICommand CopyLinkCommand => new RelayCommand<GameServer>(CopyLink);

        #region This server

        private readonly DispatcherTimer? _currentServerTimer;

        private string? _currentLocation;

        public bool HasCurrentServer => InGame;

        public string CurrentServerType => _activityWatcher?.Data.ServerType.ToTranslatedString() ?? String.Empty;

        public string CurrentJobId => _activityWatcher?.Data.JobId ?? String.Empty;

        public string CurrentUptime => _activityWatcher?.Data.StartTime is DateTime started
            ? Time.FormatTimeSpan(DateTime.UtcNow - started)
            : Strings.Common_Loading;

        public string CurrentLocation => _currentLocation ?? Strings.Common_Loading;

        public bool ShowCurrentLocation => App.Settings.Prop.ShowServerDetails;

        public ICommand CopyInstanceIdCommand => new RelayCommand(CopyInstanceId);

        private bool _isHopping;

        public bool CanHop => InGame && !_isHopping && !IsBusy;

        public ICommand HopCommand => new RelayCommand(async () => await HopAsync());

        private bool _findingClosest;

        public bool CanJoinClosest => InGame && !_findingClosest;

        public ICommand ClosestCommand => new RelayCommand(async () => await JoinClosestAsync());

        private async void RefreshCurrentServer()
        {
            foreach (string name in new[] { nameof(HasCurrentServer), nameof(CurrentServerType), nameof(CurrentJobId), nameof(CurrentUptime), nameof(CanHop), nameof(CanJoinClosest) })
                OnPropertyChanged(name);

            if (!InGame || !ShowCurrentLocation || _currentLocation is not null)
                return;

            string? location = await _activityWatcher!.Data.QueryServerLocation();

            if (String.IsNullOrEmpty(location))
                return;

            _currentLocation = location;

            OnPropertyChanged(nameof(CurrentLocation));
        }

        private void CopyInstanceId()
        {
            const string LOG_IDENT = "ServerBrowserViewModel::CopyInstanceId";

            try
            {
                Clipboard.SetDataObject(CurrentJobId);

                Flash(Strings.Menu_Overlay_Servers_IdCopied);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to copy the instance id");
                App.Logger.WriteException(LOG_IDENT, ex);

                Flash(Strings.Menu_Overlay_Servers_CopyFailed);
            }
        }

        private async Task HopAsync()
        {
            const string LOG_IDENT = "ServerBrowserViewModel::HopAsync";

            if (!CanHop)
                return;

            _isHopping = true;

            OnPropertyChanged(nameof(CanHop));

            try
            {
                if (!Servers.Any())
                    await LoadAsync();

                GameServer? target = GameServers.PickHopTarget(Servers);

                if (target is null)
                {
                    Flash(Strings.Menu_Overlay_Servers_NoHopTarget);
                    return;
                }

                App.Logger.WriteLine(LOG_IDENT, $"Hopping to {target.JobId}");

                GameServers.Join(_activityWatcher!.Data.PlaceId, target.JobId);

                Flash(Strings.Menu_Overlay_Servers_Hopping);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to hop servers");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
            finally
            {
                _isHopping = false;

                OnPropertyChanged(nameof(CanHop));
            }
        }

        private async Task JoinClosestAsync()
        {
            const string LOG_IDENT = "ServerBrowserViewModel::JoinClosestAsync";

            if (!CanJoinClosest)
                return;

            if (!App.Settings.Prop.AllowCookieAccess)
            {
                Flash(Strings.Menu_Overlay_Servers_ClosestNeedsCookies);
                return;
            }

            _findingClosest = true;

            OnPropertyChanged(nameof(CanJoinClosest));

            try
            {
                if (!await App.Cookies.EnsureLoadedAsync())
                {
                    Flash(Strings.Menu_Overlay_Servers_ClosestNeedsCookies);
                    return;
                }

                long placeId = _activityWatcher!.Data.PlaceId;

                var (jobId, alreadyClosest) = await GameServers.FindClosestAsync(placeId, _activityWatcher.Data.JobId);

                if (alreadyClosest)
                {
                    Flash(Strings.Menu_Overlay_Servers_ClosestAlready);
                    return;
                }

                if (jobId is null)
                {
                    Flash(Strings.Menu_Overlay_Servers_ClosestNone);
                    return;
                }

                App.Logger.WriteLine(LOG_IDENT, $"Joining the closest server, {jobId}");

                GameServers.Join(placeId, jobId);

                Flash(Strings.Menu_Overlay_Servers_ClosestJoining);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to find the closest server");
                App.Logger.WriteException(LOG_IDENT, ex);

                Flash(Strings.Menu_Overlay_Servers_ClosestFailed);
            }
            finally
            {
                _findingClosest = false;

                OnPropertyChanged(nameof(CanJoinClosest));
            }
        }

        #endregion

        public ServerBrowserViewModel(ActivityWatcher? activityWatcher)
        {
            _activityWatcher = activityWatcher;

            _flash = new FlashMessage(TimeSpan.FromSeconds(2), () => OnPropertyChanged(nameof(SummaryText)));

            _uptimeTimer.Tick += (_, _) =>
            {
                foreach (GameServer server in Servers)
                    server.Tick();
            };

            if (_activityWatcher is null)
                return;

            _currentServerTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            _currentServerTimer.Tick += (_, _) => RefreshCurrentServer();
            _currentServerTimer.Start();

            _activityWatcher.OnGameJoin += async (_, _) => await App.Current.Dispatcher.InvokeAsync(async () =>
            {
                _currentLocation = null;

                await LoadAsync();
            });

            _activityWatcher.OnGameLeave += (_, _) => App.Current.Dispatcher.Invoke(Clear);
        }

        public void SetVisible(bool visible)
        {
            if (visible)
                _uptimeTimer.Start();
            else
                _uptimeTimer.Stop();
        }

        public async Task InitialiseAsync()
        {
            if (!Regions.Any())
            {
                foreach (ServerRegion region in await GameServers.FetchRegionsAsync())
                    Regions.Add(region);

                _selectedRegion = Regions.FirstOrDefault();

                OnPropertyChanged(nameof(SelectedRegion));
            }

            await LoadAsync();
        }

        public async Task LoadAsync()
        {
            const string LOG_IDENT = "ServerBrowserViewModel::LoadAsync";

            if (IsBusy)
                return;

            if (!InGame)
            {
                Clear();
                return;
            }

            IsBusy = true;

            try
            {
                var servers = await GameServers.FetchAsync(
                    _activityWatcher!.Data.PlaceId,
                    _activityWatcher.Data.JobId,
                    SelectedOrder,
                    ExcludeFull,
                    SelectedRegion);

                if (servers.FirstOrDefault(x => x.IsCurrent) is GameServer current && _activityWatcher.Data.StartTime is DateTime started)
                {
                    current.StartedAt = started;
                    current.UptimeIsEstimate = false;
                }

                await Task.WhenAll(
                    GameServers.PopulateIconsAsync(servers),
                    GameServers.PopulateDetailsAsync(_activityWatcher.Data.PlaceId, servers));

                Servers.Clear();

                foreach (GameServer server in servers)
                    Servers.Add(server);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load servers");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
            finally
            {
                IsBusy = false;

                Refreshed();

                RefreshCurrentServer();
            }
        }

        private void Join(GameServer? server)
        {
            const string LOG_IDENT = "ServerBrowserViewModel::Join";

            if (server is null || server.IsCurrent || _activityWatcher is null)
                return;

            try
            {
                GameServers.Join(_activityWatcher.Data.PlaceId, server.JobId);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to join {server.JobId}");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private void CopyLink(GameServer? server)
        {
            const string LOG_IDENT = "ServerBrowserViewModel::CopyLink";

            if (server is null || _activityWatcher is null)
                return;

            try
            {
                string link = $"{App.RemoteData.Prop.DeeplinkUrl}?placeId={_activityWatcher.Data.PlaceId}&gameInstanceId={server.JobId}";

                Clipboard.SetDataObject(link);

                Flash(Strings.Menu_Overlay_Servers_LinkCopied);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to copy a link to {server.JobId}");
                App.Logger.WriteException(LOG_IDENT, ex);

                Flash(Strings.Menu_Overlay_Servers_CopyFailed);
            }
        }

        private void Flash(string message) => _flash.Show(message);

        private void Clear()
        {
            Servers.Clear();

            Refreshed();

            RefreshCurrentServer();
        }

        private void Refreshed()
        {
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(EmptyText));
            OnPropertyChanged(nameof(SummaryText));
        }
    }
}
