using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.IO;
using System.Windows.Input;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public partial class ShortcutsViewModel : NotifyPropertyChangedViewModel
    {
        public bool IsStudioOptionVisible => App.IsStudioVisible;

        public ShortcutTask DesktopIconTask { get; } = new("Desktop", Paths.Desktop, $"{App.ProjectName}.lnk");
        public ShortcutTask StartMenuIconTask { get; } = new("StartMenu", Paths.WindowsStartMenu, $"{App.ProjectName}.lnk");
        public ShortcutTask PlayerIconTask { get; } = new("RobloxPlayer", Paths.Desktop, $"{Strings.LaunchMenu_LaunchRoblox}.lnk", "-player");
        public ShortcutTask StudioIconTask { get; } = new("RobloxStudio", Paths.Desktop, $"{Strings.LaunchMenu_LaunchRobloxStudio}.lnk", "-studio");
        public ShortcutTask SettingsIconTask { get; } = new("Settings", Paths.Desktop, $"{Strings.Menu_Title}.lnk", "-settings");
        public ExtractIconsTask ExtractIconsTask { get; } = new();

        private string _gameShortcutId = "";
        public string GameShortcutId
        {
            get => _gameShortcutId;
            set
            {
                if (_gameShortcutId != value)
                {
                    _gameShortcutId = value;
                    OnPropertyChanged(nameof(GameShortcutId));
                }
            }
        }

        private string _gameShortcutName = "Roblox Game";
        public string GameShortcutName
        {
            get => _gameShortcutName;
            set
            {
                if (_gameShortcutName != value)
                {
                    _gameShortcutName = value;
                    OnPropertyChanged(nameof(GameShortcutName));
                }
            }
        }

        private string _gameShortcutIconPath = "";
        public string GameShortcutIconPath
        {
            get => _gameShortcutIconPath;
            set
            {
                if (_gameShortcutIconPath != value)
                {
                    _gameShortcutIconPath = value;
                    OnPropertyChanged(nameof(GameShortcutIconPath));
                }
            }
        }

        private string _gameShortcutStatus = "";
        public string GameShortcutStatus
        {
            get => _gameShortcutStatus;
            set
            {
                if (_gameShortcutStatus != value)
                {
                    _gameShortcutStatus = value;
                    OnPropertyChanged(nameof(GameShortcutStatus));
                }
            }
        }

        public ICommand BrowseGameShortcutIconCommand { get; }
        public ICommand CreateGameShortcutCommand { get; }

        public ShortcutsViewModel()
        {
            BrowseGameShortcutIconCommand = new RelayCommand(BrowseGameShortcutIcon);
            CreateGameShortcutCommand = new RelayCommand(CreateGameShortcut);
        }

        private void BrowseGameShortcutIcon()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Icon Files (*.ico)|*.ico|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                GameShortcutIconPath = dlg.FileName;
            }
        }

        private void CreateGameShortcut()
        {
            if (string.IsNullOrWhiteSpace(GameShortcutId))
            {
                GameShortcutStatus = "Game ID is required.";
                return;
            }

            string url = $"roblox://placeId={GameShortcutId}/";
            string shortcutPath = Path.Combine(
                Paths.Desktop,
                $"{GameShortcutName}.url"
            );

            using (var writer = new StreamWriter(shortcutPath))
            {
                writer.WriteLine("[InternetShortcut]");
                writer.WriteLine($"URL={url}");
                if (!string.IsNullOrWhiteSpace(GameShortcutIconPath) && 
                    Path.GetExtension(GameShortcutIconPath).Equals(".ico", StringComparison.OrdinalIgnoreCase) &&
                    File.Exists(GameShortcutIconPath))
                {
                    writer.WriteLine($"IconFile={GameShortcutIconPath}");
                    writer.WriteLine("IconIndex=0");
                }
            }

            GameShortcutStatus = $"Shortcut created: {shortcutPath}";
        }
    }
}