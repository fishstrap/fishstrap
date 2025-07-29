using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Bloxstrap.UI.ViewModels.Settings;
using Wpf.Ui.Hardware;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for ChannelPage.xaml
    /// </summary>
    public partial class ChannelPage
    {
        public ChannelPage()
        {
            DataContext = new ChannelViewModel();
            InitializeComponent();
            (App.Current as App)?._froststrapRPC?.UpdatePresence("Page: Settings");
        }

        private void ToggleSwitch_Checked_1(object sender, RoutedEventArgs e)
        {
            HardwareAcceleration.MemoryTrimming();
        }

        private void ToggleSwitch_Unchecked_1(object sender, RoutedEventArgs e)
        {
            Frontend.ShowMessageBox(
            Strings.Menu_Channels_HardwareAccelRestart,
            MessageBoxImage.Information
            );
        }

        private void ToggleSwitch_Checked_2(object sender, RoutedEventArgs e)
        {
            HardwareAcceleration.DisableAllAnimations();
        }

        private void ToggleSwitch_Unchecked_2(object sender, RoutedEventArgs e)
        {
            Frontend.ShowMessageBox(
            Strings.Menu_Channels_DisableAnimationRestart,
            MessageBoxImage.Information
            );
        }
    }
}