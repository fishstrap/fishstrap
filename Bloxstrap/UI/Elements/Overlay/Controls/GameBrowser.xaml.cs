using System.Windows.Controls;

using Bloxstrap.Integrations;
using Bloxstrap.UI.ViewModels.Overlay.Controls;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public partial class GameBrowser : UserControl
    {
        private bool _attached;

        public GameBrowser()
        {
            DataContext = new GameBrowserViewModel(null);

            InitializeComponent();

            IsVisibleChanged += (_, _) => LoadContinue();
        }

        public void Attach(ActivityWatcher? activityWatcher)
        {
            DataContext = new GameBrowserViewModel(activityWatcher);

            _attached = true;

            LoadContinue();
        }

        private void LoadContinue()
        {
            if (_attached && IsVisible && DataContext is GameBrowserViewModel viewModel)
                _ = viewModel.LoadContinueAsync();
        }
    }
}
