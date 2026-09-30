using System.Windows.Controls;

using Bloxstrap.Integrations;
using Bloxstrap.UI.ViewModels.Overlay.Controls;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    /// <summary>
    /// Interaction logic for ServerBrowser.xaml
    /// </summary>
    public partial class ServerBrowser : UserControl
    {
        private ServerBrowserViewModel _viewModel;

        private RecentServersViewModel? _recentViewModel;

        public ServerBrowser()
        {
            _viewModel = new ServerBrowserViewModel(null);

            DataContext = _viewModel;

            InitializeComponent();

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
    }
}
