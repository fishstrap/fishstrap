using System.Windows.Controls;
using Wpf.Ui.Mvvm.Contracts;
using Wpf.Ui.Controls.Interfaces;
using Bloxstrap.UI.Elements.Base;


namespace Bloxstrap.UI.Elements.ClickerGame
{
    public partial class MainWindow : INavigationWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            (App.Current as App)?._froststrapRPC?.UpdatePresence("Dialog: Easter Egg Area");
        }

        #region INavigationWindow

        public Frame GetFrame() => ContentFrame;

        public INavigation GetNavigation() => NavigationStore;

        public bool Navigate(System.Type pageType) => NavigationStore.Navigate(pageType);

        public void SetPageService(IPageService pageService) => NavigationStore.PageService = pageService;

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion
    }
}
