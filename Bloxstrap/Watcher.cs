using Bloxstrap.AppData;
using Bloxstrap.Integrations;
using Bloxstrap.Models;

namespace Bloxstrap
{
    public class Watcher : IDisposable
    {
        private readonly InterProcessLock _lock = new("Watcher");

        private readonly WatcherData? _watcherData;

        private readonly NotifyIconWrapper? _notifyIcon;

        public readonly ActivityWatcher? ActivityWatcher;

        public readonly DiscordRichPresence? RichPresence;

        public readonly IntegrationWatcher? IntegrationWatcher;

        public Watcher()
        {
            const string LOG_IDENT = "Watcher";

            if (!_lock.IsAcquired)
            {
                App.Logger.WriteLine(LOG_IDENT, "Watcher instance already exists");
                return;
            }

            string? watcherDataArg = App.LaunchSettings.WatcherFlag.Data;

#if DEBUG
            if (String.IsNullOrEmpty(watcherDataArg))
            {
                string path = new RobloxPlayerData().ExecutablePath;
                using var gameClientProcess = Process.Start(path);

                _watcherData = new() { ProcessId = gameClientProcess.Id };
            }
#else
            if (String.IsNullOrEmpty(watcherDataArg))
                throw new Exception("Watcher data not specified");
#endif

            if (!String.IsNullOrEmpty(watcherDataArg))
                _watcherData = JsonSerializer.Deserialize<WatcherData>(Encoding.UTF8.GetString(Convert.FromBase64String(watcherDataArg)));

            if (_watcherData is null)
                throw new Exception("Watcher data is invalid");

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

                if (App.Settings.Prop.UseDiscordRichPresence)
                    RichPresence = new(ActivityWatcher);

                IntegrationWatcher = new IntegrationWatcher(ActivityWatcher);
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

        public async Task Run()
        {
            if (!_lock.IsAcquired || _watcherData is null)
                return;

            ActivityWatcher?.Start();

            while (Utilities.GetProcessesSafe().Any(x => x.Id == _watcherData.ProcessId))
                await Task.Delay(1000);

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

            IntegrationWatcher?.Dispose();
            _notifyIcon?.Dispose();
            RichPresence?.Dispose();

            GC.SuppressFinalize(this);
        }

        public static void ApplyRecordingBlock(bool block, bool saveSetting = false)
        {
            const string LOG_IDENT = "Watcher::ApplyRecordingBlock";

            string videosPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Roblox");
            string backupPath = videosPath + " (Before Blocking)";

            try
            {
                if (block)
                {
                    if (Directory.Exists(videosPath))
                    {
                        bool hasContent = Directory.EnumerateFileSystemEntries(videosPath).Any();

                        if (hasContent)
                        {
                            if (!Directory.Exists(backupPath))
                                Directory.Move(videosPath, backupPath);
                        }
                        else
                        {
                            Directory.Delete(videosPath);
                        }
                    }

                    if (!File.Exists(videosPath))
                    {
                        File.WriteAllBytes(videosPath, Array.Empty<byte>());
                        File.SetAttributes(videosPath, FileAttributes.ReadOnly);
                    }
                }
                else
                {
                    if (File.Exists(videosPath) && !Directory.Exists(videosPath))
                    {
                        var attributes = File.GetAttributes(videosPath);
                        if ((attributes & FileAttributes.ReadOnly) != 0)
                        {
                            attributes &= ~FileAttributes.ReadOnly;
                            File.SetAttributes(videosPath, attributes);
                        }

                        File.Delete(videosPath);
                    }
                    if (!Directory.Exists(videosPath) && Directory.Exists(backupPath))
                    {
                        Directory.Move(backupPath, videosPath);
                    }
                }

                App.Settings.Prop.BlockRobloxRecording = block;

                if (saveSetting)
                    App.Settings.Save();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        public static void ApplyScreenshotBlock(bool block, bool saveSetting = false)
        {
            const string LOG_IDENT = "Watcher::ApplyScreenshotBlock";

            string picturesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Roblox");
            string backupPath = picturesPath + " (Before Blocking)";

            try
            {
                if (block)
                {
                    if (Directory.Exists(picturesPath))
                    {
                        bool hasContent = Directory.EnumerateFileSystemEntries(picturesPath).Any();

                        if (hasContent)
                        {
                            if (!Directory.Exists(backupPath))
                            {
                                Directory.Move(picturesPath, backupPath);
                                App.Logger.WriteLine(LOG_IDENT, $"Moved existing folder to '{backupPath}'");
                            }
                        }
                        else
                        {
                            Directory.Delete(picturesPath);
                            App.Logger.WriteLine(LOG_IDENT, $"Deleted empty folder '{picturesPath}'");
                        }
                    }

                    if (!File.Exists(picturesPath))
                    {
                        File.WriteAllBytes(picturesPath, Array.Empty<byte>());
                        File.SetAttributes(picturesPath, FileAttributes.ReadOnly);
                        App.Logger.WriteLine(LOG_IDENT, $"Created read-only file '{picturesPath}'");
                    }
                }
                else
                {
                    if (File.Exists(picturesPath) && !Directory.Exists(picturesPath))
                    {
                        var attributes = File.GetAttributes(picturesPath);
                        if ((attributes & FileAttributes.ReadOnly) != 0)
                        {
                            attributes &= ~FileAttributes.ReadOnly;
                            File.SetAttributes(picturesPath, attributes);
                        }

                        File.Delete(picturesPath);
                        App.Logger.WriteLine(LOG_IDENT, $"Deleted read-only file '{picturesPath}'");
                    }

                    if (!Directory.Exists(picturesPath) && Directory.Exists(backupPath))
                    {
                        Directory.Move(backupPath, picturesPath);
                        App.Logger.WriteLine(LOG_IDENT, $"Restored backup folder from '{backupPath}'");
                    }
                }

                App.Settings.Prop.BlockRobloxScreenshots = block;

                if (saveSetting)
                    App.Settings.Save();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

    }
}