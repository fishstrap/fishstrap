using System;
using System.Windows.Controls;
using Wpf.Ui.Controls.Interfaces;
using Wpf.Ui.Mvvm.Contracts;
using Bloxstrap.UI.Elements.Help.Pages; // Make sure to include your pages

namespace Bloxstrap.UI.Elements.Help
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INavigationWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            App.Logger.WriteLine("MainWindow", "Initializing about window");

            Navigate(typeof(InfoPage));
        }

        #region INavigationWindow methods

        public Frame GetFrame() => RootFrame;

        public INavigation GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(IPageService pageService) => RootNavigation.PageService = pageService;

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods
    }
}