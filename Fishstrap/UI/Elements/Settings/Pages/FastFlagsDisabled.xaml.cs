using Fishstrap.UI.ViewModels.Settings;

using System.Windows.Controls;

namespace Fishstrap.UI.Elements.Settings.Pages
{
    public partial class FastFlagsDisabled
    {
        public FastFlagsDisabled()
        {
            DataContext = new FastFlagsDisabledViewModel(this);
            InitializeComponent();
        }
    }
}