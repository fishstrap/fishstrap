using Bloxstrap.PcTweaks;
using System.Windows.Controls;
using System.Windows;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public partial class PCTweaksViewModel : NotifyPropertyChangedViewModel
    {
        public bool RobloxWiFiPriorityBoost
        {
            get => QosPolicies.IsPolicyEnabled();
            set
            {
                if (QosPolicies.IsPolicyEnabled() == value)
                    return;

                bool success = QosPolicies.TogglePolicy(value);
                if (success)
                {
                    OnPropertyChanged(nameof(RobloxWiFiPriorityBoost));
                }
            }
        }

        public bool DisableGameDVR
        {
            get => GameDvrToggle.IsGameDvrDisabled();
            set
            {
                if (GameDvrToggle.IsGameDvrDisabled() == value)
                    return;

                bool success = GameDvrToggle.ToggleGameDvr(value);
                if (success)
                    OnPropertyChanged(nameof(DisableGameDVR));
            }
        }

        public bool NetworkAdapterOptimizationEnabled
        {
            get => NetworkAdapterOptimization.IsNetworkOptimizationEnabled();
            set
            {
                if (NetworkAdapterOptimization.IsNetworkOptimizationEnabled() == value)
                    return;

                bool success = NetworkAdapterOptimization.ToggleNetworkOptimization(value);
                if (success)
                {
                    OnPropertyChanged(nameof(NetworkAdapterOptimizationEnabled));
                }
            }
        }

        public bool AllowRobloxFirewall
        {
            get => FirewallRules.IsFirewallRuleEnabled();
            set
            {
                if (FirewallRules.IsFirewallRuleEnabled() == value)
                    return;

                bool success = FirewallRules.ToggleFirewallRule(value);
                if (success)
                    OnPropertyChanged(nameof(AllowRobloxFirewall));
            }
        }

        public bool TelemetryDisabled
        {
            get => TelemetryTweaks.IsTelemetryDisabled();
            set
            {
                if (TelemetryTweaks.IsTelemetryDisabled() == value)
                    return;

                bool success = TelemetryTweaks.ToggleTelemetrySettings(value);
                if (success)
                {
                    OnPropertyChanged(nameof(TelemetryDisabled));
                }
            }
        }

        public bool DisableMitigations
        {
            get => PcTweaks.DisableMitigations.AreMitigationsDisabled();
            set
            {
                if (value != PcTweaks.DisableMitigations.AreMitigationsDisabled())
                {
                    _ = Task.Run(() =>
                    {
                        bool result = PcTweaks.DisableMitigations.TogglePolicy(value);
                        if (!result)
                        {
                            App.Current.Dispatcher.Invoke(() =>
                                OnPropertyChanged(nameof(DisableMitigations)));
                        }
                        else
                        {
                            App.Current.Dispatcher.Invoke(() =>
                                OnPropertyChanged(nameof(DisableMitigations)));
                        }
                    });
                }
            }
        }

        public bool DisableDefenderSmartScreen
        {
            get => PcTweaks.DisableDefenderSmartScreen.IsDisabled();
            set
            {
                if (value != PcTweaks.DisableDefenderSmartScreen.IsDisabled())
                {
                    _ = Task.Run(() =>
                    {
                        bool success = PcTweaks.DisableDefenderSmartScreen.ToggleDisableDefenderSmartScreen(value);
                        if (!success)
                        {
                            App.Current.Dispatcher.Invoke(() =>
                                OnPropertyChanged(nameof(DisableDefenderSmartScreen)));
                        }
                        else
                        {
                            App.Current.Dispatcher.Invoke(() =>
                                OnPropertyChanged(nameof(DisableDefenderSmartScreen)));
                        }
                    });
                }
            }
        }
    }
}