using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

using Wpf.Ui.Common;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public partial class Browser : UserControl
    {
        private const string HomeUrl = "https://www.google.com/";
        private const string SearchUrl = "https://www.google.com/search?q=";

        private const string RuntimeClientKey = @"Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}";

        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private WebView2? _view;
        private Window? _host;
        private Window? _owner;
        private HWND _hostHandle;
        private Int32Rect _hostBounds;

        private bool _started;
        private bool _open = true;
        private bool _loading;

        private bool _hostVisible = true;
        private readonly List<Rect> _occluders = new();
        private bool _placeQueued;

        private string? _pending;

        public event EventHandler? DismissRequested;

        public Browser()
        {
            InitializeComponent();

            ViewHost.IsVisibleChanged += (_, _) => SyncHost();
            ViewHost.LayoutUpdated += (_, _) => QueuePlaceHost();
            IsVisibleChanged += (_, _) => SyncHost();
        }

        [SupportedOSPlatformGuard("windows10.0.17763.0")]
        private static bool IsSupported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763);

        private CoreWebView2? Core => IsSupported ? _view?.CoreWebView2 : null;

        internal bool IsHostWindow(HWND window) => _host is not null && window == _hostHandle;

        public void SetOpen(bool open)
        {
            _open = open;

            if (Core is not null)
                Core.IsMuted = !open;

            SyncHost();
        }

        public void SetHostVisible(bool visible)
        {
            _hostVisible = visible;
            SyncHost();
        }

        public void SetOccluders(IReadOnlyList<Rect> occluders)
        {
            _occluders.Clear();
            _occluders.AddRange(occluders);
            ApplyClip();
        }

        public void Navigate(Uri address)
        {
            if (Core is not null)
                Core.Navigate(address.AbsoluteUri);
            else
                _pending = address.AbsoluteUri;
        }

        public void Shutdown()
        {
            if (_owner is not null)
            {
                _owner.LocationChanged -= OnOwnerMoved;
                _owner.IsVisibleChanged -= OnOwnerIsVisibleChanged;
                _owner.StateChanged -= OnOwnerStateChanged;
                _owner = null;
            }

            _view?.Dispose();
            _view = null;
            _occluders.Clear();
            _host?.Close();
            _host = null;
            _hostHandle = HWND.Null;
        }

        private async void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || _started)
                return;

            _started = true;

            if (IsSupported)
                await StartAsync();
            else
                ShowFailed();
        }

        // shut up compiler
        [SupportedOSPlatform("windows10.0.17763.0")]
        private async Task StartAsync()
        {
            const string LOG_IDENT = "Browser::StartAsync";

            try
            {
                Window? owner = Window.GetWindow(this);

                if (owner is null)
                    throw new InvalidOperationException("Browser has no owner window");

                _owner = owner;

                _view = new WebView2();

                _host = new Window
                {
                    Owner = owner,
                    Content = _view,
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    ShowInTaskbar = false,
                    ShowActivated = false,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -32000,
                    Top = -32000,
                    Width = 1,
                    Height = 1,
                    Background = TryFindResource("ApplicationBackgroundBrush") as Brush ?? Brushes.Transparent
                };

                _host.PreviewKeyDown += OnHostKeyDown;
                _hostHandle = new HWND(new WindowInteropHelper(_host).EnsureHandle());

                int exStyle = PInvoke.GetWindowLong(_hostHandle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
                PInvoke.SetWindowLong(_hostHandle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

                owner.LocationChanged += OnOwnerMoved;
                owner.IsVisibleChanged += OnOwnerIsVisibleChanged;
                owner.StateChanged += OnOwnerStateChanged;

                SyncHost();

                CoreWebView2Environment environment = await CreateEnvironmentAsync(Path.Combine(Paths.Base, "WebView2"));

                await _view.EnsureCoreWebView2Async(environment);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to start the browser");
                App.Logger.WriteException(LOG_IDENT, ex);

                _host?.Close();
                _host = null;

                ShowFailed();
                return;
            }

            if (_view is null)
                return;

            CoreWebView2 core = _view.CoreWebView2;

            core.IsMuted = !_open;

            core.NewWindowRequested += OnNewWindowRequested;
            core.HistoryChanged += (_, _) => UpdateHistory();
            core.SourceChanged += (_, _) => ShowAddress();
            core.NavigationStarting += (_, _) => SetLoading(true);
            core.NavigationCompleted += (_, _) => SetLoading(false);

            core.Navigate(_pending ?? HomeUrl);

            _pending = null;
        }

        [SupportedOSPlatform("windows10.0.17763.0")]
        private static async Task<CoreWebView2Environment> CreateEnvironmentAsync(string userDataFolder)
        {
            const string LOG_IDENT = "Browser::CreateEnvironmentAsync";

            try
            {
                return await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            }
            catch (WebView2RuntimeNotFoundException)
            {
                string? runtime = FindInstalledRuntime();

                if (runtime is null)
                    throw;

                App.Logger.WriteLine(LOG_IDENT, $"WebView2 couldn't find its runtime, using the installed one at {runtime}");

                return await CoreWebView2Environment.CreateAsync(runtime, userDataFolder);
            }
        }

        private static string? FindInstalledRuntime()
        {
            foreach (string path in new[] { $@"SOFTWARE\WOW6432Node\{RuntimeClientKey}", $@"SOFTWARE\{RuntimeClientKey}" })
            {
                foreach (RegistryKey root in new[] { Registry.LocalMachine, Registry.CurrentUser })
                {
                    using RegistryKey? key = root.OpenSubKey(path);

                    if (key?.GetValue("location") is not string location || key.GetValue("pv") is not string version)
                        continue;

                    string folder = Path.Combine(location, version);

                    if (File.Exists(Path.Combine(folder, "msedgewebview2.exe")))
                        return folder;
                }
            }

            return null;
        }

        private void ShowFailed()
        {
            ViewHost.Visibility = Visibility.Collapsed;
            FailedText.Visibility = Visibility.Visible;
        }

        private void OnOwnerMoved(object? sender, EventArgs e) => QueuePlaceHost();

        private void OnOwnerIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) => SyncHost();

        private void OnOwnerStateChanged(object? sender, EventArgs e) => SyncHost();

        private void SyncHost()
        {
            if (_host is null)
                return;

            bool ownerVisible = _owner?.IsVisible == true && _owner?.WindowState != System.Windows.WindowState.Minimized;

            if (!ViewHost.IsVisible || !IsVisible || !_hostVisible || !ownerVisible)
            {
                if (_host.IsVisible)
                    _host.Hide();

                return;
            }

            if (!_host.IsVisible)
                _host.Show();

            PlaceHost(true);
        }

        private void QueuePlaceHost()
        {
            if (_placeQueued || _host is null)
                return;

            _placeQueued = true;

            Dispatcher.BeginInvoke(() =>
            {
                _placeQueued = false;
                PlaceHost(false);
            }, DispatcherPriority.Render);
        }

        private void PlaceHost(bool force)
        {
            if (_host is null || !_host.IsVisible || !ViewHost.IsVisible || !IsVisible)
                return;

            if (!_hostVisible)
                return;

            if (_owner?.IsVisible != true || _owner?.WindowState == System.Windows.WindowState.Minimized)
                return;

            if (PresentationSource.FromVisual(ViewHost) is null)
                return;

            Point topLeft;
            Point bottomRight;

            try
            {
                topLeft = ViewHost.PointToScreen(new Point(0, 0));
                bottomRight = ViewHost.PointToScreen(new Point(ViewHost.ActualWidth, ViewHost.ActualHeight));
            }
            catch (InvalidOperationException)
            {
                return;
            }

            var bounds = new Int32Rect(
                (int)Math.Round(topLeft.X),
                (int)Math.Round(topLeft.Y),
                (int)Math.Round(bottomRight.X - topLeft.X),
                (int)Math.Round(bottomRight.Y - topLeft.Y));

            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            if (!force && bounds == _hostBounds)
            {
                ApplyClip();
                return;
            }

            _hostBounds = bounds;

            PInvoke.SetWindowPos(
                _hostHandle,
                HWND.Null,
                bounds.X,
                bounds.Y,
                bounds.Width,
                bounds.Height,
                SET_WINDOW_POS_FLAGS.SWP_NOZORDER |
                SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);

            ApplyClip();
        }

        private bool TryGetViewScreenRect(out Rect screen)
        {
            screen = Rect.Empty;

            if (ViewHost.ActualWidth <= 0 || ViewHost.ActualHeight <= 0)
                return false;

            if (PresentationSource.FromVisual(ViewHost) is null)
                return false;

            Point topLeft;
            Point bottomRight;

            try
            {
                topLeft = ViewHost.PointToScreen(new Point(0, 0));
                bottomRight = ViewHost.PointToScreen(new Point(ViewHost.ActualWidth, ViewHost.ActualHeight));
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            screen = new Rect(topLeft, bottomRight);

            return !screen.IsEmpty && screen.Width > 0 && screen.Height > 0;
        }

        private unsafe void ApplyClip()
        {
            if (_host is null || !_host.IsVisible || _hostHandle == HWND.Null)
                return;

            if (!TryGetViewScreenRect(out Rect view))
                return;

            var cuts = new List<RECT>();

            foreach (Rect occluder in _occluders)
            {
                Rect hit = Rect.Intersect(view, occluder);

                if (hit.IsEmpty || hit.Width <= 0 || hit.Height <= 0)
                    continue;

                cuts.Add(new RECT
                {
                    left = (int)Math.Round(hit.X - view.X),
                    top = (int)Math.Round(hit.Y - view.Y),
                    right = (int)Math.Round(hit.X - view.X + hit.Width),
                    bottom = (int)Math.Round(hit.Y - view.Y + hit.Height)
                });
            }

            if (cuts.Count == 0)
            {
                HRGN empty = default;
                PInvoke.SetWindowRgn(_hostHandle, empty, true);
                return;
            }

            HRGN full = PInvoke.CreateRectRgn(0, 0, (int)Math.Round(view.Width), (int)Math.Round(view.Height));

            if (full == HRGN.Null)
                return;

            foreach (RECT cut in cuts)
            {
                HRGN hole = PInvoke.CreateRectRgn(cut.left, cut.top, cut.right, cut.bottom);

                if (hole == HRGN.Null)
                    continue;

                PInvoke.CombineRgn(full, full, hole, RGN_COMBINE_MODE.RGN_DIFF);
                PInvoke.DeleteObject(new HGDIOBJ((IntPtr)hole.Value));
            }

            PInvoke.SetWindowRgn(_hostHandle, full, true);
        }

        private void OnHostKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape || Core?.ContainsFullScreenElement == true)
                return;

            e.Handled = true;

            DismissRequested?.Invoke(this, EventArgs.Empty);
        }

        private void FocusPage()
        {
            if (_host is null || _view is null || !_host.IsVisible)
                return;

            _view.Focus();
        }

        private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;

            if (IsWebAddress(e.Uri, out Uri? uri))
                Core?.Navigate(uri.AbsoluteUri);
        }

        private void UpdateHistory()
        {
            BackButton.IsEnabled = Core?.CanGoBack == true;
            ForwardButton.IsEnabled = Core?.CanGoForward == true;
        }

        private void ShowAddress()
        {
            if (AddressBox.IsKeyboardFocusWithin)
                return;

            AddressBox.Text = Core?.Source ?? String.Empty;
        }

        private void SetLoading(bool loading)
        {
            _loading = loading;

            RefreshIcon.Symbol = loading ? SymbolRegular.Dismiss24 : SymbolRegular.ArrowClockwise24;
            RefreshButton.ToolTip = loading ? Strings.Menu_Overlay_Browser_Stop : Strings.Menu_Overlay_Refresh;
        }

        private void BackClicked(object sender, RoutedEventArgs e) => Core?.GoBack();

        private void ForwardClicked(object sender, RoutedEventArgs e) => Core?.GoForward();

        private void HomeClicked(object sender, RoutedEventArgs e) => Core?.Navigate(HomeUrl);

        private void RefreshClicked(object sender, RoutedEventArgs e)
        {
            if (_loading)
                Core?.Stop();
            else
                Core?.Reload();
        }

        private void OpenExternallyClicked(object sender, RoutedEventArgs e)
        {
            if (IsWebAddress(Core?.Source, out Uri? uri))
                Utilities.ShellExecute(uri.AbsoluteUri);
        }

        private void AddressKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;

                string? target = Resolve(AddressBox.Text);

                if (target is null || Core is null)
                    return;

                Core.Navigate(target);
                FocusPage();
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;

                FocusPage();
                ShowAddress();
            }
        }

        private void AddressGotFocus(object sender, KeyboardFocusChangedEventArgs e) => AddressBox.SelectAll();

        private void AddressMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (AddressBox.IsKeyboardFocusWithin)
                return;

            e.Handled = true;

            AddressBox.Focus();
        }

        private static bool IsWebAddress(string? address, [NotNullWhen(true)] out Uri? uri) =>
            Uri.TryCreate(address, UriKind.Absolute, out uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        private static string? Resolve(string input)
        {
            string text = input.Trim();

            if (text.Length == 0)
                return null;

            if (IsWebAddress(text, out Uri? uri))
                return uri.AbsoluteUri;

            if (!text.Contains("://") && !text.Contains(' ') && text.Contains('.') && IsWebAddress($"https://{text}", out uri))
                return uri.AbsoluteUri;

            return SearchUrl + Uri.EscapeDataString(text);
        }
    }
}
