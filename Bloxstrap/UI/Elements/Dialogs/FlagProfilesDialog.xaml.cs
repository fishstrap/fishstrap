using Microsoft.Win32;
using System.Windows;
using Bloxstrap.Resources;
using System.Reflection;

namespace Bloxstrap.UI.Elements.Dialogs
{
    /// <summary>
    /// Interaction logic for FlagProfilesDialog.xaml
    /// </summary>
    public partial class FlagProfilesDialog
    {
        public MessageBoxResult Result = MessageBoxResult.Cancel;

        public FlagProfilesDialog()
        {
            InitializeComponent();
            LoadProfiles();
            LoadPresetProfiles();
        }

        private void LoadProfiles()
        {
            LoadProfile.Items.Clear();

            string profilesDirectory = Path.Combine(Paths.Base, Paths.SavedFlagProfiles);

            if (!Directory.Exists(profilesDirectory))
                Directory.CreateDirectory(profilesDirectory);

            string[] Profiles = Directory.GetFiles(profilesDirectory);

            foreach (string rawProfileName in Profiles)
            {
                string ProfileName = Path.GetFileName(rawProfileName);
                LoadProfile.Items.Add(ProfileName);
            }

            LoadProfileEmptyText.Visibility = LoadProfile.Items.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            RenamePanel.Visibility = LoadProfile.Items.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            RenameTextBox.Text = string.Empty;
            RenameTextBox.IsEnabled = false;
        }

        private void LoadProfile_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (LoadProfile.SelectedItem is string selectedProfile)
            {
                RenameTextBox.Text = selectedProfile;
                RenameTextBox.IsEnabled = true;
            }
            else
            {
                RenameTextBox.Text = string.Empty;
                RenameTextBox.IsEnabled = false;
            }
        }

        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            if (LoadProfile.SelectedItem is not string oldProfileName)
            {
                Frontend.ShowMessageBox("Please select a profile to rename.", MessageBoxImage.Warning, MessageBoxButton.OK);
                return;
            }

            string newName = RenameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(newName))
            {
                Frontend.ShowMessageBox("New profile name cannot be empty.", MessageBoxImage.Error, MessageBoxButton.OK);
                return;
            }

            foreach (var c in Path.GetInvalidFileNameChars())
            {
                if (newName.Contains(c))
                {
                    Frontend.ShowMessageBox($"Profile name contains invalid character '{c}'.", MessageBoxImage.Error, MessageBoxButton.OK);
                    return;
                }
            }

            string profilesDirectory = Path.Combine(Paths.Base, Paths.SavedFlagProfiles);
            string oldPath = Path.Combine(profilesDirectory, oldProfileName);
            string newPath = Path.Combine(profilesDirectory, newName);

            if (File.Exists(newPath))
            {
                Frontend.ShowMessageBox("A profile with that name already exists.", MessageBoxImage.Error, MessageBoxButton.OK);
                return;
            }

            try
            {
                File.Move(oldPath, newPath);
                LoadProfiles();
                LoadProfile.SelectedItem = newName;
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Failed to rename profile:\n{ex.Message}", MessageBoxImage.Error, MessageBoxButton.OK);
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (LoadProfile.SelectedItem is not string selectedProfile)
            {
                Frontend.ShowMessageBox("Please select a profile to update.", MessageBoxImage.Warning, MessageBoxButton.OK);
                return;
            }

            try
            {
                var currentFlags = App.FastFlags.Prop;

                if (currentFlags == null)
                {
                    Frontend.ShowMessageBox("Failed to get current FastFlags.", MessageBoxImage.Error, MessageBoxButton.OK);
                    return;
                }

                string json = JsonSerializer.Serialize(currentFlags, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                string profilesDirectory = Path.Combine(Paths.Base, Paths.SavedFlagProfiles);
                string profilePath = Path.Combine(profilesDirectory, selectedProfile);

                File.WriteAllText(profilePath, json);

                LoadProfiles();
                LoadProfile.SelectedItem = selectedProfile;
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Failed to update profile:\n{ex.Message}", MessageBoxImage.Error, MessageBoxButton.OK);
            }
        }

        private void LoadPresetProfiles()
        {
            LoadPresetProfile.Items.Clear();

            var assembly = Assembly.GetExecutingAssembly();
            string resourcePrefix = "Bloxstrap.Resources.PresetFlags.";

            var resourceNames = assembly.GetManifestResourceNames();

            var profiles = resourceNames.Where(r => r.StartsWith(resourcePrefix));

            foreach (var resourceName in profiles)
            {
                string profileName = resourceName.Substring(resourcePrefix.Length);
                LoadPresetProfile.Items.Add(profileName);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            switch (Tabs.SelectedIndex)
            {
                case 0: // Save tab
                    if (!string.IsNullOrWhiteSpace(SaveProfile.Text))
                    {
                        App.FastFlags.SaveProfile(SaveProfile.Text);
                    }
                    break;
                case 1: // Load tab
                    if (LoadProfile.SelectedItem is string selectedProfile)
                    {
                        App.FastFlags.LoadProfile(selectedProfile, clearFlags: ClearFlags.IsChecked == true);
                    }
                    break;

                case 2: // Preset Flags tab
                    if (LoadPresetProfile.SelectedItem is string selectedPreset)
                    {
                        App.FastFlags.LoadPresetProfile(selectedPreset, clearFlags: true);
                    }
                    break;
            }

            Result = MessageBoxResult.OK;
            Close();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            string? ProfileName = LoadProfile.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(ProfileName))
                return;

            App.FastFlags.DeleteProfile(ProfileName);
            LoadProfiles();
        }
    }
}