using System.Windows;
using System.Windows.Controls;

using Bloxstrap.Integrations;
using Bloxstrap.UI.ViewModels.Overlay.Controls;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public partial class ServerBrowser : UserControl
    {
        private ServerBrowserViewModel _viewModel;

        private RecentServersViewModel? _recentViewModel;

        public ServerBrowser()
        {
            _viewModel = new ServerBrowserViewModel(null);

            DataContext = _viewModel;

            InitializeComponent();

            PublicTab.Checked += (_, _) => Frost.Source = PublicList;
            PrivateTab.Checked += (_, _) => Frost.Source = PrivateView;
            RecentTab.Checked += (_, _) => Frost.Source = RecentList;

            PublicView.IsVisibleChanged += (_, _) => _viewModel.SetVisible(PublicView.IsVisible);

            RecentView.IsVisibleChanged += async (_, _) =>
            {
                if (RecentView.IsVisible && _recentViewModel is not null)
                    await _recentViewModel.ReloadAsync();
            };
        }

        public void Attach(ActivityWatcher? activityWatcher)
        {
            _viewModel.SetVisible(false);

            _viewModel = new ServerBrowserViewModel(activityWatcher);

            DataContext = _viewModel;

            _viewModel.SetVisible(PublicView.IsVisible);

            _ = _viewModel.InitialiseAsync();

            _recentViewModel = new RecentServersViewModel(activityWatcher, _viewModel);
            RecentView.DataContext = _recentViewModel;

            PrivateView.Attach(activityWatcher);
        }

        private void RoValraCreditClicked(object sender, RoutedEventArgs e)
        {
            var site = new Uri("https://www.rovalra.com/");

            if (Window.GetWindow(this) is GameOverlay overlay)
                overlay.OpenPage(site);
            else
                Utilities.ShellExecute(site.AbsoluteUri);
        }
    }
}
