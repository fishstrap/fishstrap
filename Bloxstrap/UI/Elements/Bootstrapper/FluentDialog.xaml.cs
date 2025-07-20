using Bloxstrap.UI.ViewModels.Bootstrapper;
using Wpf.Ui.Mvvm.Interfaces;

namespace Bloxstrap.UI.Elements.Bootstrapper
{
    /// <summary>
    /// Interaction logic for FluentDialog.xaml
    /// </summary>
    public partial class FluentDialog
    {
        public FluentDialog(bool aero)
            : base()
        {
            InitializeComponent();

            string version = Utilities.GetRobloxVersionStr(false);
            string channel = App.Settings.Prop.Channel;

            _viewModel = new FluentDialogViewModel(this, aero, version, channel);
            DataContext = _viewModel;

            // setting this to true for mica results in the window being undraggable
            if (aero)
                AllowsTransparency = true;
        }
    }
}