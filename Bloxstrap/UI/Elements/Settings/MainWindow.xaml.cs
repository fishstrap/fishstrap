using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Win32;
using System.Windows.Media;
using System.IO;
using System.Collections.Generic;
using Bloxstrap;

using Wpf.Ui.Controls;
using Wpf.Ui.Controls.Interfaces;
using Wpf.Ui.Mvvm.Contracts;

using Bloxstrap.UI.ViewModels.Settings;
using Wpf.Ui.Common;
using System.Windows.Media.Animation;

namespace Bloxstrap.UI.Elements.Settings
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INavigationWindow
    {
        private Models.Persistable.WindowState _state => App.State.Prop.SettingsWindow;

        public static ObservableCollection<NavigationItem> MainNavigationItems { get; } = new ObservableCollection<NavigationItem>();
        public static ObservableCollection<NavigationItem> FooterNavigationItems { get; } = new ObservableCollection<NavigationItem>();

        public static List<string> DefaultNavigationOrder { get; private set; } = new();
        public static List<string> DefaultFooterOrder { get; private set; } = new();


        public MainWindow(bool showAlreadyRunningWarning)
        {
            var viewModel = new MainWindowViewModel();

            viewModel.RequestSaveNoticeEvent += (_, _) => SettingsSavedSnackbar.Show();
            viewModel.RequestCloseWindowEvent += (_, _) => Close();

            DataContext = viewModel;

            InitializeComponent();

            App.Logger.WriteLine("MainWindow", "Initializing settings window");

            if (showAlreadyRunningWarning)
                ShowAlreadyRunningSnackbar();

            LoadState();

            var allItems = RootNavigation.Items.OfType<NavigationItem>().ToList();
            var allFooters = RootNavigation.Footer?.OfType<NavigationItem>().ToList() ?? new List<NavigationItem>();

            MainNavigationItems.Clear();
            FooterNavigationItems.Clear();

            foreach (var item in allItems)
                MainNavigationItems.Add(item);

            foreach (var item in allFooters)
                FooterNavigationItems.Add(item);

            CacheDefaultNavigationOrder();

            ReorderNavigationItemsFromSettings();
            RebuildNavigationItems();

            int lastPage = App.State.Prop.LastPage;
            if (lastPage >= 0 && lastPage < RootNavigation.Items.Count)
                RootNavigation.SelectedPageIndex = lastPage;
            else
                RootNavigation.SelectedPageIndex = 0;

            RootNavigation.Navigated += SaveNavigation!;

            void SaveNavigation(object? sender, RoutedNavigationEventArgs? e)
            {
                if (sender == null || e == null) return;

                if (RootNavigation.SelectedPageIndex >= 0 && RootNavigation.SelectedPageIndex < RootNavigation.Items.Count)
                    App.State.Prop.LastPage = RootNavigation.SelectedPageIndex;
            }
        }

        private void CacheDefaultNavigationOrder()
        {
            DefaultNavigationOrder = MainNavigationItems
                .Select(x => x.Tag?.ToString() ?? string.Empty)
                .ToList();

            DefaultFooterOrder = FooterNavigationItems
                .Select(x => x.Tag?.ToString() ?? string.Empty)
                .ToList();
        }

        private void RebuildNavigationItems()
        {
            RootNavigation.Items.Clear();
            foreach (var item in MainNavigationItems)
                RootNavigation.Items.Add(item);

            if (RootNavigation.Footer == null)
                RootNavigation.Footer = new ObservableCollection<INavigationControl>();

            RootNavigation.Footer.Clear();
            foreach (var footerItem in FooterNavigationItems)
                RootNavigation.Footer.Add(footerItem);
        }

        public void ApplyNavigationReorder()
        {
            RebuildNavigationItems();

            App.Settings.Prop.NavigationOrder = MainNavigationItems.Select(item => item.Tag?.ToString() ?? "").ToList();
            App.Settings.Prop.NavigationOrder.AddRange(FooterNavigationItems.Select(item => item.Tag?.ToString() ?? ""));

            App.State.Save();
        }

        private void ReorderNavigationItemsFromSettings()
        {
            if (App.Settings.Prop.NavigationOrder == null || App.Settings.Prop.NavigationOrder.Count == 0)
                return;

            var allItems = MainNavigationItems.Concat(FooterNavigationItems).ToList();

            var reorderedMain = new ObservableCollection<NavigationItem>();
            var reorderedFooter = new ObservableCollection<NavigationItem>();

            foreach (var tag in App.Settings.Prop.NavigationOrder)
            {
                var navItem = allItems.FirstOrDefault(i => i.Tag?.ToString() == tag);
                if (navItem != null)
                {
                    if (MainNavigationItems.Contains(navItem))
                        reorderedMain.Add(navItem);
                    else if (FooterNavigationItems.Contains(navItem))
                        reorderedFooter.Add(navItem);
                }
            }

            foreach (var item in MainNavigationItems)
            {
                if (!reorderedMain.Contains(item))
                    reorderedMain.Add(item);
            }
            foreach (var item in FooterNavigationItems)
            {
                if (!reorderedFooter.Contains(item))
                    reorderedFooter.Add(item);
            }

            MainNavigationItems.Clear();
            foreach (var item in reorderedMain)
                MainNavigationItems.Add(item);

            FooterNavigationItems.Clear();
            foreach (var item in reorderedFooter)
                FooterNavigationItems.Add(item);

            RebuildNavigationItems();
        }

        public void ResetNavigationToDefault()
        {
            var allItems = MainNavigationItems.Concat(FooterNavigationItems).ToList();

            var reorderedMain = new ObservableCollection<NavigationItem>();
            var reorderedFooter = new ObservableCollection<NavigationItem>();

            foreach (var tag in DefaultNavigationOrder)
            {
                var navItem = allItems.FirstOrDefault(i => i.Tag?.ToString() == tag);
                if (navItem != null)
                    reorderedMain.Add(navItem);
            }
            foreach (var item in MainNavigationItems)
            {
                if (!reorderedMain.Contains(item))
                    reorderedMain.Add(item);
            }

            foreach (var tag in DefaultFooterOrder)
            {
                var navItem = allItems.FirstOrDefault(i => i.Tag?.ToString() == tag);
                if (navItem != null)
                    reorderedFooter.Add(navItem);
            }
            foreach (var item in FooterNavigationItems)
            {
                if (!reorderedFooter.Contains(item))
                    reorderedFooter.Add(item);
            }

            MainNavigationItems.Clear();
            foreach (var item in reorderedMain)
                MainNavigationItems.Add(item);

            FooterNavigationItems.Clear();
            foreach (var item in reorderedFooter)
                FooterNavigationItems.Add(item);

            RebuildNavigationItems();

            App.Settings.Prop.NavigationOrder.Clear();
            App.State.Save();
        }

        public void LoadState()
        {
            if (_state.Left > SystemParameters.VirtualScreenWidth)
                _state.Left = 0;

            if (_state.Top > SystemParameters.VirtualScreenHeight)
                _state.Top = 0;

            if (_state.Width > 0)
                this.Width = _state.Width;

            if (_state.Height > 0)
                this.Height = _state.Height;

            if (_state.Left > 0 && _state.Top > 0)
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Left = _state.Left;
                this.Top = _state.Top;
            }
        }

        private async void ShowAlreadyRunningSnackbar()
        {
            await Task.Delay(750); // wait for everything to finish loading
            AlreadyRunningSnackbar.Show();
        }

        #region INavigationWindow methods

        public Frame GetFrame() => RootFrame;

        public INavigation GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(IPageService pageService) => RootNavigation.PageService = pageService;

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        private void WpfUiWindow_Closing(object sender, CancelEventArgs e)
        {
            if (App.FastFlags.Changed || App.PendingSettingTasks.Any())
            {
                var result = Frontend.ShowMessageBox(Strings.Menu_UnsavedChanges, MessageBoxImage.Warning, MessageBoxButton.YesNo);

                if (result != MessageBoxResult.Yes)
                    e.Cancel = true;
            }

            _state.Width = this.Width;
            _state.Height = this.Height;

            _state.Top = this.Top;
            _state.Left = this.Left;

            App.State.Save();
        }

        private void WpfUiWindow_Closed(object sender, EventArgs e)
        {
            if (App.LaunchSettings.TestModeFlag.Active)
                LaunchHandler.LaunchRoblox(LaunchMode.Player);
            else
                App.SoftTerminate();
        }

        public void ShowLoading(string message = "Loading...")
        {
            LoadingOverlayText.Text = message;
            LoadingOverlay.Visibility = Visibility.Visible;
            // DO NOT disable RootGrid
            // RootGrid.IsEnabled = false;
        }

        public void HideLoading()
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            // RootGrid.IsEnabled = true;
        }

    }
}