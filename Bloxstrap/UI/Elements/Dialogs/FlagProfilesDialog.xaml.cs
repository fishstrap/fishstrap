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
        }

        private void LoadPresetProfiles()
        {
            LoadPresetProfile.Items.Clear();

            var assembly = Assembly.GetExecutingAssembly();
            string resourcePrefix = "Bloxstrap.Resources.PresetFlags."; // change this!

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
                case 1: // Load tab
                    if (LoadProfile.SelectedItem is string selectedProfile)
                    {
                        App.FastFlags.LoadProfile(selectedProfile, clearFlags: ClearFlags.IsChecked == true);
                    }
                    break;

                case 2: // Preset Flags tab
                    if (LoadPresetProfile.SelectedItem is string selectedPreset)
                    {
                        App.FastFlags.LoadPresetProfile(selectedPreset, clearFlags: true); // or use ClearFlags if needed
                    }
                    break;

                case 0: // Save tab (optional)
                    if (!string.IsNullOrWhiteSpace(SaveProfile.Text))
                    {
                        App.FastFlags.SaveProfile(SaveProfile.Text);
                    }
                    break;
            }

            Result = MessageBoxResult.OK;
            Close();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {

            string? ProfileName = LoadProfile.SelectedItem.ToString();

            if (String.IsNullOrEmpty(ProfileName))
                return;

            App.FastFlags.DeleteProfile(ProfileName);
            LoadProfiles();
        }
    }
}
