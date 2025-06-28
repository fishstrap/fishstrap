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

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for PCTweaksPage.xaml
    /// </summary>
    public partial class PCTweaksPage
    {
        public PCTweaksPage()
        {
            DataContext = new ViewModels.Settings.PCTweaksViewModel();
            InitializeComponent();
        }

        private void EasterEggButton_Click(object sender, RoutedEventArgs e)
        {
            var navService = NavigationService.GetNavigationService(this);
            navService?.Navigate(new BloxstrapPage());
        }
    }
}
