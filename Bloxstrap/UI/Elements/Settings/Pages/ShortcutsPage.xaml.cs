using Bloxstrap.UI.ViewModels.Settings;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for ShortcutsPage.xaml
    /// </summary>
    public partial class ShortcutsPage
    {
        public ShortcutsPage()
        {
            InitializeComponent();
            DataContext = new ShortcutsViewModel();
            (App.Current as App)?._froststrapRPC?.UpdatePresence("Page: Shortcut");
        }
    }
}
