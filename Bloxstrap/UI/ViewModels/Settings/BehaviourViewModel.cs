using Bloxstrap.AppData;
using Bloxstrap.Enums;
using Bloxstrap.Models.APIs.Fishstrap;
using Bloxstrap.RobloxInterfaces;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public partial class BehaviourViewModel : NotifyPropertyChangedViewModel
    {
        public ObservableCollection<Artifact>? Artifacts { get; set; } = new ObservableCollection<Artifact>();

        public BehaviourViewModel()
        {
            App.Cookies.StateChanged += (object? _, CookieState state) => CookieLoadingFailed = state != CookieState.Success && state != CookieState.Unknown;
        }

        [RelayCommand]
        public async Task DownloadButtonClicked(Artifact artifact)
        {
            if (artifact != null)
                await DownloadArtifact(artifact);
        }

        //modified version of CheckForUpdates
        private async Task DownloadArtifact(Artifact artifact)
        {
            const string LOG_IDENT = "BehaviourViewModel::DownloadArtifact";

            try
            {
                string downloadLocation = Path.Combine(Paths.TempUpdates, $"Fishstrap-{artifact.Hash}.exe");

                Directory.CreateDirectory(Paths.TempUpdates);

                App.Logger.WriteLine(LOG_IDENT, $"Downloading artifact with (branch: {artifact.Branch}, hash: {artifact.Hash})...");

                if (!File.Exists(downloadLocation))
                {
                    var response = await App.HttpClient.GetAsync(artifact.Url);

                    await using var fileStream = new FileStream(downloadLocation, FileMode.OpenOrCreate, FileAccess.Write);
                    await response.Content.CopyToAsync(fileStream);
                }

                App.Logger.WriteLine(LOG_IDENT, $"Starting artifact (branch: {artifact.Branch}, hash: {artifact.Hash})...");

                ProcessStartInfo startInfo = new()
                {
                    FileName = downloadLocation,
                };

                startInfo.ArgumentList.Add("-upgrade");

                foreach (string arg in App.LaunchSettings.Args)
                    startInfo.ArgumentList.Add(arg);

                App.Settings.Save();

                new InterProcessLock("AutoUpdater");
                Process.Start(startInfo);

                // glup.
                Application.Current.Shutdown();
            } catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "An exception occurred when running the auto-updater");
                App.Logger.WriteException(LOG_IDENT, ex);

                Frontend.ShowMessageBox(
                    string.Format("Failed to fetch artifact with hash: ", artifact.Hash),
                    MessageBoxImage.Information
                );
            }

        }

        public async Task GetCanaryBuilds()
        {
            const string LOG_IDENT = "BehaviourViewModel::GetLatestArtifacts";

            try
            {
                var artifacts = await Http.GetJson<List<Artifact>>("https://fishstrap.app/fetchArtifact?workflow=Release&amount=5");

                if (artifacts != null)
                {
                    Artifacts.Clear();

                    foreach (var artifact in artifacts)
                        Artifacts.Add(artifact);
                } else
                {
                    App.Logger.WriteLine(LOG_IDENT, "artifacts api responded with empty response...");
                }
            } finally
            {
                IsLoading = Visibility.Collapsed;
            }
        }

        public bool IsRobloxInstallationMissing => String.IsNullOrEmpty(App.RobloxState.Prop.Player.VersionGuid) && String.IsNullOrEmpty(App.RobloxState.Prop.Studio.VersionGuid);

        public bool CookieAccess
        {
            get => App.Settings.Prop.AllowCookieAccess;
            set
            {
                App.Settings.Prop.AllowCookieAccess = value;
                if (value)
                    Task.Run(App.Cookies.LoadCookies);

                OnPropertyChanged(nameof(CookieAccess));
            }
        }

        private Visibility _isLoading = Visibility.Visible;
        public Visibility IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        private Visibility _canaryDownloaderVisibility = App.Settings.Prop.DeveloperMode ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CanaryDownloaderVisibility
        {
            get => _canaryDownloaderVisibility;
            set  
            {
                _canaryDownloaderVisibility = value;
                OnPropertyChanged(nameof(CanaryDownloaderVisibility));
            }
        }

        // guh
        private bool _cookieLoadingFailed;
        public bool CookieLoadingFailed
        {
            get => _cookieLoadingFailed;
            set
            {
                _cookieLoadingFailed = value;
                OnPropertyChanged(nameof(CookieLoadingFailed));
            }
        }

        public bool EnableBetterMatchmaking
        {
            get => App.Settings.Prop.EnableBetterMatchmaking;
            set => App.Settings.Prop.EnableBetterMatchmaking = value;
        }

        public bool EnableBetterMatchmakingRandomization
        {
            get => App.Settings.Prop.EnableBetterMatchmakingRandomization;
            set => App.Settings.Prop.EnableBetterMatchmakingRandomization = value;
        }
        public bool EnableFakeBorderlessFullscreen
        {
            get => App.Settings.Prop.FakeBorderlessFullscreen;
            set => App.Settings.Prop.FakeBorderlessFullscreen = value;
        }

        public bool UpdateRoblox
        {
            get => App.Settings.Prop.UpdateRoblox && !IsRobloxInstallationMissing;
            set => App.Settings.Prop.UpdateRoblox = value;
        }

        public bool ConfirmLaunches
        {
            get => App.Settings.Prop.ConfirmLaunches;
            set => App.Settings.Prop.ConfirmLaunches = value;
        }

        public bool ForceRobloxLanguage
        {
            get => App.Settings.Prop.ForceRobloxLanguage;
            set => App.Settings.Prop.ForceRobloxLanguage = value;
        }

        public bool BackgroundUpdates
        {
            get => App.Settings.Prop.BackgroundUpdatesEnabled;
            set => App.Settings.Prop.BackgroundUpdatesEnabled = value;
        }

        public CleanerOptions SelectedCleanUpMode
        {
            get => App.Settings.Prop.CleanerOptions;
            set => App.Settings.Prop.CleanerOptions = value;
        }

        public IEnumerable<CleanerOptions> CleanerOptions { get; } = CleanerOptionsEx.Selections;

        public CleanerOptions CleanerOption
        {
            get => App.Settings.Prop.CleanerOptions;
            set
            {
                App.Settings.Prop.CleanerOptions = value;
            }
        }

        private List<string> CleanerItems = App.Settings.Prop.CleanerDirectories;

        public bool CleanerLogs
        {
            get => CleanerItems.Contains("RobloxLogs");
            set
            {
                if (value)
                    CleanerItems.Add("RobloxLogs");
                else
                    CleanerItems.Remove("RobloxLogs"); // should we try catch it?
            }
        }

        public bool CleanerCache
        {
            get => CleanerItems.Contains("RobloxCache");
            set
            {
                if (value)
                    CleanerItems.Add("RobloxCache");
                else
                    CleanerItems.Remove("RobloxCache");
            }
        }

        public bool CleanerFishstrap
        {
            get => CleanerItems.Contains("FishstrapLogs");
            set
            {
                if (value)
                    CleanerItems.Add("FishstrapLogs");
                else
                    CleanerItems.Remove("FishstrapLogs");
            }
        }
    }
}
