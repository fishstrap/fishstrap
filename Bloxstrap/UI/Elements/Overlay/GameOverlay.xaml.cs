using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

using Bloxstrap.Enums.Overlay;
using Bloxstrap.UI.Elements.Overlay.Controls;
using Bloxstrap.UI.ViewModels.Overlay;

namespace Bloxstrap.UI.Elements.Overlay
{
    public partial class GameOverlay : Window
    {
        private const int ToggleHotkeyId = 9000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WM_HOTKEY = 0x0312;

        private const double CascadeStep = 44;

        private readonly GameOverlayViewModel _viewModel;
        private readonly Integrations.Overlay? _overlay;

        private HWND _hwnd;
        private HwndSource? _source;
        private bool _hotkeyRegistered;
        private string _shortcut = String.Empty;

        private int _placed;
        private bool _presenting;
        private bool _presented;
        private bool _leaving;
        private int _motion;
        private bool _browserClipQueued;

        private string? _savedLayout;

        private FriendActivity ChatWindow => (FriendActivity)MessagesPanel.PanelContent!;

        private BadgeTracker BadgeTracker => (BadgeTracker)BadgesPanel.PanelContent!;

        private ServerBrowser ServerBrowser => (ServerBrowser)ServersPanel.PanelContent!;

        private Notes NotesPad => (Notes)NotesPanel.PanelContent!;

        private GameBrowser GameBrowser => (GameBrowser)GamesPanel.PanelContent!;

        private GameHistory GameHistory => (GameHistory)HistoryPanel.PanelContent!;

        private Browser BrowserView => (Browser)BrowserPanel.PanelContent!;

        private OverlaySettings SettingsView => (OverlaySettings)SettingsPanel.PanelContent!;

        public string? HotkeyHint => String.IsNullOrEmpty(_shortcut)
            ? null
            : String.Format(_hotkeyRegistered ? Strings.Menu_Overlay_Notify_Hint : Strings.Menu_Overlay_Notify_HintTaken, _shortcut);

        public GameOverlay(Integrations.Overlay? overlay)
        {
            Wpf.Ui.Appearance.Accent.ApplySystemAccent();

            _overlay = overlay;
            _viewModel = new GameOverlayViewModel(this, overlay);

            DataContext = _viewModel;

            InitializeComponent();

            _viewModel.PanelOpened += (_, panel) => Place(panel);
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            BrowserView.DismissRequested += (_, _) => Dismiss();

            Deactivated += OnDeactivated;

            PanelSurface.LayoutUpdated += (_, _) => QueueBrowserClipUpdate();

            ChatWindow.Attach(overlay);
            BadgeTracker.Attach(overlay?.ActivityWatcher);
            BadgeTracker.BadgeEarned += OnBadgeEarned;
            ServerBrowser.Attach(overlay?.ActivityWatcher);
            GameBrowser.Attach(overlay?.ActivityWatcher);
            GameHistory.Attach(overlay?.ActivityWatcher);
            SettingsView.Attach(overlay, this, () => _viewModel.ProfileIcon, () => _viewModel.GameIcon);
        }

        private void OnBadgeEarned(object? sender, Badge badge)
        {
            if (!App.Settings.Prop.OverlayBadgeNotifications)
                return;

            string message = badge.Name + Environment.NewLine + String.Format(Strings.Menu_Overlay_Notify_BadgeRarity, badge.RarityText);

            _overlay?.Notify(new OverlayNotice(Strings.Menu_Overlay_Notify_BadgeEarned, message, badge.IconUrl, Kind: NoticeKind.Badge));
        }

        private OverlayPanel PanelFor(OverlayPanelKind kind) => kind switch
        {
            OverlayPanelKind.Badges => BadgesPanel,
            OverlayPanelKind.Servers => ServersPanel,
            OverlayPanelKind.Notes => NotesPanel,
            OverlayPanelKind.Games => GamesPanel,
            OverlayPanelKind.History => HistoryPanel,
            OverlayPanelKind.Browser => BrowserPanel,
            OverlayPanelKind.Settings => SettingsPanel,
            _ => MessagesPanel
        };

