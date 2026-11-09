using System.Windows;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

using Bloxstrap.Enums.Overlay;
using Bloxstrap.Integrations.OverlayModules;
using Bloxstrap.RobloxInterfaces;
using Bloxstrap.UI.Elements.Overlay;

namespace Bloxstrap.Integrations
{
    public class Overlay : IDisposable
    {
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;
        private const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;
        private const uint EVENT_OBJECT_DESTROY = 0x8001;
        private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const int OBJID_WINDOW = 0;
        private const int CHILDID_SELF = 0;

        private const int MaxQueuedNotices = 4;

        private static readonly TimeSpan HintDelay = TimeSpan.FromSeconds(4);

        public event EventHandler<OverlayBounds>? BoundsChanged;
        public event EventHandler<bool>? GameVisibilityChanged;
        public event EventHandler? WindowClosed;

        public readonly RealtimeMessaging Messaging = new();

        public readonly ActivityWatcher? ActivityWatcher;

        public readonly FriendPresence Friends;

        private readonly HWND _robloxWindow;
        private readonly uint _robloxProcessId;

        private WINEVENTPROC? _systemCallback;
        private WINEVENTPROC? _objectCallback;
        private WINEVENTPROC? _foregroundCallback;

        private UnhookWinEventSafeHandle? _systemHook;
        private UnhookWinEventSafeHandle? _objectHook;
        private UnhookWinEventSafeHandle? _foregroundHook;

        private GameOverlay? _window;
        private OverlayToast? _toast;
        private OverlayBounds? _lastBounds;
        private bool _disposed;
        private bool _hinted;
        private bool _friendsForPanel;

        private readonly Queue<OverlayNotice> _notices = new();

        public Overlay(long windowHandle, long robloxProcessId, ActivityWatcher? activityWatcher)
        {
            _robloxWindow = (HWND)(IntPtr)windowHandle;
            _robloxProcessId = (uint)robloxProcessId;

            ActivityWatcher = activityWatcher;

            Friends = new FriendPresence(activityWatcher, Messaging);
            Friends.Changed += OnFriendChanged;
        }

        public void Start()
        {
            const string LOG_IDENT = "Overlay::Start";

            if (_robloxWindow == IntPtr.Zero)
            {
                App.Logger.WriteLine(LOG_IDENT, "No window handle, not starting");
                return;
            }

            App.Logger.WriteLine(LOG_IDENT, $"Attaching to window {(IntPtr)_robloxWindow}");

            Application.Current.Dispatcher.Invoke(() =>
            {
                _window = new GameOverlay(this);
                _window.PrepareHidden();

                _systemCallback = new WINEVENTPROC(OnSystemEvent);
                _objectCallback = new WINEVENTPROC(OnObjectEvent);

                _systemHook = PInvoke.SetWinEventHook(
                    EVENT_SYSTEM_MINIMIZESTART, EVENT_SYSTEM_MINIMIZEEND,
                    null, _systemCallback, _robloxProcessId, 0, WINEVENT_OUTOFCONTEXT);

                _objectHook = PInvoke.SetWinEventHook(
                    EVENT_OBJECT_DESTROY, EVENT_OBJECT_LOCATIONCHANGE,
                    null, _objectCallback, _robloxProcessId, 0, WINEVENT_OUTOFCONTEXT);

                _foregroundCallback = new WINEVENTPROC(OnForegroundEvent);

                _foregroundHook = PInvoke.SetWinEventHook(
                    EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
                    null, _foregroundCallback, 0, 0, WINEVENT_OUTOFCONTEXT);
            });

            SyncBounds();

            Application.Current.Dispatcher.InvokeAsync(() => _window?.Reanchor(), System.Windows.Threading.DispatcherPriority.Background);

            Messaging.ConnectToUserhub();

            if (ActivityWatcher is not null)
                ActivityWatcher.OnGameJoin += OnGameJoin;

            UpdateFriendWatch();
        }

        public void SyncBounds()
        {
            OverlayBounds bounds = GetBounds();

            _lastBounds = bounds;

            Application.Current.Dispatcher.Invoke(() => BoundsChanged?.Invoke(this, bounds));
        }

