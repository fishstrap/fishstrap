using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

using CommunityToolkit.Mvvm.Input;

using Bloxstrap.Enums.Overlay;
using Bloxstrap.Integrations;
using Bloxstrap.UI.Elements.Overlay;

namespace Bloxstrap.UI.ViewModels.Overlay
{
    public class GameOverlayViewModel : NotifyPropertyChangedViewModel
    {
        private readonly GameOverlay _window;
        private readonly Integrations.Overlay? _overlay;
        private readonly ActivityWatcher? _activityWatcher;

        private readonly DispatcherTimer _sessionTimer;

        #region Account

        private string _profileIcon = String.Empty;

        public string ProfileIcon
        {
            get => _profileIcon;
            set => Set(ref _profileIcon, value, nameof(ProfileIcon));
        }

        private string _displayName = String.Empty;

        public string DisplayName
        {
            get => _displayName;
            set => Set(ref _displayName, value, nameof(DisplayName));
        }

        public Controls.OnlineStatusViewModel OnlineStatus { get; } = new();

        private string _username = String.Empty;

        public string Username
        {
            get => _username;
            set => Set(ref _username, value, nameof(Username));
        }

        #endregion

        #region Current experience

        private string _gameIcon = String.Empty;

        public string GameIcon
        {
            get => _gameIcon;
            set => Set(ref _gameIcon, value, nameof(GameIcon));
        }

        private string _game = Strings.Menu_Overlay_NotInGame;

        public string Game
        {
            get => _game;
            set => Set(ref _game, value, nameof(Game));
        }

        private string _timePlayed = String.Empty;

        public string TimePlayed
        {
            get => _timePlayed;
            set => Set(ref _timePlayed, value, nameof(TimePlayed));
        }

        private string _serverUptime = String.Empty;

        public string ServerUptime
        {
            get => _serverUptime;
            set
            {
                Set(ref _serverUptime, value, nameof(ServerUptime));
                OnPropertyChanged(nameof(ServerUptimeVisibility));
            }
        }

        public Visibility ServerUptimeVisibility => String.IsNullOrEmpty(_serverUptime) ? Visibility.Collapsed : Visibility.Visible;

        private Visibility _gameVisibility = Visibility.Collapsed;

        public Visibility GameVisibility
        {
            get => _gameVisibility;
            set => Set(ref _gameVisibility, value, nameof(GameVisibility));
        }

        #endregion

        #region Panels

        private readonly HashSet<OverlayPanelKind> _open = new();

        private readonly HashSet<OverlayPanelKind> _pinned = new();

        private bool _showingPinnedOnly;

        public event EventHandler<OverlayPanelKind>? PanelOpened;

        public bool MessagesOpen => _open.Contains(OverlayPanelKind.Messages);

        public bool BadgesOpen => _open.Contains(OverlayPanelKind.Badges);

        public bool ServersOpen => _open.Contains(OverlayPanelKind.Servers);

        public bool GamesOpen => _open.Contains(OverlayPanelKind.Games);

        public bool HistoryOpen => _open.Contains(OverlayPanelKind.History);

        public bool NotesOpen => _open.Contains(OverlayPanelKind.Notes);

        public bool BrowserOpen => _open.Contains(OverlayPanelKind.Browser);

        public bool SettingsOpen => _open.Contains(OverlayPanelKind.Settings);

        public bool MessagesPinned => _pinned.Contains(OverlayPanelKind.Messages);

        public bool BadgesPinned => _pinned.Contains(OverlayPanelKind.Badges);

        public bool ServersPinned => _pinned.Contains(OverlayPanelKind.Servers);

        public bool GamesPinned => _pinned.Contains(OverlayPanelKind.Games);

        public bool HistoryPinned => _pinned.Contains(OverlayPanelKind.History);

        public bool NotesPinned => _pinned.Contains(OverlayPanelKind.Notes);

        public bool BrowserPinned => _pinned.Contains(OverlayPanelKind.Browser);

        public bool SettingsPinned => _pinned.Contains(OverlayPanelKind.Settings);

        public bool HasPinnedPanels => _open.Any(_pinned.Contains);

        public bool ShowingPinnedOnly
        {
            get => _showingPinnedOnly;
            set
            {
                if (_showingPinnedOnly == value)
                    return;

                _showingPinnedOnly = value;

                OnPropertyChanged(nameof(ShowingPinnedOnly));
                OnPropertyChanged(nameof(DockVisibility));
                NotifyPanels();
            }
        }

        public Visibility DockVisibility => _showingPinnedOnly ? Visibility.Collapsed : Visibility.Visible;

        public bool IsOpen(OverlayPanelKind panel) => _open.Contains(panel);

