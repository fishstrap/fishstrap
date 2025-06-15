using Bloxstrap.Integrations;
using Bloxstrap.UI.ViewModels.ContextMenu;

namespace Bloxstrap.UI.Elements.ContextMenu
{
    /// <summary>
    /// Interaction logic for Logs.xaml
    /// </summary>
    public partial class Logs
    {
        public Logs(ActivityWatcher watcher)
        {
            InitializeComponent();

            var viewModel = new LogsViewModel(watcher);
            viewModel.RequestCloseEvent += (_, _) => Close();

            DataContext = viewModel;
        }
    }
}
