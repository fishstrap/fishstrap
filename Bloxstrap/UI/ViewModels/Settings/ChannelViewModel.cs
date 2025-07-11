using Bloxstrap.AppData;
using Bloxstrap.RobloxInterfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class ChannelViewModel : NotifyPropertyChangedViewModel, INotifyPropertyChanged
    {
        public ChannelViewModel()
        {
            Task.Run(() => LoadChannelDeployInfo(App.Settings.Prop.Channel));
            _selectedPriority = App.Settings.Prop.SelectedProcessPriority;
        }

        public bool UpdateCheckingEnabled
        {
            get => App.Settings.Prop.CheckForUpdates;
            set => App.Settings.Prop.CheckForUpdates = value;
        }

        public bool IsRobloxInstallationMissing => String.IsNullOrEmpty(App.RobloxState.Prop.Player.VersionGuid) && String.IsNullOrEmpty(App.RobloxState.Prop.Studio.VersionGuid);

        private async Task LoadChannelDeployInfo(string channel)
        {
            ShowLoadingError = false;
            OnPropertyChanged(nameof(ShowLoadingError));

            ChannelInfoLoadingText = Strings.Menu_Channel_Switcher_Fetching;
            OnPropertyChanged(nameof(ChannelInfoLoadingText));

            ChannelDeployInfo = null;
            OnPropertyChanged(nameof(ChannelDeployInfo));

            try
            {
                ClientVersion info = await Deployment.GetInfo(channel);

                ShowChannelWarning = info.IsBehindDefaultChannel;
                OnPropertyChanged(nameof(ShowChannelWarning));

                ChannelDeployInfo = new DeployInfo
                {
                    Version = info.Version,
                    VersionGuid = info.VersionGuid
                };

                App.State.Prop.IgnoreOutdatedChannel = true;

                OnPropertyChanged(nameof(ChannelDeployInfo));
            }
            catch (InvalidChannelException ex)
            {
                ShowLoadingError = true;
                OnPropertyChanged(nameof(ShowLoadingError));

                if (ex.StatusCode == HttpStatusCode.Unauthorized)
                    ChannelInfoLoadingText = Strings.Menu_Channel_Switcher_Unauthorized;
                else
                    ChannelInfoLoadingText = $"An http error has occured ({ex.StatusCode})";

                OnPropertyChanged(nameof(ChannelInfoLoadingText));
            }
        }

        public bool ShowLoadingError { get; set; } = false;
        public bool ShowChannelWarning { get; set; } = false;

        public DeployInfo? ChannelDeployInfo { get; private set; } = null;
        public string ChannelInfoLoadingText { get; private set; } = null!;

        public string ViewChannel
        {
            get => App.Settings.Prop.Channel;
            set
            {
                value = value.Trim();
                Task.Run(() => LoadChannelDeployInfo(value));

                if (value.ToLower() == "live" || value.ToLower() == "zlive")
                {
                    App.Settings.Prop.Channel = Deployment.DefaultChannel;
                }
                else
                {
                    App.Settings.Prop.Channel = value;
                }
            }
        }

        public string ChannelHash
        {
            get => App.Settings.Prop.ChannelHash;
            set
            {
                const string VersionHashFormat = "version-(.*)";
                Match match = Regex.Match(value, VersionHashFormat);
                if (match.Success || String.IsNullOrEmpty(value))
                {
                    App.Settings.Prop.ChannelHash = value;
                }
            }
        }

        public bool UpdateRoblox
        {
            get => App.Settings.Prop.UpdateRoblox;
            set => App.Settings.Prop.UpdateRoblox = value;
        }

        public IReadOnlyDictionary<string, ChannelChangeMode> ChannelChangeModes => new Dictionary<string, ChannelChangeMode>
        {
            { Strings.Menu_Channel_ChangeAction_Automatic, ChannelChangeMode.Automatic },
            { Strings.Menu_Channel_ChangeAction_Prompt, ChannelChangeMode.Prompt },
            { Strings.Menu_Channel_ChangeAction_Ignore, ChannelChangeMode.Ignore },
        };

        public string SelectedChannelChangeMode
        {
            get => ChannelChangeModes.FirstOrDefault(x => x.Value == App.Settings.Prop.ChannelChangeMode).Key;
            set => App.Settings.Prop.ChannelChangeMode = ChannelChangeModes[value];
        }

        public bool ForceRobloxReinstallation
        {
            get => App.State.Prop.ForceReinstall || IsRobloxInstallationMissing;
            set => App.State.Prop.ForceReinstall = value;
        }

        public bool DisableAnimations
        {
            get => App.Settings.Prop.DisableAnimations;
            set => App.Settings.Prop.DisableAnimations = value;
        }

        public bool HardwareAcceleration
        {
            get => App.Settings.Prop.WPFSoftwareRender;
            set => App.Settings.Prop.WPFSoftwareRender = value;
        }

        public ObservableCollection<ProcessPriorityOption> ProcessPriorityOptions { get; } =
            new ObservableCollection<ProcessPriorityOption>(Enum.GetValues(typeof(ProcessPriorityOption)).Cast<ProcessPriorityOption>());

        private ProcessPriorityOption _selectedPriority;
        public ProcessPriorityOption SelectedPriority
        {
            get => _selectedPriority;
            set
            {
                if (_selectedPriority == value)
                    return;

                _selectedPriority = value;
                App.Settings.Prop.SelectedProcessPriority = value;
                App.Settings.Save();
                OnPropertyChanged(nameof(SelectedPriority));
            }
        }
    }
}