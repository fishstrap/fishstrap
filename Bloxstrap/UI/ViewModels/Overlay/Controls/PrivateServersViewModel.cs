using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using Bloxstrap.Integrations;
using Bloxstrap.RobloxInterfaces;

namespace Bloxstrap.UI.ViewModels.Overlay.Controls
{
    public class PrivateServersViewModel : NotifyPropertyChangedViewModel
    {
        private readonly ActivityWatcher? _activityWatcher;

        private readonly Dictionary<long, PrivateServerDetails> _ownDetails = new();

        private readonly FlashMessage _flash;

        private bool _visible;

        private long _loadedPlaceId;

        private bool _failed;

        private int _generation;

        private string? _cursor;

        private long? _managingId;

        private PrivateServerDetails? _details;

        public ObservableCollection<PrivateServerItem> Servers { get; } = new();

        public ObservableCollection<PrivateServerUser> AllowedUsers { get; } = new();

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                _isBusy = value;

                OnPropertyChanged(nameof(IsBusy));
                Refreshed();
            }
        }

        private bool _isLoadingMore;

        public bool IsLoadingMore
        {
            get => _isLoadingMore;
            private set
            {
                _isLoadingMore = value;

                OnPropertyChanged(nameof(IsLoadingMore));
                OnPropertyChanged(nameof(HasMore));
            }
        }

        public bool HasMore => !String.IsNullOrEmpty(_cursor) && !_isLoadingMore && !IsManaging;

        private bool _isSaving;

        public bool IsSaving
        {
            get => _isSaving;
            private set
            {
                _isSaving = value;

                OnPropertyChanged(nameof(IsSaving));
                OnPropertyChanged(nameof(CanEdit));
            }
        }

        public bool CanEdit => _details is not null && !_isSaving;

        public bool IsManaging => _managingId is not null;

        public Visibility ListVisibility => IsManaging ? Visibility.Collapsed : Visibility.Visible;

        public Visibility ManageVisibility => IsManaging ? Visibility.Visible : Visibility.Collapsed;

        public bool ShowEmptyState => !IsBusy && !IsManaging && Servers.Count == 0;

        public string EmptyText
        {
            get
            {
                if (!InGame)
                    return Strings.Menu_Overlay_Servers_NotInGame;

                if (!SignedIn)
                    return Strings.Menu_Overlay_PrivateServers_NeedsCookies;

                return _failed ? Strings.Menu_Overlay_PrivateServers_LoadFailed : Strings.Menu_Overlay_PrivateServers_None;
            }
        }

        public bool ShowCreate => InGame && SignedIn && !IsBusy;

        private bool _createAllowed = true;

        public bool CreateAllowed
        {
            get => _createAllowed;
            private set
            {
                _createAllowed = value;

                OnPropertyChanged(nameof(CreateAllowed));
                OnPropertyChanged(nameof(CreateStatusText));
            }
        }

        public string CreateStatusText => _createAllowed ? Strings.Menu_Overlay_PrivateServers_CreateHint : Strings.Menu_Overlay_PrivateServers_CreateUnavailable;

        public string StatusText => _flash.Text ?? String.Empty;

        public bool HasStatus => _flash.Text is not null;

        private string _serverName = String.Empty;

        public string ServerName
        {
            get => _serverName;
            private set
            {
                _serverName = value;
                OnPropertyChanged(nameof(ServerName));
            }
        }

        private string _gameName = String.Empty;

        public string GameName
        {
            get => _gameName;
            private set
            {
                _gameName = value;
                OnPropertyChanged(nameof(GameName));
            }
        }

        private string? _gameIcon;

        public string? GameIcon
        {
            get => _gameIcon;
            private set
            {
                _gameIcon = value;
                OnPropertyChanged(nameof(GameIcon));
            }
        }

        private string _editName = String.Empty;

        public string EditName
        {
            get => _editName;
            set
            {
                _editName = value;
                OnPropertyChanged(nameof(EditName));
            }
        }

        private bool _isEditingName;

        public bool IsEditingName
        {
            get => _isEditingName;
            private set
            {
                _isEditingName = value;

                OnPropertyChanged(nameof(IsEditingName));
                OnPropertyChanged(nameof(IsShowingName));
            }
        }

        public bool IsShowingName => !_isEditingName;

        private bool _isAddingPeople;

        public bool IsAddingPeople
        {
            get => _isAddingPeople;
            private set
            {
                _isAddingPeople = value;
                OnPropertyChanged(nameof(IsAddingPeople));
            }
        }

        public bool IsActive
        {
            get => _details?.Active == true;
            set
            {
                if (_details is null || _details.Active == value || _isSaving)
                    return;

                _ = ChangeAsync(details => PrivateServers.SetActiveAsync(details, value),
                    value ? Strings.Menu_Overlay_PrivateServers_TurnedOn : Strings.Menu_Overlay_PrivateServers_TurnedOff);
            }
        }

        public bool FriendsAllowed
        {
            get => _details?.Permissions?.FriendsAllowed == true;
            set
            {
                if (_details is null || FriendsAllowed == value || _isSaving)
                    return;

                _ = ChangeAsync(details => PrivateServers.SetFriendsAllowedAsync(details, value), Strings.Menu_Overlay_PrivateServers_Saved);
            }
        }

        public string Link => _details?.Link ?? String.Empty;

        public bool HasLink => !String.IsNullOrEmpty(_details?.Link);

        public string PriceText => _details?.Subscription?.Price is long price and > 0
            ? String.Format(Strings.Menu_Overlay_PrivateServers_PriceRobux, price.ToString("N0"))
            : Strings.Menu_Overlay_PrivateServers_Free;

        public string RenewalText
        {
            get
            {
                PrivateServerSubscription? subscription = _details?.Subscription;

                if (subscription is null)
                    return String.Empty;

                if (subscription.Expired)
                    return Strings.Menu_Overlay_PrivateServers_Expired;

                if (subscription.Price is not > 0 || subscription.ExpirationDate is not DateTime date)
                    return String.Empty;

                string when = date.ToLocalTime().ToString("d");

                return String.Format(subscription.Active ? Strings.Menu_Overlay_PrivateServers_Renews : Strings.Menu_Overlay_PrivateServers_Ends, when);
            }
        }

        public bool HasRenewal => !String.IsNullOrEmpty(RenewalText);

        public bool HasNoAllowedUsers => AllowedUsers.Count == 0;

        private string _addUsername = String.Empty;

        public string AddUsername
        {
            get => _addUsername;
            set
            {
                _addUsername = value;
                OnPropertyChanged(nameof(AddUsername));
            }
        }

        public ICommand RefreshCommand => new AsyncRelayCommand(LoadAsync);

        public ICommand LoadMoreCommand => new AsyncRelayCommand(LoadMoreAsync);

        public ICommand CreateCommand => new RelayCommand(Create);

        public ICommand JoinCommand => new RelayCommand<PrivateServerItem>(Join);

        public ICommand ConfigureCommand => new AsyncRelayCommand<PrivateServerItem>(ConfigureAsync);

        public ICommand ToggleRowActiveCommand => new AsyncRelayCommand<PrivateServerItem>(item =>
            RowChangeAsync(item, details => PrivateServers.SetActiveAsync(details, !details.Active), details =>
                details.Active ? Strings.Menu_Overlay_PrivateServers_TurnedOff : Strings.Menu_Overlay_PrivateServers_TurnedOn));

        public ICommand GenerateRowLinkCommand => new AsyncRelayCommand<PrivateServerItem>(item =>
            RowChangeAsync(item, PrivateServers.RegenerateLinkAsync, _ => Strings.Menu_Overlay_PrivateServers_NewLinkMade));

        public ICommand CopyRowLinkCommand => new RelayCommand<PrivateServerItem>(item => CopyToClipboard(item?.Link));

        public ICommand BackCommand => new RelayCommand(Back);

        public ICommand EditNameCommand => new RelayCommand(() =>
        {
            EditName = _details?.Name ?? ServerName;
            IsEditingName = true;
        });

        public ICommand SaveNameCommand => new AsyncRelayCommand(SaveNameAsync);

        public ICommand CancelNameCommand => new RelayCommand(() => IsEditingName = false);

        public ICommand AddPeopleCommand => new RelayCommand(() =>
        {
            AddUsername = String.Empty;
            IsAddingPeople = !IsAddingPeople;
        });

        public ICommand AddUserCommand => new AsyncRelayCommand(AddUserAsync);

        public ICommand RemoveUserCommand => new AsyncRelayCommand<PrivateServerUser>(RemoveUserAsync);

        public ICommand RegenerateLinkCommand => new AsyncRelayCommand(() => ChangeAsync(PrivateServers.RegenerateLinkAsync, Strings.Menu_Overlay_PrivateServers_NewLinkMade));

        public ICommand CopyLinkCommand => new RelayCommand(() => CopyToClipboard(Link));

        private bool InGame => _activityWatcher?.InGame == true && _activityWatcher.Data.PlaceId != 0;

        private long PlaceId => _activityWatcher?.Data.PlaceId ?? 0;

        private static bool SignedIn => App.Cookies.Loaded;

        public PrivateServersViewModel(ActivityWatcher? activityWatcher)
        {
            _activityWatcher = activityWatcher;

            _flash = new FlashMessage(TimeSpan.FromSeconds(4), () =>
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(HasStatus));
            });

            if (_activityWatcher is null)
                return;

            _activityWatcher.OnGameJoin += (_, _) => App.Current.Dispatcher.Invoke(OnGameJoin);
            _activityWatcher.OnGameLeave += (_, _) => App.Current.Dispatcher.Invoke(OnGameLeave);
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;

            if (visible && !IsManaging && InGame && _loadedPlaceId != PlaceId)
                _ = LoadAsync();
        }

        private void OnGameJoin()
        {
            _loadedPlaceId = 0;

            StopManaging();

            if (_visible)
                _ = LoadAsync();
            else
                Refreshed();
        }

        private void OnGameLeave()
        {
            _generation++;
            _loadedPlaceId = 0;
            _cursor = null;

            StopManaging();

            Servers.Clear();
            _ownDetails.Clear();

            IsBusy = false;
            OnPropertyChanged(nameof(HasMore));
        }

        public async Task LoadAsync()
        {
            const string LOG_IDENT = "PrivateServersViewModel::LoadAsync";

            int generation = ++_generation;

            Servers.Clear();
            _ownDetails.Clear();
            _cursor = null;
            _failed = false;

            OnPropertyChanged(nameof(HasMore));

            if (!InGame || !SignedIn)
            {
                Refreshed();
                return;
            }

            long placeId = PlaceId;

            IsBusy = true;

            try
            {
                await LoadPageAsync(generation, placeId, null, true);

                _loadedPlaceId = placeId;
            }
            catch (Exception ex)
            {
                if (generation != _generation)
                    return;

                App.Logger.WriteLine(LOG_IDENT, $"Failed to load the private servers of {placeId}");
                App.Logger.WriteException(LOG_IDENT, ex);

                _failed = true;
            }
            finally
            {
                if (generation == _generation)
                    IsBusy = false;
            }

            if (generation == _generation)
                await LoadCreateAvailabilityAsync();
        }

        private async Task LoadMoreAsync()
        {
            const string LOG_IDENT = "PrivateServersViewModel::LoadMoreAsync";

            if (String.IsNullOrEmpty(_cursor) || _isLoadingMore || !InGame)
                return;

            int generation = _generation;

            IsLoadingMore = true;

            try
            {
                await LoadPageAsync(generation, PlaceId, _cursor, false);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load more private servers");
                App.Logger.WriteException(LOG_IDENT, ex);

                Flash(Strings.Menu_Overlay_PrivateServers_LoadFailed);
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        private async Task LoadPageAsync(int generation, long placeId, string? cursor, bool first)
        {
            ApiPageResponse<PrivateServerEntry> page = await PrivateServers.ListPageAsync(placeId, cursor);

            if (generation != _generation)
                return;

            long me = App.Cookies.CurrentUser?.Id ?? 0;

            string currentCode = _activityWatcher?.Data.ServerType == ServerType.Private
                ? _activityWatcher.Data.AccessCode
                : String.Empty;

            IEnumerable<PrivateServerItem> items = page.Data
                .Where(entry => Servers.All(existing => existing.VipServerId != entry.VipServerId))
                .Select(entry => ToItem(entry, me, currentCode));

            if (first)
                items = items.OrderByDescending(item => item.IsCurrent).ThenByDescending(item => item.IsMine);

            List<PrivateServerItem> added = items.ToList();

            foreach (PrivateServerItem item in added)
                Servers.Add(item);

            _cursor = page.NextPageCursor;

            OnPropertyChanged(nameof(HasMore));
            Refreshed();

            _ = LoadAvatarsAsync(added);

            foreach (PrivateServerItem item in added.Where(item => item.IsMine))
                _ = RefreshOwnAsync(item);
        }

        private static PrivateServerItem ToItem(PrivateServerEntry entry, long me, string currentCode) => new()
        {
            VipServerId = entry.VipServerId,
            AccessCode = entry.AccessCode ?? String.Empty,
            Name = String.IsNullOrWhiteSpace(entry.Name) ? Strings.Menu_Overlay_PrivateServers_Untitled : entry.Name,
            OwnerId = entry.Owner?.Id ?? 0,
            CapacityText = String.Format(Strings.Menu_Overlay_PrivateServers_Capacity, entry.Playing ?? 0, entry.MaxPlayers),
            IsMine = me != 0 && entry.Owner?.Id == me,
            IsCurrent = !String.IsNullOrEmpty(currentCode) && currentCode == entry.AccessCode
        };

        private async Task LoadCreateAvailabilityAsync()
        {
            const string LOG_IDENT = "PrivateServersViewModel::LoadCreateAvailabilityAsync";

            try
            {
                UniverseDetails? universe = await CurrentUniverseAsync();

                if (universe is not null)
                    CreateAllowed = universe.Data.CreateVipServersAllowed;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to check whether private servers can be made");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            OnPropertyChanged(nameof(ShowCreate));
        }

        private async Task<UniverseDetails?> CurrentUniverseAsync()
        {
            ActivityData? activity = _activityWatcher?.Data;

            if (activity is null || activity.UniverseId == 0)
                return null;

            return await activity.EnsureUniverseDetailsAsync();
        }

        private static async Task LoadAvatarsAsync(IReadOnlyList<PrivateServerItem> items)
        {
            const string LOG_IDENT = "PrivateServersViewModel::LoadAvatarsAsync";

            List<long> owners = items.Select(item => item.OwnerId).Where(id => id != 0).Distinct().ToList();

            if (owners.Count == 0)
                return;

            try
            {
                Dictionary<long, UserDetails> details = await UserDetails.FetchBatch(owners);

                foreach (PrivateServerItem item in items)
                {
                    if (details.TryGetValue(item.OwnerId, out UserDetails? owner))
                        item.OwnerAvatar = owner.Thumbnail.ImageUrl;
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load owner avatars");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private async Task RefreshOwnAsync(PrivateServerItem item)
        {
            const string LOG_IDENT = "PrivateServersViewModel::RefreshOwnAsync";

            try
            {
                PrivateServerDetails details = await PrivateServers.DetailsAsync(item.VipServerId);

                _ownDetails[item.VipServerId] = details;

                item.IsActive = details.Active;
                item.Link = details.Link ?? String.Empty;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load a private server's settings");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private async Task RowChangeAsync(PrivateServerItem? item, Func<PrivateServerDetails, Task> change, Func<PrivateServerDetails, string> success)
        {
            const string LOG_IDENT = "PrivateServersViewModel::RowChangeAsync";

            if (item is null || !item.IsMine || item.IsWorking)
                return;

            item.IsWorking = true;

            try
            {
                if (!_ownDetails.TryGetValue(item.VipServerId, out PrivateServerDetails? details))
                    details = await PrivateServers.DetailsAsync(item.VipServerId);

                string message = success(details);

                await change(details);

                Flash(message);
            }
            catch (SettingRejectedException ex) when (!String.IsNullOrWhiteSpace(ex.Reason))
            {
                App.Logger.WriteException(LOG_IDENT, ex);

                Flash(String.Format(Strings.Menu_Overlay_PrivateServers_Rejected, ex.Reason));
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to change the private server");
                App.Logger.WriteException(LOG_IDENT, ex);

                Flash(Strings.Menu_Overlay_PrivateServers_SaveFailed);
            }

            await RefreshOwnAsync(item);

            item.IsWorking = false;
        }

        private void Create()
        {
            const string LOG_IDENT = "PrivateServersViewModel::Create";

            if (!InGame || !CreateAllowed)
                return;

            try
            {
                Utilities.ShellExecute(PrivateServers.ServersPageUrl(PlaceId));

                Flash(Strings.Menu_Overlay_PrivateServers_CreateOpened);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to open the experience page");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private void Join(PrivateServerItem? item)
        {
            const string LOG_IDENT = "PrivateServersViewModel::Join";

            if (item is null || !item.CanJoin)
                return;

            try
            {
                PrivateServers.Join(PlaceId, item.AccessCode);

                Flash(String.Format(Strings.Menu_Overlay_Games_Joining, item.Name));
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to join a private server");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private async Task ConfigureAsync(PrivateServerItem? item)
        {
            const string LOG_IDENT = "PrivateServersViewModel::ConfigureAsync";

            if (item is null || !item.IsMine)
                return;

            _managingId = item.VipServerId;
            _details = null;

            ServerName = item.Name;
            IsEditingName = false;
            IsAddingPeople = false;
            AddUsername = String.Empty;
            AllowedUsers.Clear();

            NotifyManage();

            IsBusy = true;

            try
            {
                UniverseDetails? universe = await CurrentUniverseAsync();

                GameName = universe?.Data.Name ?? String.Empty;
                GameIcon = universe?.Thumbnail?.ImageUrl;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load the experience");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            try
            {
                await ReloadDetailsAsync();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load the private server's settings");
                App.Logger.WriteException(LOG_IDENT, ex);

                StopManaging();

                Flash(Strings.Menu_Overlay_PrivateServers_LoadFailed);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ReloadDetailsAsync()
        {
            if (_managingId is not long id)
                return;

            PrivateServerDetails details = await PrivateServers.DetailsAsync(id);

            if (_managingId != id)
                return;

            _details = details;
            _ownDetails[id] = details;

            ServerName = String.IsNullOrWhiteSpace(details.Name) ? Strings.Menu_Overlay_PrivateServers_Untitled : details.Name;

            if (!String.IsNullOrWhiteSpace(details.Game?.Name))
                GameName = details.Game.Name;

            AllowedUsers.Clear();

            foreach (PrivateServerUser user in details.Permissions?.Users ?? new List<PrivateServerUser>())
                AllowedUsers.Add(user);

            NotifyManage();
        }

        private async Task<bool> ChangeAsync(Func<PrivateServerDetails, Task> change, string success)
        {
            const string LOG_IDENT = "PrivateServersViewModel::ChangeAsync";

            if (_details is null || _isSaving)
                return false;

            bool changed = false;

            IsSaving = true;

            try
            {
                await change(_details);

                changed = true;

                Flash(success);
            }
            catch (SettingRejectedException ex) when (!String.IsNullOrWhiteSpace(ex.Reason))
            {
                App.Logger.WriteException(LOG_IDENT, ex);

                Flash(String.Format(Strings.Menu_Overlay_PrivateServers_Rejected, ex.Reason));
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to change the private server");
                App.Logger.WriteException(LOG_IDENT, ex);

                Flash(Strings.Menu_Overlay_PrivateServers_SaveFailed);
            }

            try
            {
                await ReloadDetailsAsync();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to reload the private server's settings");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            IsSaving = false;

            NotifyManage();

            return changed;
        }

        private async Task SaveNameAsync()
        {
            string name = EditName.Trim();

            if (name.Length == 0 || name == _details?.Name)
            {
                IsEditingName = false;
                return;
            }

            if (await ChangeAsync(details => PrivateServers.RenameAsync(details, name), Strings.Menu_Overlay_PrivateServers_Saved))
                IsEditingName = false;
        }

        private void CopyToClipboard(string? link)
        {
            const string LOG_IDENT = "PrivateServersViewModel::CopyToClipboard";

            if (String.IsNullOrEmpty(link))
                return;

            try
            {
                Clipboard.SetDataObject(link);

                Flash(Strings.Menu_Overlay_PrivateServers_LinkCopied);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to copy the link");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private async Task AddUserAsync()
        {
            const string LOG_IDENT = "PrivateServersViewModel::AddUserAsync";

            string username = AddUsername.Trim().TrimStart('@');

            if (username.Length == 0 || _details is null || _isSaving)
                return;

            PrivateServerUser? user;

            IsSaving = true;

            try
            {
                user = await PrivateServers.FindUserAsync(username);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to look the user up");
                App.Logger.WriteException(LOG_IDENT, ex);

                Flash(Strings.Menu_Overlay_PrivateServers_SaveFailed);
                return;
            }
            finally
            {
                IsSaving = false;
            }

            if (user is null)
            {
                Flash(String.Format(Strings.Menu_Overlay_PrivateServers_NoSuchUser, username));
                return;
            }

            string name = user.Name ?? username;

            if (AllowedUsers.Any(existing => existing.Id == user.Id))
            {
                Flash(String.Format(Strings.Menu_Overlay_PrivateServers_AlreadyAdded, name));
                return;
            }

            if (await ChangeAsync(details => PrivateServers.AddUserAsync(details, user), String.Format(Strings.Menu_Overlay_PrivateServers_Added, name)))
            {
                AddUsername = String.Empty;
                IsAddingPeople = false;
            }
        }

        private async Task RemoveUserAsync(PrivateServerUser? user)
        {
            if (user is null)
                return;

            await ChangeAsync(details => PrivateServers.RemoveUserAsync(details, user),
                String.Format(Strings.Menu_Overlay_PrivateServers_Removed, user.Name ?? user.Id.ToString()));
        }

        private void Back()
        {
            StopManaging();

            _ = LoadAsync();
        }

        private void StopManaging()
        {
            _managingId = null;
            _details = null;

            IsEditingName = false;
            IsAddingPeople = false;
            AllowedUsers.Clear();

            NotifyManage();
        }

        private void NotifyManage()
        {
            OnPropertyChanged(nameof(IsManaging));
            OnPropertyChanged(nameof(ListVisibility));
            OnPropertyChanged(nameof(ManageVisibility));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(IsActive));
            OnPropertyChanged(nameof(FriendsAllowed));
            OnPropertyChanged(nameof(Link));
            OnPropertyChanged(nameof(HasLink));
            OnPropertyChanged(nameof(PriceText));
            OnPropertyChanged(nameof(RenewalText));
            OnPropertyChanged(nameof(HasRenewal));
            OnPropertyChanged(nameof(HasNoAllowedUsers));
            OnPropertyChanged(nameof(HasMore));

            Refreshed();
        }

        private void Flash(string message) => _flash.Show(message);

        private void Refreshed()
        {
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(EmptyText));
            OnPropertyChanged(nameof(ShowCreate));
        }
    }
}