        public void AnchorAboveGame(IntPtr overlayHandle)
        {
            if (overlayHandle == IntPtr.Zero || _robloxWindow == IntPtr.Zero)
                return;

            PInvoke.SetWindowPos(
                (HWND)overlayHandle,
                HWND.Null,
                0, 0, 0, 0,
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE |
                SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
                SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
        }

        public bool ShowToast(string title, string message)
        {
            const string LOG_IDENT = "Overlay::ShowToast";

            if (_disposed || _window is null || _robloxWindow == IntPtr.Zero)
            {
                App.Logger.WriteLine(LOG_IDENT, "No overlay to show it in");
                return false;
            }

            if (!App.Settings.Prop.OverlayServerToasts)
            {
                App.Logger.WriteLine(LOG_IDENT, "Server details are set to use desktop notifications");
                return false;
            }

            if (IsGameMinimised() || !IsGameForeground())
            {
                App.Logger.WriteLine(LOG_IDENT, "Game isn't in front, leaving it to the desktop notification");
                return false;
            }

            App.Logger.WriteLine(LOG_IDENT, $"{title}: {message.Replace("\n", "\\n")}");

            Notify(new OverlayNotice(title, message, Kind: NoticeKind.Server));

            return true;
        }

        public void Notify(OverlayNotice notice)
        {
            if (_disposed)
                return;

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                while (_notices.Count >= MaxQueuedNotices)
                    _notices.Dequeue();

                _notices.Enqueue(notice);

                if (_toast?.IsShowing != true)
                    ShowNextNotice();
            });
        }

        private void ShowNextNotice()
        {
            const string LOG_IDENT = "Overlay::ShowNextNotice";

            while (_notices.Count > 0)
            {
                OverlayNotice notice = _notices.Dequeue();

                if (_disposed || _window is null || _robloxWindow == IntPtr.Zero)
                {
                    _notices.Clear();
                    return;
                }

                if (IsGameMinimised() || !IsGameForeground())
                {
                    App.Logger.WriteLine(LOG_IDENT, "Game isn't in front, dropping a notification");
                    continue;
                }

                OverlayBounds bounds = GetBounds();

                if (bounds.Rect.Width <= 0 || bounds.Rect.Height <= 0)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Could not measure the game window");
                    continue;
                }

                try
                {
                    Toast().Present(notice, bounds.Rect);

                    return;
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Failed to show a notification");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }
            }
        }

