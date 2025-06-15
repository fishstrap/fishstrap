using Microsoft.Win32;
using System.Windows;
using Bloxstrap.Resources;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Bloxstrap.UI.Elements.Dialogs
{
    /// <summary>
    /// Interaction logic for AdvancedSettingsDialog.xaml
    /// </summary>
    public partial class AdvancedSettingsDialog
    {
        public AdvancedSettingViewModel ViewModel { get; } = new();
        public static AdvancedSettingViewModel SharedViewModel { get; private set; } = new();

        public AdvancedSettingsDialog()
        {
            InitializeComponent();
            ViewModel.LoadSettings();
            DataContext = ViewModel;

            SharedViewModel = ViewModel;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SaveSettings();
            Frontend.ShowMessageBox(
                Strings.Menu_AdvancedSettings_SettingsSaved,
                MessageBoxImage.Information
            );
        }
    }
}
