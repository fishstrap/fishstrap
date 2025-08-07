using Bloxstrap.UI.ViewModels.Dialogs;
using Bloxstrap.UI.ViewModels.Settings;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    public partial class CommunityPage : UiPage
    {
        public CommunityPage()
        {
            InitializeComponent();
            DataContext = new CommunityViewModel();
        }

        private void BrowseScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 20)
            {
                if (DataContext is CommunityViewModel vm && vm.LoadMoreCommand.CanExecute(null))
                {
                    vm.LoadMoreCommand.Execute(null);
                }
            }
        }
    }
}