        public void Preview(OverlayNotice notice)
        {
            const string LOG_IDENT = "Overlay::Preview";

            if (_disposed || _window is null || _robloxWindow == IntPtr.Zero)
                return;

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                OverlayBounds bounds = GetBounds();

                if (bounds.Rect.Width <= 0 || bounds.Rect.Height <= 0)
                    return;

                try
                {
                    _notices.Clear();

                    Toast().Present(notice, bounds.Rect);
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Failed to show a test notification");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }
            });
        }

        public void WatchFriendsForPanel()
        {
            _friendsForPanel = true;

            UpdateFriendWatch();
        }

        public void UpdateFriendWatch()
        {
            if (App.Settings.Prop.OverlayFriendNotifications || _friendsForPanel)
                Friends.Start();
            else
                Friends.Stop();
        }

        private OverlayToast Toast()
        {
            if (_toast is null)
            {
                _toast = new OverlayToast();
                _toast.Finished += (_, _) => ShowNextNotice();
            }

            return _toast;
        }

        public void Open() => Application.Current.Dispatcher.Invoke(() => _window?.Open());

        public void OpenPage(Uri address) => Application.Current.Dispatcher.Invoke(() => _window?.OpenPage(address));

        public void DismissToast()
        {
            _notices.Clear();
            _toast?.Dismiss();
        }

        private async void OnGameJoin(object? sender, EventArgs e)
        {
            if (_hinted || !App.Settings.Prop.OverlayStartHint)
                return;

            _hinted = true;

            await Task.Delay(HintDelay);

            string? hint = Application.Current.Dispatcher.Invoke(() => _window?.HotkeyHint);

            if (!String.IsNullOrEmpty(hint))
                Notify(new OverlayNotice(Strings.Menu_Overlay_Notify_HintTitle, hint, Kind: NoticeKind.Hint));
        }

        private async void OnFriendChanged(object? sender, FriendPresenceChange change)
        {
            const string LOG_IDENT = "Overlay::OnFriendChanged";

            if (!App.Settings.Prop.OverlayFriendNotifications)
                return;

            try
            {
                var users = await UserDetails.FetchBatch(new List<long> { change.UserId });

                if (!users.TryGetValue(change.UserId, out UserDetails? friend))
                    return;

                string name = String.IsNullOrEmpty(friend.Data.DisplayName) ? friend.Data.Name : friend.Data.DisplayName;

                string title = change.Kind switch
                {
                    FriendPresenceKind.JoinedYourServer => Strings.Menu_Overlay_Notify_FriendJoinedServer,
                    FriendPresenceKind.PlayingYourGame => Strings.Menu_Overlay_Notify_FriendSameGame,
                    _ => Strings.Menu_Overlay_Notify_FriendPlaying
                };

                App.Logger.WriteLine(LOG_IDENT, $"Notifying about a friend ({change.Kind})");

                (string? actionText, Action? action) = FriendAction(change);

                Notify(new OverlayNotice(String.Format(title, name), change.GameName, friend.Thumbnail?.ImageUrl, true, NoticeKind.Friend, actionText, action));
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to look up a friend");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private (string? Text, Action? Action) FriendAction(FriendPresenceChange change)
        {
            const string LOG_IDENT = "Overlay::FriendAction";

            if (change.Kind == FriendPresenceKind.JoinedYourServer)
                return (null, null);

            if (change.PlaceId > 0 && change.ServerId is string server && server.Length > 0)
            {
                return (Strings.Menu_Overlay_Notify_ClickToJoin, () =>
                {
                    App.Logger.WriteLine(LOG_IDENT, "Joining a friend from their notification");
                    GameServers.Join(change.PlaceId, server);
                });
            }

            long page = change.RootPlaceId > 0 ? change.RootPlaceId : change.PlaceId;

            if (page <= 0)
                return (null, null);

            return (Strings.Menu_Overlay_Notify_ClickToView, () =>
            {
                App.Logger.WriteLine(LOG_IDENT, "Opening a friend's game page from their notification");
                _window?.OpenPage(GameServers.GamePage(page));
            });
        }

        public bool IsGameMinimised() => PInvoke.IsIconic(_robloxWindow);

        public bool IsGameForeground() => PInvoke.GetForegroundWindow() == _robloxWindow;

        public void FocusGame() => PInvoke.SetForegroundWindow(_robloxWindow);

        private OverlayBounds GetBounds()
        {
            if (!PInvoke.GetClientRect(_robloxWindow, out RECT client))
                return new OverlayBounds(Rect.Empty, false);

            var origin = new System.Drawing.Point(client.left, client.top);

            if (!PInvoke.ClientToScreen(_robloxWindow, ref origin))
                return new OverlayBounds(Rect.Empty, false);

            var bounds = new Rect(
                origin.X,
                origin.Y,
                Math.Max(client.right - client.left, 0),
                Math.Max(client.bottom - client.top, 0));

            return new OverlayBounds(bounds, PInvoke.IsZoomed(_robloxWindow));
        }

        private void OnSystemEvent(HWINEVENTHOOK hook, uint iEvent, HWND hWnd, int idObject, int idChild, uint thread, uint time)
        {
            if (hWnd != _robloxWindow)
                return;

            switch (iEvent)
            {
                case EVENT_SYSTEM_MINIMIZESTART:
                    Application.Current.Dispatcher.Invoke(() => GameVisibilityChanged?.Invoke(this, false));
                    break;

                case EVENT_SYSTEM_MINIMIZEEND:
                    Application.Current.Dispatcher.Invoke(() => GameVisibilityChanged?.Invoke(this, true));
                    SyncBounds();
                    break;
            }
        }

        private void OnForegroundEvent(HWINEVENTHOOK hook, uint iEvent, HWND hWnd, int idObject, int idChild, uint thread, uint time) =>
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (hWnd == _robloxWindow)
                    _window?.Reanchor();
                else
                    _window?.RefreshPinned();
            });

        private void OnObjectEvent(HWINEVENTHOOK hook, uint iEvent, HWND hWnd, int idObject, int idChild, uint thread, uint time)
        {
            const string LOG_IDENT = "Overlay::OnObjectEvent";

            if (hWnd != _robloxWindow || idObject != OBJID_WINDOW || idChild != CHILDID_SELF)
                return;

            if (iEvent == EVENT_OBJECT_DESTROY)
            {
                App.Logger.WriteLine(LOG_IDENT, "Game window went away");

                Application.Current.Dispatcher.Invoke(() => WindowClosed?.Invoke(this, EventArgs.Empty));
                return;
            }

            try
            {
                PublishBounds();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to track the game window");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private void PublishBounds()
        {
            OverlayBounds bounds = GetBounds();

            if (bounds == _lastBounds)
                return;

            _lastBounds = bounds;

            BoundsChanged?.Invoke(this, bounds);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (ActivityWatcher is not null)
                ActivityWatcher.OnGameJoin -= OnGameJoin;

            Friends.Changed -= OnFriendChanged;
            Friends.Dispose();

            _systemHook?.Dispose();
            _objectHook?.Dispose();
            _foregroundHook?.Dispose();

            _systemHook = null;
            _objectHook = null;
            _foregroundHook = null;

            _systemCallback = null;
            _objectCallback = null;
            _foregroundCallback = null;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                _notices.Clear();
                _toast?.Close();
                _window?.Close();
            });

            _ = Messaging.DisposeAsync();

            GC.SuppressFinalize(this);
        }
    }
}
