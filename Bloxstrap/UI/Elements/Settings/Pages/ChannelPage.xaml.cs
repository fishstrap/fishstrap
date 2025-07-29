using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Bloxstrap.UI.ViewModels.Settings;
using Microsoft.Win32;
using Wpf.Ui.Hardware;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for ChannelPage.xaml
    /// </summary>
    public partial class ChannelPage
    {
        public ChannelPage()
        {
            DataContext = new ChannelViewModel();
            InitializeComponent();
            (App.Current as App)?._froststrapRPC?.UpdatePresence("Page: Settings");
        }

        private void ResetSettings_Click(object sender, RoutedEventArgs e)
        {
            var confirm = Frontend.ShowMessageBox(
                "Are you sure you want to reset all settings to their default values?",
                MessageBoxImage.Warning,
                MessageBoxButton.YesNo
            );

            if (confirm != MessageBoxResult.Yes)
                return;

            string preservedUserId = App.Settings.Prop.UserId;

            App.Settings.Prop = new Models.Persistable.Settings();
            App.Settings.Prop.UserId = preservedUserId; // this is so the user dosent lose his id that he uses for publishing lists

            App.Settings.Save();

            Frontend.ShowMessageBox("Settings have been reset. Restarting the app...", MessageBoxImage.Information);

            System.Windows.Forms.Application.Restart();
            Application.Current.Shutdown();
        }

        private void ExportSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                FileName = "FroststrapSettings.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string json = JsonSerializer.Serialize(App.Settings, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    File.WriteAllText(dialog.FileName, json);
                    Frontend.ShowMessageBox("Settings exported successfully.", MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Frontend.ShowMessageBox($"Failed to export settings: {ex.Message}", MessageBoxImage.Error);
                }
            }
        }

        private void ImportSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(dialog.FileName);
                    var imported = JsonSerializer.Deserialize<Models.Persistable.Settings>(json);

                    if (imported is not null)
                    {
                        App.Settings.Prop = imported;
                        App.Settings.Save();

                        Frontend.ShowMessageBox("Settings imported successfully. Restarting the app...", MessageBoxImage.Information);
                        System.Windows.Forms.Application.Restart();
                        Application.Current.Shutdown();
                    }
                    else
                    {
                        Frontend.ShowMessageBox("The selected file is not a valid settings file.", MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    Frontend.ShowMessageBox($"Failed to import settings: {ex.Message}", MessageBoxImage.Error);
                }
            }
        }

        private void ToggleSwitch_Checked_1(object sender, RoutedEventArgs e)
        {
            HardwareAcceleration.MemoryTrimming();
        }

        private void ToggleSwitch_Unchecked_1(object sender, RoutedEventArgs e)
        {
            Frontend.ShowMessageBox(
            Strings.Menu_Channels_HardwareAccelRestart,
            MessageBoxImage.Information
            );
        }

        private void ToggleSwitch_Checked_2(object sender, RoutedEventArgs e)
        {
            HardwareAcceleration.DisableAllAnimations();
        }

        private void ToggleSwitch_Unchecked_2(object sender, RoutedEventArgs e)
        {
            Frontend.ShowMessageBox(
            Strings.Menu_Channels_DisableAnimationRestart,
            MessageBoxImage.Information
            );
        }
    }
}