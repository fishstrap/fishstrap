using Bloxstrap.UI.ViewModels.Help;
using System.Windows;
using System.Windows.Controls;

namespace Bloxstrap.UI.Elements.Help.Pages
{
    public partial class InfoPage
    {
        public InfoPage()
        {
            DataContext = new InfoViewModel();
            InitializeComponent();
        }
    }
}