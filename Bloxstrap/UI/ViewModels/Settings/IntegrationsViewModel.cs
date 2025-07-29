using System.Collections.ObjectModel;
using System.Windows.Input;

using Microsoft.Win32;

using CommunityToolkit.Mvvm.Input;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class IntegrationsViewModel : NotifyPropertyChangedViewModel
    {
        public ICommand AddIntegrationCommand => new RelayCommand(AddIntegration);

        public ICommand DeleteIntegrationCommand => new RelayCommand(DeleteIntegration);

        public ICommand BrowseIntegrationLocationCommand => new RelayCommand(BrowseIntegrationLocation);

        private void AddIntegration()
        {
            CustomIntegrations.Add(new CustomIntegration()
            {
                Name = Strings.Menu_Integrations_Custom_NewIntegration
            });

            SelectedCustomIntegrationIndex = CustomIntegrations.Count - 1;

            OnPropertyChanged(nameof(SelectedCustomIntegrationIndex));
            OnPropertyChanged(nameof(IsCustomIntegrationSelected));
        }

        private void DeleteIntegration()
        {
            if (SelectedCustomIntegration is null)
                return;

            CustomIntegrations.Remove(SelectedCustomIntegration);

            if (CustomIntegrations.Count > 0)
            {
                SelectedCustomIntegrationIndex = CustomIntegrations.Count - 1;
                OnPropertyChanged(nameof(SelectedCustomIntegrationIndex));
            }

            OnPropertyChanged(nameof(IsCustomIntegrationSelected));
        }

        private void BrowseIntegrationLocation()
        {
            if (SelectedCustomIntegration is null)
                return;

            var dialog = new OpenFileDialog
            {
                Filter = $"{Strings.Menu_AllFiles}|*.*"
            };

            if (dialog.ShowDialog() != true)
                return;

            SelectedCustomIntegration.Name = dialog.SafeFileName;
            SelectedCustomIntegration.Location = dialog.FileName;
            OnPropertyChanged(nameof(SelectedCustomIntegration));
        }

        public bool ActivityTrackingEnabled
        {
            get => App.Settings.Prop.EnableActivityTracking;
            set
            {
                App.Settings.Prop.EnableActivityTracking = value;
                App.FastFlags.SetPreset("Flog.Network", value ? "7" : null);

                if (!value)
                {
                    ShowServerDetailsEnabled = false;
                    ShowGameHistory = false;
                    DisableAppPatchEnabled = false;
                    DiscordActivityEnabled = false;
                    DiscordActivityJoinEnabled = false;

                    OnPropertyChanged(nameof(ShowServerDetailsEnabled));
                    OnPropertyChanged(nameof(ShowGameHistory));
                    OnPropertyChanged(nameof(DisableAppPatchEnabled));
                    OnPropertyChanged(nameof(DiscordActivityEnabled));
                    OnPropertyChanged(nameof(DiscordActivityJoinEnabled));
                }

                OnPropertyChanged(nameof(ActivityTrackingEnabled));
            }
        }

        public bool ShowServerDetailsEnabled
        {
            get => App.Settings.Prop.ShowServerDetails;
            set => App.Settings.Prop.ShowServerDetails = value;
        }

        public bool ShowGameHistory
        {
            get => App.Settings.Prop.ShowGameHistoryMenu;
            set => App.Settings.Prop.ShowGameHistoryMenu = value;
        }

        public bool PlayerLogsEnabled
        {
            get => App.FastFlags.GetPreset("Players.LogLevel") == "trace"; // we r using this to determine if its enabled
            set
            {
                App.FastFlags.SetPreset("Players.LogLevel", value ? "trace" : null);
                App.FastFlags.SetPreset("Players.LogPattern", value ? "ExpChat/mountClientApp" : null);
            }
        }

        public bool DiscordActivityEnabled
        {
            get => App.Settings.Prop.UseDiscordRichPresence;
            set
            {
                App.Settings.Prop.UseDiscordRichPresence = value;

                if (!value)
                {
                    DiscordActivityJoinEnabled = value;
                    EnableCustomStatusDisplay = value;
                    DiscordAccountOnProfile = value;
                    OnPropertyChanged(nameof(DiscordActivityJoinEnabled));
                    OnPropertyChanged(nameof(EnableCustomStatusDisplay));
                    OnPropertyChanged(nameof(DiscordAccountOnProfile));
                }
            }
        }

        public bool ShowUsingFroststrapRPC
        {
            get => App.Settings.Prop.ShowUsingFroststrapRPC;
            set => App.Settings.Prop.ShowUsingFroststrapRPC = value;
        }

        public bool DiscordActivityJoinEnabled
        {
            get => !App.Settings.Prop.HideRPCButtons;
            set => App.Settings.Prop.HideRPCButtons = !value;
        }

        public bool EnableCustomStatusDisplay
        {
            get => App.Settings.Prop.EnableCustomStatusDisplay;
            set => App.Settings.Prop.EnableCustomStatusDisplay = value;
        }

        public bool DiscordAccountOnProfile
        {
            get => App.Settings.Prop.ShowAccountOnRichPresence;
            set => App.Settings.Prop.ShowAccountOnRichPresence = value;
        }

        public bool DisableAppPatchEnabled
        {
            get => App.Settings.Prop.UseDisableAppPatch;
            set => App.Settings.Prop.UseDisableAppPatch = value;
        }

        public bool BlockRobloxRecording
        {
            get => App.Settings.Prop.BlockRobloxRecording;
            set
            {
                if (App.Settings.Prop.BlockRobloxRecording != value)
                {
                    Watcher.ApplyRecordingBlock(value, saveSetting: true);
                    OnPropertyChanged(nameof(BlockRobloxRecording));
                }
            }
        }

        public bool BlockRobloxScreenshots
        {
            get => App.Settings.Prop.BlockRobloxScreenshots;
            set
            {
                if (App.Settings.Prop.BlockRobloxScreenshots != value)
                {
                    Watcher.ApplyScreenshotBlock(value, saveSetting: true);
                    OnPropertyChanged(nameof(BlockRobloxScreenshots));
                }
            }
        }

        public ObservableCollection<CustomIntegration> CustomIntegrations
        {
            get => App.Settings.Prop.CustomIntegrations;
            set => App.Settings.Prop.CustomIntegrations = value;
        }

        public CustomIntegration? SelectedCustomIntegration { get; set; }
        public int SelectedCustomIntegrationIndex { get; set; }
        public bool IsCustomIntegrationSelected => SelectedCustomIntegration is not null;
    }
}
