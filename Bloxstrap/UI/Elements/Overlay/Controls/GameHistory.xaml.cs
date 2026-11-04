using System.Windows.Controls;

using Bloxstrap.Integrations;
using Bloxstrap.UI.ViewModels.ContextMenu;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public partial class GameHistory : UserControl
    {
        public GameHistory()
        {
            InitializeComponent();
        }

        public void Attach(ActivityWatcher? activityWatcher)
        {
            if (activityWatcher is not null)
                DataContext = new ServerHistoryViewModel(activityWatcher);
        }
    }
}
