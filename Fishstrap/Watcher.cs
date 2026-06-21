using Fishstrap.AppData;
using Fishstrap.Integrations;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Fishstrap
{
    public class Watcher : IDisposable
    {
        private readonly InterProcessLock _lock = new("Watcher");

        private readonly WatcherData? _watcherData;
        
        private readonly NotifyIconWrapper? _notifyIcon;

        public readonly ActivityWatcher? ActivityWatcher;

        public readonly WindowManipulation? WindowManipulation;

        public readonly DiscordRichPresence? RichPresence;

        public Watcher()
        {
            const string LOG_IDENT = "Watcher";


            if (!_lock.IsAcquired)
            {
                App.Logger.WriteLine(LOG_IDENT, "Watcher instance already exists");
                return;
            }

            string? watcherDataArg = App.LaunchSettings.WatcherFlag.Data;

            if (String.IsNullOrEmpty(watcherDataArg))
            {
#if DEBUG
                string path = new RobloxPlayerData().ExecutablePath;
                if (!File.Exists(path))
                    throw new ApplicationException("Roblox player is not been installed");

                using var gameClientProcess = Process.Start(path);

                while (gameClientProcess.MainWindowHandle == IntPtr.Zero)
                    Thread.Sleep(100);

                _watcherData = new() { ProcessId = gameClientProcess.Id, Handle = gameClientProcess.MainWindowHandle.ToInt64() };
#else
                throw new Exception("Watcher data not specified");
#endif
            }
            else
            {
                _watcherData = JsonSerializer.Deserialize<WatcherData>(Encoding.UTF8.GetString(Convert.FromBase64String(watcherDataArg)));
            }

            if (_watcherData is null)
                throw new Exception("Watcher data is invalid");

            if (App.Settings.Prop.EnableWindowManipulation && _watcherData.Handle != 0)
                WindowManipulation = new(_watcherData.Handle, _watcherData.ProcessId);

            if (App.Settings.Prop.EnableActivityTracking)
            {
                ActivityWatcher = new(_watcherData.LogFile);

                if (App.Settings.Prop.UseDisableAppPatch)
                {
                    ActivityWatcher.OnAppClose += delegate
                    {
                        App.Logger.WriteLine(LOG_IDENT, "Received desktop app exit, closing Roblox");
                        using var process = Process.GetProcessById(_watcherData.ProcessId);
                        process.CloseMainWindow();
                    };
                }

                if (App.Settings.Prop.UseDiscordRichPresence && !App.State.Prop.WatcherRunning)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Running rpc");
                    RichPresence = new(ActivityWatcher);
                }
            }

            _notifyIcon = new(this);
        }

        public void KillRobloxProcess() => CloseProcess(_watcherData!.ProcessId, true);

        public void CloseProcess(int pid, bool force = false)
        {
            const string LOG_IDENT = "Watcher::CloseProcess";

            try
            {
                using var process = Process.GetProcessById(pid);

                App.Logger.WriteLine(LOG_IDENT, $"Killing process '{process.ProcessName}' (pid={pid}, force={force})");

                if (process.HasExited)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"PID {pid} has already exited");
                    return;
                }

                if (force)
                    process.Kill();
                else
                    process.CloseMainWindow();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"PID {pid} could not be closed");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private bool IsRobloxWindowClosed()
        {
            const string LOG_IDENT = "Watcher::IsRobloxWindowClosed";
            string playerProcessName = Path.GetFileNameWithoutExtension(App.RobloxPlayerAppName);
            List<Process> processes = Utilities.GetProcessesSafe()
                .Where(x => x.ProcessName == playerProcessName)
                .ToList();

            if (!processes.Any())
                return false;

            foreach (Process process in processes)
            {
                try
                {
                    process.Refresh();

                    if (process.HasExited)
                        continue;

                    IntPtr windowHandle = process.MainWindowHandle;

                    if (windowHandle != IntPtr.Zero && PInvoke.IsWindowVisible((HWND)windowHandle))
                        return false;
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Failed to inspect Roblox window state for PID {process.Id}");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }
            }

            App.Logger.WriteLine(LOG_IDENT, "Roblox is still running with no visible player window");
            return true;
        }

        public async Task Run()
        {
            if (!_lock.IsAcquired || _watcherData is null)
                return;

            ActivityWatcher?.Start();
            WindowManipulation?.Start();

            DateTime backgroundKillStartTime = DateTime.UtcNow.AddSeconds(10);

            while (Utilities.GetProcessesSafe().Any(x => x.Id == _watcherData.ProcessId) || (App.Settings.Prop.KillRobloxOnWindowClose && Utilities.IsRobloxRunning()))
            {
                if (App.Settings.Prop.KillRobloxOnWindowClose && DateTime.UtcNow >= backgroundKillStartTime && IsRobloxWindowClosed())
                {
                    App.Logger.WriteLine("Watcher::Run", "Roblox window closed while process is still running, closing background player processes");
                    CloseProcess(_watcherData.ProcessId, true);
                    Utilities.CloseRobloxPlayerProcesses(true, logIdent: "Watcher::Run");
                    break;
                }

                await Task.Delay(1000);
            }

            if (_watcherData.AutoclosePids is not null)
            {
                foreach (int pid in _watcherData.AutoclosePids)
                    CloseProcess(pid);
            }

            if (App.LaunchSettings.TestModeFlag.Active)
                Process.Start(Paths.Process, "-settings -testmode");
        }

        public void Dispose()
        {
            App.Logger.WriteLine("Watcher::Dispose", "Disposing Watcher");

            _notifyIcon?.Dispose();
            RichPresence?.Dispose();

            App.State.Prop.WatcherRunning = false;

            GC.SuppressFinalize(this);
        }
    }
}
