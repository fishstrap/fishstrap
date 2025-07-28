using Bloxstrap.AppData;
using Bloxstrap.Enums;
using Bloxstrap.Integrations;
using Bloxstrap.RobloxInterfaces;
using System.Collections.ObjectModel;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class BehaviourViewModel : NotifyPropertyChangedViewModel
    {

        public BehaviourViewModel()
        {

            foreach (var entry in RobloxIconEx.Selections)
                RobloxIcons.Add(new RobloxIconEntry { IconType = (RobloxIcon)entry });
        }

        public bool MultiInstances
        {
            get => App.Settings.Prop.MultiInstanceLaunching;
            set
            {
                App.Settings.Prop.MultiInstanceLaunching = value;
                App.FastFlags.SetPreset("Instances.WndCheck", value ? "0" : null);
            }
        }

        public bool BackgroundUpdates
        {
            get => App.Settings.Prop.BackgroundUpdatesEnabled;
            set => App.Settings.Prop.BackgroundUpdatesEnabled = value;
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

        public IEnumerable<RobloxIcon> RobloxIcon { get; } = Enum.GetValues(typeof(RobloxIcon)).Cast<RobloxIcon>();

        public RobloxIcon SelectedRobloxIcon
        {
            get => App.Settings.Prop.SelectedRobloxIcon;
            set
            {
                if (App.Settings.Prop.SelectedRobloxIcon != value)
                {
                    App.Settings.Prop.SelectedRobloxIcon = value;
                    App.Settings.Save();
                    OnPropertyChanged(nameof(SelectedRobloxIcon));
                }
            }
        }

        public ObservableCollection<RobloxIconEntry> RobloxIcons { get; set; } = new();

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

        public bool CleanerFroststrap
        {
            get => CleanerItems.Contains("FroststrapLogs");
            set
            {
                if (value)
                    CleanerItems.Add("FroststrapLogs");
                else
                    CleanerItems.Remove("FroststrapLogs");
            }
        }
    }
}
