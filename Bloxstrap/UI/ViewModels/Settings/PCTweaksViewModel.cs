using Bloxstrap.Integrations;
using Bloxstrap.PcTweaks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.IO;
using System.Windows.Input;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public partial class PCTweaksViewModel : NotifyPropertyChangedViewModel
    {
        public bool RobloxWiFiPriorityBoost
        {
            get => App.Settings.Prop.RobloxWiFiPriorityBoost;
            set
            {
                if (App.Settings.Prop.RobloxWiFiPriorityBoost == value) return;

                bool success = QosPolicies.TogglePolicy(value);
                if (success)
                {
                    App.Settings.Prop.RobloxWiFiPriorityBoost = value;
                    OnPropertyChanged(nameof(RobloxWiFiPriorityBoost));
                }
                else
                {

                }
            }
        }

        public bool UltraPerformanceMode
        {
            get => App.Settings.Prop.EnableUltraPerformanceMode;
            set
            {
                if (App.Settings.Prop.EnableUltraPerformanceMode == value)
                    return;

                bool success = PcTweaks.UltraPerformanceMode.TogglePerformanceMode(value);
                if (success)
                {
                    App.Settings.Prop.EnableUltraPerformanceMode = value;
                    OnPropertyChanged(nameof(UltraPerformanceMode));
                }
            }
        }

        public bool DisableGameDvr
        {
            get => !App.Settings.Prop.GameDvrEnabled;
            set
            {
                bool newEnabledState = !value;
                if (App.Settings.Prop.GameDvrEnabled == newEnabledState)
                    return;

                bool success = GameDvrToggle.ToggleGameDvr(newEnabledState);
                if (success)
                {
                    App.Settings.Prop.GameDvrEnabled = newEnabledState;
                    OnPropertyChanged(nameof(DisableGameDvr));
                }
            }
        }

        public bool NetworkAdapterOptimizationEnabled
        {
            get => App.Settings.Prop.NetworkAdapterOptimizationEnabled;
            set
            {
                if (App.Settings.Prop.NetworkAdapterOptimizationEnabled == value)
                    return;

                bool success = NetworkAdapterOptimization.ToggleNetworkOptimization(value);
                if (success)
                {
                    App.Settings.Prop.NetworkAdapterOptimizationEnabled = value;
                    OnPropertyChanged(nameof(NetworkAdapterOptimizationEnabled));
                }
            }
        }

        public bool AllowRobloxFirewall
        {
            get => App.Settings.Prop.AllowRobloxFirewall;
            set
            {
                if (App.Settings.Prop.AllowRobloxFirewall == value)
                    return;

                bool success = FirewallRules.ToggleFirewallRule(value);
                if (success)
                {
                    App.Settings.Prop.AllowRobloxFirewall = value;
                    OnPropertyChanged(nameof(AllowRobloxFirewall));
                }
            }
        }

    }
}