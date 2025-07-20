using Bloxstrap.UI.ViewModels.Bootstrapper;
using Wpf.Ui.Mvvm.Interfaces;

namespace Bloxstrap.UI.Elements.Bootstrapper
{
    /// <summary>
    /// Interaction logic for FluentDialog.xaml
    /// </summary>
    public partial class CustomFluentDialog
    {
        public CustomFluentDialog()
            : base()
        {
            InitializeComponent();

            _viewModel = new CustomFluentDialogViewModel(this);
            DataContext = _viewModel;
        }
    }
}