        public bool IsPinned(OverlayPanelKind panel) => _pinned.Contains(panel);

        public Visibility MessagesVisibility => Visible(OverlayPanelKind.Messages);

        public Visibility BadgesVisibility => Visible(OverlayPanelKind.Badges);

        public Visibility ServersVisibility => Visible(OverlayPanelKind.Servers);

        public Visibility GamesVisibility => Visible(OverlayPanelKind.Games);

        public Visibility HistoryVisibility => Visible(OverlayPanelKind.History);

        public Visibility NotesVisibility => Visible(OverlayPanelKind.Notes);

        public Visibility BrowserVisibility => Visible(OverlayPanelKind.Browser);

        public Visibility SettingsVisibility => Visible(OverlayPanelKind.Settings);

        public ICommand TogglePanelCommand => new RelayCommand<string>(TogglePanel);

        public ICommand ClosePanelCommand => new RelayCommand<string>(ClosePanel);

        public ICommand TogglePinCommand => new RelayCommand<string>(TogglePin);

        private void TogglePanel(string? name)
        {
            if (!Enum.TryParse(name, out OverlayPanelKind panel))
                return;

            if (_open.Add(panel))
                PanelOpened?.Invoke(this, panel);
            else
                _open.Remove(panel);

            NotifyPanels();
        }

        private void ClosePanel(string? name)
        {
            if (!Enum.TryParse(name, out OverlayPanelKind panel))
                return;

            _open.Remove(panel);

            NotifyPanels();
        }

        private void TogglePin(string? name)
        {
            if (!Enum.TryParse(name, out OverlayPanelKind panel))
                return;

            if (!_pinned.Add(panel))
                _pinned.Remove(panel);

            NotifyPanels();
        }

        private void NotifyPanels()
        {
            foreach (string name in Enum.GetNames<OverlayPanelKind>())
            {
                OnPropertyChanged($"{name}Open");
                OnPropertyChanged($"{name}Pinned");
                OnPropertyChanged($"{name}Visibility");
            }

            OnPropertyChanged(nameof(HasPinnedPanels));
        }

        private Visibility Visible(OverlayPanelKind panel) =>
            _open.Contains(panel) && (!_showingPinnedOnly || _pinned.Contains(panel)) ? Visibility.Visible : Visibility.Collapsed;

        #endregion

        public ICommand CloseCommand => new RelayCommand(_window.Dismiss);

        #region Sharing

        private Wpf.Ui.Common.SymbolRegular _shareIcon = Wpf.Ui.Common.SymbolRegular.Link24;

        public Wpf.Ui.Common.SymbolRegular ShareIcon
        {
            get => _shareIcon;
            set => Set(ref _shareIcon, value, nameof(ShareIcon));
        }

        private DispatcherTimer? _shareTimer;

        public ICommand ShareCommand => new RelayCommand(Share);