        private void Place(OverlayPanelKind kind)
        {
            OverlayPanel panel = PanelFor(kind);

            if (panel.Tag is bool placed && placed)
            {
                panel.Raise();
                UpdateBrowserClip();
                return;
            }

            panel.Tag = true;

            if (Restore(kind, panel))
            {
                UpdateBrowserClip();
                return;
            }

            (double width, double height) = kind switch
            {
                OverlayPanelKind.Notes => (560d, 420d),
                OverlayPanelKind.Servers => (880d, 520d),
                OverlayPanelKind.Games => (800d, 540d),
                OverlayPanelKind.History => (580d, 440d),
                OverlayPanelKind.Browser => (960d, 600d),
                OverlayPanelKind.Settings => (520d, 640d),
                OverlayPanelKind.Messages => (880d, 580d),
                _ => (720d, 500d)
            };

            double offset = CascadeStep * _placed++;

            panel.PlaceAt(24 + offset, 16 + offset, width, height);

            UpdateBrowserClip();
        }

        private void SyncBrowserHost()
        {
            BrowserView.SetHostVisible(_presented && !_leaving && IsVisible);
        }

        private void QueueBrowserClipUpdate()
        {
            if (_browserClipQueued)
                return;

            _browserClipQueued = true;

            Dispatcher.BeginInvoke(() =>
            {
                _browserClipQueued = false;
                UpdateBrowserClip();
            }, DispatcherPriority.Render);
        }

        private static bool TryGetScreenRect(FrameworkElement element, out Rect screen)
        {
            screen = Rect.Empty;

            if (element.Visibility != Visibility.Visible || element.ActualWidth <= 0 || element.ActualHeight <= 0)
                return false;

            if (PresentationSource.FromVisual(element) is null)
                return false;

            Point topLeft;
            Point bottomRight;

            try
            {
                topLeft = element.PointToScreen(new Point(0, 0));
                bottomRight = element.PointToScreen(new Point(element.ActualWidth, element.ActualHeight));
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            screen = new Rect(topLeft, bottomRight);

            return !screen.IsEmpty && screen.Width > 0 && screen.Height > 0;
        }

        private void UpdateBrowserClip()
        {
            if (!_viewModel.BrowserOpen || BrowserPanel.Visibility != Visibility.Visible)
            {
                BrowserView.SetOccluders(Array.Empty<Rect>());
                return;
            }

            var occluders = new List<Rect>();

            if (Dock.Visibility == Visibility.Visible && TryGetScreenRect(Dock, out Rect dockRect))
                occluders.Add(dockRect);

            int browserDepth = BrowserPanel.Depth;

            foreach (OverlayPanelKind kind in Enum.GetValues<OverlayPanelKind>())
            {
                if (kind == OverlayPanelKind.Browser || !_viewModel.IsOpen(kind))
                    continue;

                OverlayPanel other = PanelFor(kind);

                if (other.Visibility != Visibility.Visible || other.Depth <= browserDepth)
                    continue;

                if (TryGetScreenRect(other, out Rect otherRect))
                    occluders.Add(otherRect);
            }

            BrowserView.SetOccluders(occluders);
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GameOverlayViewModel.BrowserOpen))
            {
                BrowserView.SetOpen(_viewModel.BrowserOpen);
                UpdateBrowserClip();
            }
            else if (e.PropertyName == nameof(GameOverlayViewModel.HasPinnedPanels) && _viewModel.ShowingPinnedOnly)
            {
                RefreshPinned();
            }
            else if (e.PropertyName?.EndsWith("Visibility") == true)
            {
                QueueBrowserClipUpdate();
            }
        }

        private bool Restore(OverlayPanelKind kind, OverlayPanel panel)
        {
            if (!App.State.Prop.OverlayPanels.TryGetValue(kind.ToString(), out OverlayPanelLayout? saved))
                return false;

            if (saved.Width < OverlayPanel.MinPanelWidth || saved.Height < OverlayPanel.MinPanelHeight)
                return false;

            double scaleX = Scale(PanelSurface.ActualWidth, saved.SurfaceWidth);
            double scaleY = Scale(PanelSurface.ActualHeight, saved.SurfaceHeight);

            panel.PlaceAt(saved.Left * scaleX, saved.Top * scaleY, saved.Width, saved.Height);

            return true;
        }

