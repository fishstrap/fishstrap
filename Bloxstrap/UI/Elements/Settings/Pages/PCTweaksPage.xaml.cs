using Bloxstrap.UI.ViewModels.Settings;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Bloxstrap.PcTweaks;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for PCTweaksPage.xaml
    /// </summary>
    public partial class PCTweaksPage
    {
        public PCTweaksPage()
        {
            DataContext = new PCTweaksViewModel();
            InitializeComponent();
            (App.Current as App)?._froststrapRPC?.UpdatePresence("Page: PC Tweaks");
        }

        private async void BtnImportMaxFPS_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            button.IsEnabled = false;

            bool success = PowerPlanImporter.ImportAndActivatePowerPlan("FroststrapMaximumFPS.pow", out string message);

            if (success)
            {
                button.Content = "Applied";
                await Task.Delay(3000);
                button.Content = "Apply";
            }
            else
            {
                Frontend.ShowMessageBox(
                    message,
                    MessageBoxImage.Error,
                    MessageBoxButton.OK
                );
            }

            button.IsEnabled = true;
        }

        private async void BtnImportLowLatency_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            button.IsEnabled = false;

            bool success = PowerPlanImporter.ImportAndActivatePowerPlan("FroststrapLowLatency.pow", out string message);

            if (success)
            {
                button.Content = "Applied";
                await Task.Delay(3000);
                button.Content = "Apply";
            }
            else
            {
                Frontend.ShowMessageBox(
                    message,
                    MessageBoxImage.Error,
                    MessageBoxButton.OK
                );
            }

            button.IsEnabled = true;
        }

        private void EasterEggButton_Click(object sender, RoutedEventArgs e)
        {
            var navService = NavigationService.GetNavigationService(this);
            navService?.Navigate(new BloxstrapPage());
        }
    }
}
