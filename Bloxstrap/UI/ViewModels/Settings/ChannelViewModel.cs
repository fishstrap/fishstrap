using Bloxstrap.AppData;
using Bloxstrap.RobloxInterfaces;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class ChannelViewModel : NotifyPropertyChangedViewModel
    {
        private string _oldPlayerVersionGuid = "";
        private string _oldStudioVersionGuid = "";

        public ChannelViewModel()
        {
            Task.Run(() => LoadChannelDeployInfo(App.Settings.Prop.Channel));
        }

        public bool UpdateCheckingEnabled
        {
            get => App.Settings.Prop.CheckForUpdates;
            set => App.Settings.Prop.CheckForUpdates = value;
        }

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
            // wouldnt it be better to check old version guids?
            // what about fresh installs?
            get => String.IsNullOrEmpty(App.State.Prop.Player.VersionGuid) && String.IsNullOrEmpty(App.State.Prop.Studio.VersionGuid);
            set
            {
                if (value)
                {
                    _oldPlayerVersionGuid = App.State.Prop.Player.VersionGuid;
                    _oldStudioVersionGuid = App.State.Prop.Studio.VersionGuid;
                    App.State.Prop.Player.VersionGuid = "";
                    App.State.Prop.Studio.VersionGuid = "";
                }
                else
                {
                    App.State.Prop.Player.VersionGuid = _oldPlayerVersionGuid;
                    App.State.Prop.Studio.VersionGuid = _oldStudioVersionGuid;
                }
            }
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


        private ProcessPriorityOption _selectedPriority;

        public IReadOnlyList<ProcessPriorityOption> ProcessPriorityOptions { get; } =
            Enum.GetValues(typeof(ProcessPriorityOption)).Cast<ProcessPriorityOption>().ToList();

        public ProcessPriorityOption SelectedPriority
        {
            get => _selectedPriority;
            set
            {
                if (_selectedPriority != value)
                {
                    _selectedPriority = value;
                    App.Settings.Prop.SelectedProcessPriority = value;
                    OnPropertyChanged(nameof(SelectedPriority));
                }
            }
        }
    }
}