        private static double Scale(double now, double then) => then > 0 && now > 0 ? now / then : 1;

        private void RestoreDepth()
        {
            var order = Enum.GetValues<OverlayPanelKind>()
                .Where(_viewModel.IsOpen)
                .Select(kind => (Kind: kind, Saved: Saved(kind)))
                .Where(x => x.Saved is not null)
                .OrderBy(x => x.Saved!.Depth);

            foreach (var (kind, _) in order)
                PanelFor(kind).Raise();

            UpdateBrowserClip();
        }

        private static OverlayPanelLayout? Saved(OverlayPanelKind kind) =>
            App.State.Prop.OverlayPanels.TryGetValue(kind.ToString(), out OverlayPanelLayout? saved) ? saved : null;

        private void Persist()
        {
            NotesPad.Flush();
            SettingsView.Flush();

            SaveLayout();
        }

        private void SaveLayout()
        {
            const string LOG_IDENT = "GameOverlay::SaveLayout";

            try
            {
                var panels = new Dictionary<string, OverlayPanelLayout>();

                foreach (OverlayPanelKind kind in Enum.GetValues<OverlayPanelKind>())
                {
                    OverlayPanel panel = PanelFor(kind);
                    string name = kind.ToString();

                    if (panel.Tag is not bool placed || !placed)
                    {
                        if (Saved(kind) is OverlayPanelLayout previous)
                        {
                            previous.Open = _viewModel.IsOpen(kind);
                            previous.Pinned = _viewModel.IsPinned(kind);
                            panels[name] = previous;
                        }

                        continue;
                    }

                    panels[name] = new OverlayPanelLayout
                    {
                        Open = _viewModel.IsOpen(kind),
                        Pinned = _viewModel.IsPinned(kind),
                        Left = panel.Position.X,
                        Top = panel.Position.Y,
                        Width = panel.PanelSize.Width,
                        Height = panel.PanelSize.Height,
                        Depth = panel.Depth,
                        SurfaceWidth = panel.SurfaceSize.Width,
                        SurfaceHeight = panel.SurfaceSize.Height
                    };
                }

                string serialised = JsonSerializer.Serialize(panels);

                if (serialised == _savedLayout)
                    return;

                _savedLayout = serialised;

                if (File.Exists(App.State.FileLocation) && App.State.HasFileOnDiskChanged())
                    App.State.Load(false);

                App.State.Prop.OverlayPanels = panels;
                App.State.Save();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to remember the layout");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        public void PrepareHidden() => new WindowInteropHelper(this).EnsureHandle();

        public void Toggle()
        {
            const string LOG_IDENT = "GameOverlay::Toggle";

            bool presented = _presented;
            bool inFront = IsInFront();
            bool minimised = _overlay?.IsGameMinimised() == true;

            var action = OverlayToggle.Decide(presented, inFront, minimised);

            App.Logger.WriteLine(LOG_IDENT, $"presented={presented} inFront={inFront} minimised={minimised} -> {action}");

            switch (action)
            {
                case OverlayToggleAction.Hide:
                    Dismiss();
                    break;

                case OverlayToggleAction.Present:
                    Present();
                    break;
            }
        }

        public void Dismiss() => Dismiss(true);

        public void Open()
        {
            if (_presented)
                Activate();
            else
                Present();
        }

        public void OpenPage(Uri address)
        {
            Open();

            _viewModel.OpenPanel(OverlayPanelKind.Browser);

            BrowserView.Navigate(address);
        }

        private void Dismiss(bool returnFocus)
        {
            if (_leaving)
                return;

            Persist();

            _presented = false;
            SyncBrowserHost();

            bool minimised = _overlay?.IsGameMinimised() == true;
            bool hide = !_viewModel.HasPinnedPanels || minimised;

            if (!OverlayMotion.Enabled || !IsVisible || _viewModel.ShowingPinnedOnly)
            {
                Settle(hide, returnFocus);
                return;
            }

            _leaving = true;

            int motion = ++_motion;

            Dock.IsHitTestVisible = false;

            if (returnFocus && !minimised)
                _overlay?.FocusGame();

            OverlayMotion.FadeOut(Dim);

            if (hide)
            {
                OverlayMotion.FadeOut(PanelSurface);
            }
            else
            {
                foreach (OverlayPanel panel in LoosePanels())
                    OverlayMotion.FadeOut(panel);
            }

            OverlayMotion.Drop(Dock, () =>
            {
                if (motion != _motion)
                    return;

                _leaving = false;

                Settle(hide, returnFocus);
            });
        }

        private void Settle(bool hide, bool returnFocus)
        {
            if (hide)
            {
                Hide();
            }
            else if (returnFocus)
            {
                ShowPinnedView();
                _overlay?.FocusGame();
            }
            else
            {
                RefreshPinned();
            }

            ResetMotion();
            SyncBrowserHost();
            UpdateBrowserClip();
        }

        private void Enter(bool fromHidden, bool fromPinned, bool reversing)
        {
            if (!OverlayMotion.Enabled)
            {
                ResetMotion();
                return;
            }

            Dock.IsHitTestVisible = true;

            if (!reversing)
            {
                OverlayMotion.Lower(Dock);
                OverlayMotion.Conceal(Dim);

                if (fromHidden)
                {
                    OverlayMotion.Conceal(PanelSurface);
                }
                else if (fromPinned)
                {
                    foreach (OverlayPanel panel in LoosePanels())
                        OverlayMotion.Conceal(panel);
                }
            }

            OverlayMotion.Rise(Dock);
            OverlayMotion.FadeIn(Dim);
            OverlayMotion.FadeIn(PanelSurface);

            foreach (OverlayPanel panel in LoosePanels())
                OverlayMotion.FadeIn(panel);
        }

        private void ResetMotion()
        {
            Dock.IsHitTestVisible = true;

            OverlayMotion.Rest(Dock);
            OverlayMotion.Rest(Dim);
            OverlayMotion.Rest(PanelSurface);

            foreach (OverlayPanel panel in PanelSurface.Children.OfType<OverlayPanel>())
                OverlayMotion.Rest(panel);
        }

        private IEnumerable<OverlayPanel> LoosePanels() => Enum.GetValues<OverlayPanelKind>()
            .Where(kind => _viewModel.IsOpen(kind) && !_viewModel.IsPinned(kind))
            .Select(PanelFor);

        public void HideForGame()
        {
            Persist();

            _presented = false;
            _leaving = false;
            _motion++;

            Hide();

            ResetMotion();
            SyncBrowserHost();
        }

        public void RefreshPinned()
        {
            if (_presented || _leaving)
                return;

            bool gameOrOverlayInFront = _overlay?.IsGameForeground() == true || IsOwnProcessForeground();

            if (_viewModel.HasPinnedPanels && gameOrOverlayInFront && _overlay?.IsGameMinimised() != true)
                ShowPinnedView();
            else if (IsVisible)
                Hide();

            SyncBrowserHost();
            UpdateBrowserClip();
        }

        private void ShowPinnedView()
        {
            _viewModel.ShowingPinnedOnly = true;

            if (!IsVisible)
                Show();

            SyncBrowserHost();
            UpdateBrowserClip();
        }

        private void ScrimClicked(object sender, MouseButtonEventArgs e)
        {
            if (!ReferenceEquals(e.OriginalSource, Scrim))
                return;

            Dismiss();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key != Key.Escape)
                return;

            Dismiss();

            e.Handled = true;
        }

        private bool IsInFront()
        {
            HWND foreground = PInvoke.GetForegroundWindow();

            return foreground == _hwnd || BrowserView.IsHostWindow(foreground) || _overlay?.IsGameForeground() == true;
        }

        private void OnDeactivated(object? sender, EventArgs e)
        {
            const string LOG_IDENT = "GameOverlay::OnDeactivated";

            if (_presenting || !_presented)
                return;

            if (IsOwnProcessForeground())
                return;

            if (_overlay?.IsGameForeground() == true)
                return;

            App.Logger.WriteLine(LOG_IDENT, "Focus left the game, hiding");

            Dismiss(false);
        }

        private static unsafe bool IsOwnProcessForeground()
        {
            uint processId = 0;

            PInvoke.GetWindowThreadProcessId(PInvoke.GetForegroundWindow(), &processId);

            return processId == Environment.ProcessId;
        }

        private void Present()
        {
            bool fromHidden = !IsVisible;
            bool fromPinned = !fromHidden && _viewModel.ShowingPinnedOnly;
            bool reversing = _leaving;

            _motion++;
            _leaving = false;

            _presenting = true;
            _presented = true;

            SyncBrowserHost();

            _viewModel.ShowingPinnedOnly = false;

            Enter(fromHidden, fromPinned, reversing);

            _ = _viewModel.LoadProfileAsync();

            _overlay?.DismissToast();

            if (_overlay?.IsGameForeground() == false)
                _overlay.FocusGame();

            if (Visibility != Visibility.Visible)
                Show();

            if (WindowState == System.Windows.WindowState.Minimized)
                WindowState = System.Windows.WindowState.Normal;

            Reanchor();

            Activate();
            Focus();

            SyncBrowserHost();
            UpdateBrowserClip();

            Dispatcher.BeginInvoke(new Action(() =>
            {
                _presenting = false;
                SyncBrowserHost();
                UpdateBrowserClip();
            }), DispatcherPriority.ApplicationIdle);
        }

        public void Reanchor()
        {
            RefreshPinned();

            _overlay?.AnchorAboveGame(_hwnd);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (OverlayPanel panel in PanelSurface.Children.OfType<OverlayPanel>())
                panel.Clamp();

            QueueBrowserClipUpdate();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {

            foreach (OverlayPanelKind kind in Enum.GetValues<OverlayPanelKind>())
            {
                if (_viewModel.IsOpen(kind))
                    Place(kind);
            }

            RestoreDepth();

            await _viewModel.OnLoaded();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var helper = new WindowInteropHelper(this);

            _hwnd = new HWND(helper.Handle);
            _source = HwndSource.FromHwnd(helper.Handle);
            _source?.AddHook(HwndHook);

            int exStyle = PInvoke.GetWindowLong(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            PInvoke.SetWindowLong(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);

            RegisterHotkey();
        }

        public bool HotkeyRegistered => _hotkeyRegistered;

        public string Shortcut => _shortcut;

        public void RebindHotkey()
        {
            UnregisterHotkey();
            RegisterHotkey();
        }

        public void SuspendHotkey() => UnregisterHotkey();

        private void UnregisterHotkey()
        {
            if (_hotkeyRegistered)
                PInvoke.UnregisterHotKey(_hwnd, ToggleHotkeyId);

            _hotkeyRegistered = false;
        }

        private void RegisterHotkey()
        {
            const string LOG_IDENT = "GameOverlay::RegisterHotkey";

            if (_hwnd == HWND.Null)
                return;

            ModifierKeys modifiers = App.Settings.Prop.OverlayHotkeyModifiers;
            Key key = App.Settings.Prop.OverlayHotkeyKey;

            if (!OverlayHotkey.IsAllowed(modifiers, key))
            {
                App.Logger.WriteLine(LOG_IDENT, $"The saved overlay hotkey ({OverlayHotkey.Describe(modifiers, key)}) isn't usable, falling back to the default");

                modifiers = OverlayHotkey.DefaultModifiers;
                key = OverlayHotkey.DefaultKey;
            }

            string shortcut = OverlayHotkey.Describe(modifiers, key);

            _shortcut = shortcut;

            _hotkeyRegistered = PInvoke.RegisterHotKey(
                _hwnd, ToggleHotkeyId, OverlayHotkey.ToNative(modifiers), (uint)KeyInterop.VirtualKeyFromKey(key));

            App.Logger.WriteLine(LOG_IDENT, _hotkeyRegistered
                ? $"Overlay hotkey is {shortcut}"
                : $"Could not register the overlay hotkey, something else owns {shortcut}");
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg != WM_HOTKEY || wParam.ToInt32() != ToggleHotkeyId)
                return IntPtr.Zero;

            Toggle();

            handled = true;

            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            Persist();

            _source?.RemoveHook(HwndHook);

            BrowserView.Shutdown();

            UnregisterHotkey();

            base.OnClosed(e);
        }
    }
}