        private void Share()
        {
            const string LOG_IDENT = "GameOverlayViewModel::Share";

            if (_activityWatcher?.InGame != true)
                return;

            try
            {
                Clipboard.SetDataObject(_activityWatcher.Data.GetInviteDeeplink());

                ShareIcon = Wpf.Ui.Common.SymbolRegular.Checkmark24;

                _shareTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                _shareTimer.Tick -= ResetShareIcon;
                _shareTimer.Tick += ResetShareIcon;

                _shareTimer.Stop();
                _shareTimer.Start();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to copy the invite link");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private void ResetShareIcon(object? sender, EventArgs e)
        {
            _shareTimer?.Stop();

            ShareIcon = Wpf.Ui.Common.SymbolRegular.Link24;
        }

        #endregion


        private CornerRadius _scrimCornerRadius = new(0, 0, 8, 8);

        public CornerRadius ScrimCornerRadius
        {
            get => _scrimCornerRadius;
            set => Set(ref _scrimCornerRadius, value, nameof(ScrimCornerRadius));
        }

        public GameOverlayViewModel(GameOverlay window, Integrations.Overlay? overlay)
        {
            _window = window;
            _overlay = overlay;
            _activityWatcher = overlay?.ActivityWatcher;

            foreach ((string name, OverlayPanelLayout panel) in App.OverlayLayout.Prop.Panels)
            {
                if (!Enum.TryParse(name, out OverlayPanelKind kind))
                    continue;

                if (panel.Open)
                    _open.Add(kind);

                if (panel.Pinned)
                    _pinned.Add(kind);
            }

            _sessionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _sessionTimer.Tick += (_, _) => UpdateSession();

            if (_overlay is not null)
            {
                _overlay.BoundsChanged += OnBoundsChanged;
                _overlay.GameVisibilityChanged += OnGameVisibilityChanged;
                _overlay.WindowClosed += OnWindowClosed;
            }

            if (_activityWatcher is null)
                return;

            _activityWatcher.OnGameJoin += (_, _) => App.Current.Dispatcher.Invoke(OnGameJoin);
            _activityWatcher.OnGameLeave += (_, _) => App.Current.Dispatcher.Invoke(OnGameLeave);
        }

        public async Task OnLoaded()
        {
            const string LOG_IDENT = "GameOverlayViewModel::OnLoaded";

            if (_activityWatcher?.InGame == true)
                OnGameJoin();

            if (!App.Settings.Prop.AllowCookieAccess)
                return;

            try
            {
                if (!App.Cookies.Loaded)
                    await Task.Run(App.Cookies.LoadCookies);

                AuthenticatedUser? current = App.Cookies.CurrentUser;

                if (current is null)
                    return;

                UserDetails details = await UserDetails.Fetch(current.Id);

                DisplayName = details.Data.DisplayName;
                Username = $"@{details.Data.Name}";
                ProfileIcon = details.Thumbnail.ImageUrl ?? String.Empty;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load the signed-in user");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private async void OnGameJoin()
        {
            const string LOG_IDENT = "GameOverlayViewModel::OnGameJoin";

            GameVisibility = Visibility.Visible;

            UpdateSession();

            _sessionTimer.Start();

            ActivityData? activity = _activityWatcher?.Data;

            if (activity is null)
                return;

            try
            {
                if (activity.UniverseDetails is null)
                {
                    await UniverseDetails.FetchSingle(activity.UniverseId);

                    activity.UniverseDetails = UniverseDetails.LoadFromCache(activity.UniverseId);
                }

                Game = activity.UniverseDetails?.Data.Name ?? String.Empty;
                GameIcon = activity.UniverseDetails?.Thumbnail.ImageUrl ?? String.Empty;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load the current experience");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private void OnGameLeave()
        {
            _sessionTimer.Stop();

            Game = Strings.Menu_Overlay_NotInGame;
            GameIcon = String.Empty;
            TimePlayed = String.Empty;
            ServerUptime = String.Empty;
            GameVisibility = Visibility.Collapsed;
        }

        private void UpdateSession()
        {
            DateTime? started = _activityWatcher?.Data.StartTime;

            ServerUptime = started is null ? String.Empty : FormatClock(DateTime.UtcNow - started.Value);

            DateTime? joined = _activityWatcher?.Data.TimeJoined;

            if (joined is null || joined == default(DateTime))
            {
                TimePlayed = String.Empty;
                return;
            }

            TimePlayed = FormatClock(DateTime.Now - joined.Value);
        }

        private static string FormatClock(TimeSpan elapsed)
        {
            if (elapsed < TimeSpan.Zero)
                elapsed = TimeSpan.Zero;

            if (elapsed.TotalDays >= 1)
                return $"{(int)elapsed.TotalDays}d {elapsed.ToString(@"hh\:mm\:ss")}";

            return elapsed.ToString(elapsed.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss");
        }

        private OverlayBounds? _pendingBounds;
        private bool _boundsQueued;

        private void OnBoundsChanged(object? sender, OverlayBounds bounds)
        {
            if (bounds.Rect.Width <= 0 || bounds.Rect.Height <= 0)
                return;

            _pendingBounds = bounds;

            if (_boundsQueued)
                return;

            _boundsQueued = true;

            _window.Dispatcher.InvokeAsync(() =>
            {
                _boundsQueued = false;

                if (_pendingBounds is not null)
                    ApplyBounds(_pendingBounds);
            }, DispatcherPriority.Render);
        }

        private void ApplyBounds(OverlayBounds bounds)
        {
            double radius = bounds.IsMaximised ? 0 : 8;

            ScrimCornerRadius = new CornerRadius(0, 0, radius, radius);

            Rect rect = bounds.Rect;

            Point topLeft = new(rect.Left, rect.Top);
            Point bottomRight = new(rect.Right, rect.Bottom);

            if (PresentationSource.FromVisual(_window)?.CompositionTarget is CompositionTarget target)
            {
                topLeft = target.TransformFromDevice.Transform(topLeft);
                bottomRight = target.TransformFromDevice.Transform(bottomRight);
            }

            _window.Left = topLeft.X;
            _window.Top = topLeft.Y;
            _window.Width = Math.Max(bottomRight.X - topLeft.X, 0);
            _window.Height = Math.Max(bottomRight.Y - topLeft.Y, 0);
        }

        private void OnGameVisibilityChanged(object? sender, bool visible)
        {
            if (!visible)
                _window.HideForGame();
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            _sessionTimer.Stop();

            _window.Close();
        }

        private void Set<T>(ref T field, T value, string name)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;

            OnPropertyChanged(name);
        }
    }
}
