using Bloxstrap.UI.Elements.Controls;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Wpf.Ui.Common.Interfaces;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class IntegrationsViewModel : NotifyPropertyChangedViewModel, INavigationAware
    {

        public bool VulkanFullscreenAllowed => App.Settings.Prop.EnableWindowManipulation && (App.FastFlags.GetPreset("Rendering.Mode.Vulkan") ?? "False").Equals("True", StringComparison.OrdinalIgnoreCase);

        public bool EnableOverlay
        {
            get => App.Settings.Prop.EnableOverlay;
            set => App.Settings.Prop.EnableOverlay = value;
        }

        public bool EnableRobloxBackgroundApp
        {
            get => App.Settings.Prop.EnableRobloxBackgroundApp;
            set => App.Settings.Prop.EnableRobloxBackgroundApp = value;
        }

        public IReadOnlyCollection<RobloxTheme> RobloxThemes { get; } = RobloxThemeEx.Selections;

        public RobloxTheme RobloxTheme
        {
            get => App.Settings.Prop.RobloxTheme;
            set => App.Settings.Prop.RobloxTheme = value;
        }

        public ICommand AddIntegrationCommand => new RelayCommand(AddIntegration);

        public ICommand DeleteIntegrationCommand => new RelayCommand(DeleteIntegration);

        public ICommand BrowseIntegrationLocationCommand => new RelayCommand(BrowseIntegrationLocation);

        public void OnNavigatedTo()
        {
            OnPropertyChanged(nameof(VulkanFullscreenAllowed));
            OnPropertyChanged(nameof(EnableFakeBorderlessFullscreen));
        } // vulkan is updated on a different page so we do this

        public void OnNavigatedFrom() { }

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

                if (!value)
                {
                    ShowServerDetailsEnabled = value;
                    DisableAppPatchEnabled = value;
                    DiscordActivityEnabled = value;
                    DiscordActivityJoinEnabled = value;

                    OnPropertyChanged(nameof(ShowServerDetailsEnabled));
                    OnPropertyChanged(nameof(DisableAppPatchEnabled));
                    OnPropertyChanged(nameof(DiscordActivityEnabled));
                    OnPropertyChanged(nameof(DiscordActivityJoinEnabled));
                }
            }
        }

        public bool ShowServerDetailsEnabled
        {
            get => App.Settings.Prop.ShowServerDetails;
            set => App.Settings.Prop.ShowServerDetails = value;
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
                    DiscordAccountOnProfile = value;
                    OnPropertyChanged(nameof(DiscordActivityJoinEnabled));
                    OnPropertyChanged(nameof(DiscordAccountOnProfile));
                }
            }
        }
        public IReadOnlyList<DiscordRPCStatusDisplay> DiscordActivityStatusDisplayTypes => DiscordRPCStatusDisplayEx.Selections;

        public DiscordRPCStatusDisplay DiscordActivityStatusDisplayType
        {
            get => App.Settings.Prop.RichPresenceStatusDisplayType;
            set => App.Settings.Prop.RichPresenceStatusDisplayType = value;
        }

        public bool DiscordActivityJoinEnabled
        {
            get => !App.Settings.Prop.HideRPCButtons;
            set => App.Settings.Prop.HideRPCButtons = !value;
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

        public ObservableCollection<CustomIntegration> CustomIntegrations
        {
            get => App.Settings.Prop.CustomIntegrations;
            set => App.Settings.Prop.CustomIntegrations = value;
        }

        public bool EnableWindowManipulation
        {
            get => App.Settings.Prop.EnableWindowManipulation;
            set
            {
                App.Settings.Prop.EnableWindowManipulation = value;

                if (!value)
                    EnableFakeBorderlessFullscreen = false;

                OnPropertyChanged(nameof(VulkanFullscreenAllowed));
                OnPropertyChanged(nameof(EnableFakeBorderlessFullscreen));
            }
        }

        public bool EnableFakeBorderlessFullscreen
        {
            get => App.Settings.Prop.FakeBorderlessFullscreen;
            set => App.Settings.Prop.FakeBorderlessFullscreen = VulkanFullscreenAllowed && value;
        }

        public CustomIntegration? SelectedCustomIntegration { get; set; }
        public int SelectedCustomIntegrationIndex { get; set; }
        public bool IsCustomIntegrationSelected => SelectedCustomIntegration is not null;
    }
}
