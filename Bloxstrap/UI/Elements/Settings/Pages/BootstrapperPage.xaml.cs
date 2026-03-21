using Bloxstrap.Models.APIs.Fishstrap;
using Bloxstrap.UI.ViewModels.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for BehaviourPage.xaml
    /// </summary>

    public partial class BehaviourPage
    {
        private BehaviourViewModel _viewModel = new BehaviourViewModel();

        public BehaviourPage()
        {
            _viewModel = new BehaviourViewModel();

            this.DataContext = _viewModel;
            InitializeComponent();
        }

        public async void OnCanaryDownloaderLoaded(object sender, RoutedEventArgs e)
        {
            if (App.Settings.Prop.DeveloperMode)
                await _viewModel.GetCanaryBuilds();
        }
    }
}
