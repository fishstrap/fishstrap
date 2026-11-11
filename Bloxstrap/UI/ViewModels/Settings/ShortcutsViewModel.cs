using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class ShortcutsViewModel : NotifyPropertyChangedViewModel
    {
        private string _gameLink = "";

        private bool _creatingGameShortcut;

        public bool IsStudioOptionVisible => App.IsStudioVisible;

        public ShortcutTask DesktopIconTask { get; } = new("Desktop", Paths.Desktop, $"{App.ProjectName}.lnk");

        public ShortcutTask StartMenuIconTask { get; } = new("StartMenu", Paths.WindowsStartMenu, $"{App.ProjectName}.lnk");

        public ShortcutTask PlayerIconTask { get; } = new("RobloxPlayer", Paths.Desktop, $"{Strings.LaunchMenu_LaunchRoblox}.lnk", "-player");

        public ShortcutTask StudioIconTask { get; } = new("RobloxStudio", Paths.Desktop, $"{Strings.LaunchMenu_LaunchRobloxStudio}.lnk", "-studio");

        public ShortcutTask SettingsIconTask { get; } = new("Settings", Paths.Desktop, $"{Strings.Menu_Title}.lnk", "-settings");

        public ExtractIconsTask ExtractIconsTask { get; } = new();

        public ICommand CreateGameShortcutCommand => new AsyncRelayCommand(CreateGameShortcut);

        public string GameLink
        {
            get => _gameLink;
            set
            {
                _gameLink = value;
                OnPropertyChanged(nameof(GameLink));
            }
        }

        public string GameShortcutStatus { get; private set; } = "";

        public bool CanCreateGameShortcut => !_creatingGameShortcut;

        private async Task CreateGameShortcut()
        {
            const string LOG_IDENT = "ShortcutsViewModel::CreateGameShortcut";

            if (_creatingGameShortcut)
                return;

            long? placeId = GameShortcut.ParsePlaceId(GameLink);

            if (placeId is null)
            {
                SetGameShortcutStatus(Strings.Menu_Shortcuts_Game_InvalidLink);
                return;
            }

            SetCreatingGameShortcut(true);
            SetGameShortcutStatus("");

            try
            {
                string? name = await GameShortcut.Create(placeId.Value);

                if (name is null)
                {
                    SetGameShortcutStatus(Strings.Menu_Shortcuts_Game_NotFound);
                    return;
                }

                GameLink = "";
                SetGameShortcutStatus(String.Format(Strings.Menu_Shortcuts_Game_Created, name));
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
                SetGameShortcutStatus(Strings.Menu_Shortcuts_Game_Failed);
            }
            finally
            {
                SetCreatingGameShortcut(false);
            }
        }

        private void SetGameShortcutStatus(string status)
        {
            GameShortcutStatus = status;
            OnPropertyChanged(nameof(GameShortcutStatus));
        }

        private void SetCreatingGameShortcut(bool creating)
        {
            _creatingGameShortcut = creating;
            OnPropertyChanged(nameof(CanCreateGameShortcut));
        }
    }
}
