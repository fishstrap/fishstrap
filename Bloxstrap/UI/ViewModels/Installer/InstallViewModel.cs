using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Bloxstrap.UI.ViewModels.Installer
{
    public class InstallViewModel : NotifyPropertyChangedViewModel
    {
        private readonly Bloxstrap.Installer installer = new();

        private readonly string _originalInstallLocation;

        public event EventHandler<bool>? SetCanContinueEvent;

        public string InstallLocation
        {
            get => installer.InstallLocation;
            set
            {
                if (!string.IsNullOrEmpty(ErrorMessage))
                {
                    SetCanContinueEvent?.Invoke(this, true);

                    installer.InstallLocationError = "";
                    OnPropertyChanged(nameof(ErrorMessage));
                }

                installer.InstallLocation = value;
                OnPropertyChanged(nameof(InstallLocation));
                OnPropertyChanged(nameof(DataFoundMessageVisibility));
            }
        }

        public Visibility DataFoundMessageVisibility => installer.ExistingDataPresent ? Visibility.Visible : Visibility.Collapsed;

        public string ErrorMessage => installer.InstallLocationError;

        public bool CreateDesktopShortcuts
        {
            get => installer.CreateDesktopShortcuts;
            set => installer.CreateDesktopShortcuts = value;
        }

        public bool CreateStartMenuShortcuts
        {
            get => installer.CreateStartMenuShortcuts;
            set => installer.CreateStartMenuShortcuts = value;
        }

        public bool ImportSettings
        {
            get => installer.ImportSettings;
            set
            {
                installer.ImportSettings = value;
                OnPropertyChanged(nameof(ImportSettings));
                // Trigger validation update if disabling import
                if (!value)
                {
                    installer.InstallLocationError = "";
                    SetCanContinueEvent?.Invoke(this, true);
                    OnPropertyChanged(nameof(ErrorMessage));
                }
            }
        }

        public bool ImportSettingsEnabled
        {
            get
            {
                return Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Bloxstrap")) ||
                       Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Voidstrap")) ||
                       Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fishstrap"));
            }
        }

        public bool ShowNotFound => !ImportSettingsEnabled;

        public ICommand BrowseInstallLocationCommand => new RelayCommand(BrowseInstallLocation);

        public ICommand ResetInstallLocationCommand => new RelayCommand(ResetInstallLocation);

        public ICommand OpenFolderCommand => new RelayCommand(OpenFolder);

        public ImportSettingsFrom SelectedImportSource
        {
            get => installer.ImportSource;
            set
            {
                installer.ImportSource = value;
                OnPropertyChanged(nameof(SelectedImportSource));
            }
        }

        public Array ImportSourceOptions => Enum.GetValues(typeof(ImportSettingsFrom));

        public InstallViewModel()
        {
            _originalInstallLocation = installer.InstallLocation;
        }

        public bool ValidateImportSource()
        {
            if (!ImportSettings)
                return true; // Import disabled, no validation needed

            string folderPath = SelectedImportSource switch
            {
                ImportSettingsFrom.Bloxstrap => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Bloxstrap"),
                ImportSettingsFrom.Voidstrap => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Voidstrap"),
                ImportSettingsFrom.Fishstrap => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fishstrap"),
                ImportSettingsFrom.Lunastrap => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lunastrap"),
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                installer.InstallLocationError = $"Selected import source folder not found: {SelectedImportSource}";
                OnPropertyChanged(nameof(ErrorMessage));
                SetCanContinueEvent?.Invoke(this, false);
                return false;
            }

            // Clear previous error and enable continue button
            installer.InstallLocationError = "";
            OnPropertyChanged(nameof(ErrorMessage));
            SetCanContinueEvent?.Invoke(this, true);
            return true;
        }

        public bool DoInstall()
        {
            if (!ValidateImportSource())
            {
                return false; // Block navigation if import source invalid
            }

            if (!installer.CheckInstallLocation())
            {
                SetCanContinueEvent?.Invoke(this, false);
                OnPropertyChanged(nameof(ErrorMessage));
                return false;
            }

            installer.DoInstall();
            return true;
        }

        private void BrowseInstallLocation()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            InstallLocation = dialog.SelectedPath;
            OnPropertyChanged(nameof(InstallLocation));
        }

        private void ResetInstallLocation()
        {
            InstallLocation = _originalInstallLocation;
            OnPropertyChanged(nameof(InstallLocation));
        }

        private void OpenFolder() => System.Diagnostics.Process.Start("explorer.exe", Paths.Base);
    }
}