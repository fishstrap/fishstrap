using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

using Bloxstrap.Integrations;

namespace Bloxstrap.UI.Elements.ContextMenu
{
    public partial class MenuContainer
    {
        private readonly Watcher _watcher;
        private ActivityWatcher? _activityWatcher => _watcher.ActivityWatcher;

        private ServerInformation? _serverInformationWindow;
        private ServerHistory? _gameHistoryWindow;
        private Logs? _logsWindow;

        private Stopwatch _totalPlaytimeStopwatch = new Stopwatch();
        private TimeSpan _accumulatedTotalPlaytime = TimeSpan.Zero;

        private DispatcherTimer? _playtimeTimer;

        public MenuContainer(Watcher watcher)
        {
            InitializeComponent();

            _watcher = watcher;

            if (_activityWatcher is not null)
            {
                _activityWatcher.OnLogOpen += ActivityWatcher_OnLogOpen;
                _activityWatcher.OnGameJoin += ActivityWatcher_OnGameJoin;
                _activityWatcher.OnGameLeave += ActivityWatcher_OnGameLeave;

                if (!App.Settings.Prop.UseDisableAppPatch && App.Settings.Prop.ShowGameHistoryMenu)
                    GameHistoryMenuItem.Visibility = Visibility.Visible;
                else
                    GameHistoryMenuItem.Visibility = Visibility.Collapsed;
            }

            if (_watcher.RichPresence is not null)
                RichPresenceMenuItem.Visibility = Visibility.Visible;

            VersionTextBlock.Text = $"{App.ProjectName} v{App.Version}";

            if (App.Settings.Prop.PlaytimeCounter)
            {
                StartTotalPlaytimeTimer();
                PlaytimeMenuItem.Visibility = Visibility.Visible;
            }
            else
            {
                PlaytimeMenuItem.Visibility = Visibility.Collapsed;
            }
        }

        private void StartTotalPlaytimeTimer()
        {
            _totalPlaytimeStopwatch.Start();

            _playtimeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _playtimeTimer.Tick += PlaytimeTimer_Tick;
            _playtimeTimer.Start();
        }

        private void StopTotalPlaytimeTimer()
        {
            _totalPlaytimeStopwatch.Stop();
            _accumulatedTotalPlaytime += _totalPlaytimeStopwatch.Elapsed;
            _totalPlaytimeStopwatch.Reset();

            if (_playtimeTimer != null)
            {
                _playtimeTimer.Tick -= PlaytimeTimer_Tick;
                _playtimeTimer.Stop();
                _playtimeTimer = null;
            }
        }

        private void PlaytimeTimer_Tick(object? sender, EventArgs e)
        {
            TimeSpan totalElapsed = _accumulatedTotalPlaytime + _totalPlaytimeStopwatch.Elapsed;

            if (_activityWatcher is null || !_activityWatcher.InGame)
            {
                PlaytimeTextBlock.Text = $"Playtime: Total {FormatTimeSpan(totalElapsed)}";
            }
            else
            {
                TimeSpan sessionElapsed = DateTime.Now - _activityWatcher!.Data.TimeJoined;
                PlaytimeTextBlock.Text = $"Playtime: Game {FormatTimeSpan(sessionElapsed)} | Total {FormatTimeSpan(totalElapsed)}";
            }
        }

        private static string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}";
            else
                return $"{ts.Minutes}:{ts.Seconds:D2}";
        }

        public void ShowServerInformationWindow()
        {
            if (_serverInformationWindow is null)
            {
                _serverInformationWindow = new(_watcher);
                _serverInformationWindow.Closed += (_, _) => _serverInformationWindow = null;
            }

            if (!_serverInformationWindow.IsVisible)
                _serverInformationWindow.ShowDialog();
            else
                _serverInformationWindow.Activate();
        }

        private void ActivityWatcher_OnLogOpen(object? sender, EventArgs e) =>
            Dispatcher.Invoke(() => DebugMenuItem.Visibility = Visibility.Visible);

        private void ActivityWatcher_OnGameJoin(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (_activityWatcher?.Data.ServerType == ServerType.Public)
                    InviteDeeplinkMenuItem.Visibility = Visibility.Visible;

                ServerDetailsMenuItem.Visibility = Visibility.Visible;

                if (App.FastFlags.GetPreset("Players.LogLevel") == "trace")
                    LogsMenuItem.Visibility = Visibility.Visible;
            });
        }

        private void ActivityWatcher_OnGameLeave(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                InviteDeeplinkMenuItem.Visibility = Visibility.Collapsed;
                ServerDetailsMenuItem.Visibility = Visibility.Collapsed;

                if (App.FastFlags.GetPreset("Players.LogLevel") == "trace")
                {
                    LogsMenuItem.Visibility = Visibility.Collapsed;

                    _logsWindow?.Close();
                }

                _serverInformationWindow?.Close();
            });
        }

        private void Window_Loaded(object? sender, RoutedEventArgs e)
        {
            HWND hWnd = (HWND)new WindowInteropHelper(this).Handle;
            int exStyle = PInvoke.GetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            exStyle |= 0x00000080; // WS_EX_TOOLWINDOW
            PInvoke.SetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, exStyle);
        }

        private void Window_Closed(object sender, EventArgs e) => App.Logger.WriteLine("MenuContainer::Window_Closed", "Context menu container closed");

        private void RichPresenceMenuItem_Click(object sender, RoutedEventArgs e) => _watcher.RichPresence?.SetVisibility(((MenuItem)sender).IsChecked);

        private void InviteDeeplinkMenuItem_Click(object sender, RoutedEventArgs e) => Clipboard.SetDataObject(_activityWatcher?.Data.GetInviteDeeplink());

        private void ServerDetailsMenuItem_Click(object sender, RoutedEventArgs e) => ShowServerInformationWindow();

        private void DebugMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var debugMenu = new DebugMenu();
            debugMenu.Show();
        }

        private void CloseRobloxMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = Frontend.ShowMessageBox(
                Strings.ContextMenu_CloseRobloxMessage,
                MessageBoxImage.Warning,
                MessageBoxButton.YesNo
            );

            if (result != MessageBoxResult.Yes)
                return;

            _watcher.KillRobloxProcess();
            StopTotalPlaytimeTimer();
        }

        private void JoinLastServerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_activityWatcher is null)
                throw new ArgumentNullException(nameof(_activityWatcher));

            if (_gameHistoryWindow is null)
            {
                _gameHistoryWindow = new(_activityWatcher);
                _gameHistoryWindow.Closed += (_, _) => _gameHistoryWindow = null;
            }

            if (!_gameHistoryWindow.IsVisible)
                _gameHistoryWindow.ShowDialog();
            else
                _gameHistoryWindow.Activate();
        }

        private void LogsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_activityWatcher is null)
                throw new ArgumentNullException(nameof(_activityWatcher));

            if (_logsWindow is null)
            {
                _logsWindow = new(_activityWatcher);
                _logsWindow.Closed += (_, _) => _logsWindow = null;
            }

            if (!_logsWindow.IsVisible)
                _logsWindow.ShowDialog();
            else
                _logsWindow.Activate();
        }

        private void CloseFroststrapMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var processes = Process.GetProcessesByName("Bloxstrap");
                foreach (var proc in processes)
                {
                    proc.Kill();
                    proc.WaitForExit();
                }

                this.Close();
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Failed to close Froststrap: {ex.Message}", MessageBoxImage.Error);
            }
        }

    }
}