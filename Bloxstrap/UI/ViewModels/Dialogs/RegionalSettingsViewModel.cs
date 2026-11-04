using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

namespace Bloxstrap.UI.ViewModels.Dialogs
{
    internal class RegionalSettingsViewModel
    {
        public event EventHandler? CloseRequestEvent;

        public ICommand SetRegionalSettingsCommand => new RelayCommand(SetRegionalSettings);

        public static List<string> ClientDistributions => Distributions.GetDistributions();
        public static List<string> Languages => Locale.GetLanguages();

        public string SelectedDistribution { get; set; } = Distributions.ClientDistributions[App.Settings.Prop.DistributorType];
        public string SelectedLanguage { get; set; } = Locale.SupportedLocales[App.Settings.Prop.Locale];

        private void SetRegionalSettings()
        {
            string identifier = Locale.GetIdentifierFromName(SelectedLanguage);
            DistributorType distributor = Distributions.GetDistributionFromName(SelectedDistribution);

            Locale.Set(identifier);
            App.Settings.Prop.Locale = identifier;

            if (distributor == DistributorType.VNGGames && identifier != "vi")
            {
                var result = Frontend.ShowMessageBox(
                    Strings.Dialog_DistributorSelector_VNGWarning,
                    System.Windows.MessageBoxImage.Warning,
                    System.Windows.MessageBoxButton.OKCancel
                    );

                if (result == System.Windows.MessageBoxResult.Cancel)
                    return;
            }


            Distributions.Set(distributor);

            CloseRequestEvent?.Invoke(this, new());
        }
    }
}
