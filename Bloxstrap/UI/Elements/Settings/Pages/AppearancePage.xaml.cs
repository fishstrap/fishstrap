using Bloxstrap.UI.ViewModels.Settings;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for AppearancePage.xaml
    /// </summary>
    public partial class AppearancePage
    {
        private readonly MainWindow _mainWindow;

        private bool _isNavigationLocked = false;

        public AppearancePage()
        {
            InitializeComponent();

            this.DataContext = new AppearanceViewModel(this);

            _isNavigationLocked = App.Settings.Prop.IsNavigationOrderLocked;

            UpdateNavigationLockUI();

            _mainWindow = (MainWindow)System.Windows.Application.Current.MainWindow;

            ListBoxNavigationItems.ItemsSource = MainWindow.MainNavigationItems;
        }

        public void CustomThemeSelection(object sender, SelectionChangedEventArgs e)
        {
            var viewModel = (AppearanceViewModel)DataContext;

            viewModel.SelectedCustomTheme = (string)((ListBox)sender).SelectedItem;
            viewModel.SelectedCustomThemeName = viewModel.SelectedCustomTheme;

            viewModel.OnPropertyChanged(nameof(viewModel.SelectedCustomTheme));
            viewModel.OnPropertyChanged(nameof(viewModel.SelectedCustomThemeName));
        }

        public List<string> NavigationOrder
        {
            get => App.Settings.Prop.NavigationOrder;
            set
            {
                App.Settings.Prop.NavigationOrder = value;
                App.State.Save();
            }
        }

        private void UpdateNavigationLockUI()
        {
            MoveUpButton.IsEnabled = !_isNavigationLocked;
            MoveDownButton.IsEnabled = !_isNavigationLocked;
            ResetToDefaultButton.IsEnabled = !_isNavigationLocked;

            ToggleLockOrder.IsChecked = _isNavigationLocked;
        }

        private void LockToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            SetNavigationLock(true);
        }

        private void LockToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            SetNavigationLock(false);
        }

        private void SetNavigationLock(bool isLocked)
        {
            if (_isNavigationLocked == isLocked)
                return;

            _isNavigationLocked = isLocked;
            App.Settings.Prop.IsNavigationOrderLocked = isLocked;
            App.State.Save();

            UpdateNavigationLockUI();
        }

        private void ResetOrder_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.ResetNavigationToDefault();
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxNavigationItems.SelectedItem is not NavigationItem selectedItem)
                return;

            if (MainWindow.MainNavigationItems.Contains(selectedItem))
            {
                var navItems = MainWindow.MainNavigationItems;
                int index = navItems.IndexOf(selectedItem);

                if (index > 0)
                {
                    navItems.Move(index, index - 1);
                    ListBoxNavigationItems.SelectedItem = selectedItem;
                    _mainWindow.ApplyNavigationReorder();
                    SaveNavigationOrder();
                }
            }
            else if (_mainWindow.RootNavigation.Footer.Contains(selectedItem))
            {
                var footerList = _mainWindow.RootNavigation.Footer;
                int index = footerList.IndexOf(selectedItem);

                if (index > 0)
                {
                    footerList.Move(index, index - 1);
                    ListBoxNavigationItems.SelectedItem = selectedItem;
                    _mainWindow.ApplyNavigationReorder();
                    SaveNavigationOrder();
                }
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxNavigationItems.SelectedItem is not NavigationItem selectedItem)
                return;

            if (MainWindow.MainNavigationItems.Contains(selectedItem))
            {
                var navItems = MainWindow.MainNavigationItems;
                int index = navItems.IndexOf(selectedItem);

                if (index < navItems.Count - 1)
                {
                    navItems.Move(index, index + 1);
                    ListBoxNavigationItems.SelectedItem = selectedItem;
                    _mainWindow.ApplyNavigationReorder();
                    SaveNavigationOrder();
                }
            }
            else if (_mainWindow.RootNavigation.Footer.Contains(selectedItem))
            {
                var footerList = _mainWindow.RootNavigation.Footer;
                int index = footerList.IndexOf(selectedItem);

                if (index < footerList.Count - 1)
                {
                    footerList.Move(index, index + 1);
                    ListBoxNavigationItems.SelectedItem = selectedItem;
                    _mainWindow.ApplyNavigationReorder();
                    SaveNavigationOrder();
                }
            }
        }

        private void SaveNavigationOrder()
        {
            var mainTags = MainWindow.MainNavigationItems?.Select(item => item.Tag?.ToString()) ?? Enumerable.Empty<string>();
            var footerTags = _mainWindow.RootNavigation?.Footer?.OfType<NavigationItem>().Select(item => item.Tag?.ToString()) ?? Enumerable.Empty<string>();

            var order = mainTags.Concat(footerTags)
                                .Where(tag => !string.IsNullOrEmpty(tag))
                                .ToList();

            if (App.Settings?.Prop != null)
            {
                App.Settings.Prop.NavigationOrder = order!;
                App.State?.Save();
            }
        }
    }
}