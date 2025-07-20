using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace Bloxstrap.UI.ViewModels.Bootstrapper
{
    public class CustomFluentDialogViewModel : BootstrapperDialogViewModel
    {
        public SolidColorBrush BackgroundColourBrush { get; set; } = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

        public CustomFluentDialogViewModel(IBootstrapperDialog dialog) : base(dialog)
        {
        }
    }
}
