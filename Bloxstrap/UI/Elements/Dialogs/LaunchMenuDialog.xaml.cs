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
using System.Windows.Shapes;
using Bloxstrap.UI.ViewModels.Dialogs;
using Bloxstrap.UI.ViewModels.Installer;
using Wpf.Ui.Mvvm.Interfaces;

namespace Bloxstrap.UI.Elements.Dialogs
{
    /// <summary>
    /// Interaction logic for LaunchMenuDialog.xaml
    /// </summary>
    public partial class LaunchMenuDialog
    {
        public NextAction CloseAction = NextAction.Terminate;

        public LaunchMenuDialog()
        {
            var viewModel = new LaunchMenuViewModel();
            viewModel.CloseWindowRequest += (_, closeAction) =>
            {
                CloseAction = closeAction;
                Close();
            };

            DataContext = viewModel;

            InitializeComponent();
            Random Chance = new();
            if (Chance.Next(0, 10000) == 1)
            {
                LaunchTitle.Text = "Cartistrap";
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Anchor_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CardAction_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Hyperlink_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }
    }
}