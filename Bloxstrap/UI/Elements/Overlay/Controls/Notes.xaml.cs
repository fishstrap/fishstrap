using System.Windows.Controls;

using Bloxstrap.UI.ViewModels.Overlay.Controls;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public partial class Notes : UserControl
    {
        private readonly NotesViewModel _viewModel = new();

        public Notes()
        {
            DataContext = _viewModel;

            InitializeComponent();
        }

        public void Flush() => _viewModel.Save();
    }
}
