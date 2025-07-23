using Bloxstrap.UI.Elements.Base;
using Bloxstrap.UI.ViewModels.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class PublicFlaglistsDialog : WpfUiWindow
    {
        public PublicFlaglistsDialog()
        {
            InitializeComponent();
            DataContext = new PublicFlaglistsViewModel();
        }

        private void BrowseScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 20) // 20px tolerance
            {
                if (DataContext is PublicFlaglistsViewModel vm && vm.LoadMoreCommand.CanExecute(null))
                {
                    vm.LoadMoreCommand.Execute(null);
                }
            }
        }
    }
}