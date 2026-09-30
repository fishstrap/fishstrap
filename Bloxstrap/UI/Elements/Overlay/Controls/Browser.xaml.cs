using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

using Wpf.Ui.Common;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public partial class Browser : UserControl
    {
        private const string HomeUrl = "https://www.google.com/";
        private const string SearchUrl = "https://www.google.com/search?q=";

        private const string RuntimeClientKey = @"Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}";

        private WebView2? _view;
        private Window? _host;
        private HWND _hostHandle;
        private Int32Rect _hostBounds;

        private bool _started;
        private bool _open = true;
        private bool _loading;

        private string? _pending;

        public event EventHandler? DismissRequested;

        public Browser()
        {
            InitializeComponent();

            ViewHost.IsVisibleChanged += (_, _) => SyncHost();
            ViewHost.LayoutUpdated += (_, _) => PlaceHost(false);
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
            _view?.Dispose();
            _host?.Close();
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
                Window owner = Window.GetWindow(this);

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

                owner.LocationChanged += (_, _) => PlaceHost(false);

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

        private void SyncHost()
        {
            if (_host is null)
                return;

            if (!ViewHost.IsVisible)
            {
                _host.Hide();
                return;
            }

            _host.Show();

            PlaceHost(true);
        }

        private void PlaceHost(bool force)
        {
            if (_host is null || !_host.IsVisible || !ViewHost.IsVisible || PresentationSource.FromVisual(ViewHost) is null)
                return;

            Point topLeft = ViewHost.PointToScreen(new Point(0, 0));
            Point bottomRight = ViewHost.PointToScreen(new Point(ViewHost.ActualWidth, ViewHost.ActualHeight));

            var bounds = new Int32Rect(
                (int)Math.Round(topLeft.X),
                (int)Math.Round(topLeft.Y),
                (int)Math.Round(bottomRight.X - topLeft.X),
                (int)Math.Round(bottomRight.Y - topLeft.Y));

            if (!force && bounds == _hostBounds)
                return;

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
            if (_host is null || _view is null)
                return;

            _host.Activate();
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
