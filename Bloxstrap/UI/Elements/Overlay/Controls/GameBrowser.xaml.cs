using System.Windows.Controls;

using Bloxstrap.Integrations;
using Bloxstrap.UI.ViewModels.Overlay.Controls;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public partial class GameBrowser : UserControl
    {
        public GameBrowser()
        {
            DataContext = new GameBrowserViewModel(null);

            InitializeComponent();
        }

        public void Attach(ActivityWatcher? activityWatcher) => DataContext = new GameBrowserViewModel(activityWatcher);
    }
}
