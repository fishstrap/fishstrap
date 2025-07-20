using Bloxstrap.UI.ViewModels.Bootstrapper;
using Wpf.Ui.Mvvm.Interfaces;

namespace Bloxstrap.UI.Elements.Bootstrapper
{
    /// <summary>
    /// Interaction logic for ClassicFluentDialog.xaml
    /// </summary>
    public partial class ClassicFluentDialog
    {
        public ClassicFluentDialog()
            : base()
        {
            InitializeComponent();

            _viewModel = new ClassicFluentDialogViewModel(this);
            DataContext = _viewModel;
        }
    }
}