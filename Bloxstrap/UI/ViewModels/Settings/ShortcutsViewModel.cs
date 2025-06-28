using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Bloxstrap.Models.APIs.Roblox;
using Bloxstrap.Utility;
using System.Collections.Specialized;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public partial class ShortcutsViewModel : INotifyPropertyChanged
    {
        public bool IsStudioOptionVisible => App.IsStudioVisible;

        public ShortcutTask DesktopIconTask { get; } = new("Desktop", Paths.Desktop, $"{App.ProjectName}.lnk");
        public ShortcutTask StartMenuIconTask { get; } = new("StartMenu", Paths.WindowsStartMenu, $"{App.ProjectName}.lnk");
        public ShortcutTask PlayerIconTask { get; } = new("RobloxPlayer", Paths.Desktop, $"{Strings.LaunchMenu_LaunchRoblox}.lnk", "-player");
        public ShortcutTask StudioIconTask { get; } = new("RobloxStudio", Paths.Desktop, $"{Strings.LaunchMenu_LaunchRobloxStudio}.lnk", "-studio");
        public ShortcutTask SettingsIconTask { get; } = new("Settings", Paths.Desktop, $"{Strings.Menu_Title}.lnk", "-settings");
        public ExtractIconsTask ExtractIconsTask { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        public ObservableCollection<GameShortcut> GameShortcuts { get; } = new();

        private GameShortcut? _selectedShortcut;
        public GameShortcut? SelectedShortcut
        {
            get => _selectedShortcut;
            set
            {
                if (_selectedShortcut != value)
                {
                    _selectedShortcut = value;
                    OnPropertyChanged(nameof(SelectedShortcut));
                    GameShortcutStatus = "";
                    RemoveShortcutCommand.NotifyCanExecuteChanged();
                    DownloadIconCommand.NotifyCanExecuteChanged();
                    BrowseIconCommand.NotifyCanExecuteChanged();
                    CreateShortcutCommand.NotifyCanExecuteChanged();
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

        public RelayCommand AddShortcutCommand { get; }
        public RelayCommand RemoveShortcutCommand { get; }
        public RelayCommand DownloadIconCommand { get; }
        public RelayCommand BrowseIconCommand { get; }
        public RelayCommand CreateShortcutCommand { get; }

        private static readonly string ShortcutSavePath = Path.Combine(Paths.LocalAppData, App.ProjectName, "shortcuts.json");

        public ShortcutsViewModel()
        {
            LoadShortcuts();

            AddShortcutCommand = new RelayCommand(AddShortcut);
            RemoveShortcutCommand = new RelayCommand(RemoveShortcut, () => SelectedShortcut != null);
            DownloadIconCommand = new RelayCommand(async () => await DownloadIconAsync(), () => SelectedShortcut != null && !string.IsNullOrWhiteSpace(SelectedShortcut?.GameId));
            BrowseIconCommand = new RelayCommand(BrowseIcon, () => SelectedShortcut != null);
            CreateShortcutCommand = new RelayCommand(CreateShortcut, () => SelectedShortcut != null);

            GameShortcuts.CollectionChanged += GameShortcuts_CollectionChanged;
        }

        private void AddShortcut()
        {
            var newShortcut = new GameShortcut() { GameName = "New Game", GameId = "" };
            newShortcut.PropertyChanged += GameShortcut_PropertyChanged;
            GameShortcuts.Add(newShortcut);
            SelectedShortcut = newShortcut;
            SaveShortcuts();
        }

        private void RemoveShortcut()
        {
            if (SelectedShortcut != null)
            {
                SelectedShortcut.PropertyChanged -= GameShortcut_PropertyChanged;
                GameShortcuts.Remove(SelectedShortcut);
                SelectedShortcut = null;
                SaveShortcuts();
            }
        }

        private void LoadShortcuts()
        {
            try
            {
                var json = App.Settings.Prop.GameShortcutsJson;
                var shortcuts = JsonSerializer.Deserialize<GameShortcut[]>(json);
                if (shortcuts != null && shortcuts.Length > 0)
                {
                    foreach (var shortcut in shortcuts)
                    {
                        shortcut.PropertyChanged += GameShortcut_PropertyChanged;
                        GameShortcuts.Add(shortcut);
                    }
                }
                else
                {
                    var example = new GameShortcut()
                    {
                        GameName = "Roblox Game",
                        GameId = "18668065416"
                    };
                    example.PropertyChanged += GameShortcut_PropertyChanged;
                    GameShortcuts.Add(example);
                }
            }
            catch
            {
                // ignore load errors
            }
        }

        private void SaveShortcuts()
        {
            try
            {
                var shortcuts = GameShortcuts.ToArray();
                var json = JsonSerializer.Serialize(shortcuts, new JsonSerializerOptions { WriteIndented = true });
                App.Settings.Prop.GameShortcutsJson = json;
                App.Settings.Save();  // Call your settings save method here
            }
            catch
            {
                // ignore save errors
            }
        }

        private void GameShortcuts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (GameShortcut oldItem in e.OldItems)
                    oldItem.PropertyChanged -= GameShortcut_PropertyChanged;
            }

            if (e.NewItems != null)
            {
                foreach (GameShortcut newItem in e.NewItems)
                    newItem.PropertyChanged += GameShortcut_PropertyChanged;
            }

            SaveShortcuts();
            RemoveShortcutCommand.NotifyCanExecuteChanged();
            DownloadIconCommand.NotifyCanExecuteChanged();
            BrowseIconCommand.NotifyCanExecuteChanged();
            CreateShortcutCommand.NotifyCanExecuteChanged();
        }

        private void GameShortcut_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            SaveShortcuts();
        }

        private async Task DownloadIconAsync()
        {
            if (SelectedShortcut == null)
            {
                GameShortcutStatus = "No shortcut selected.";
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedShortcut.GameId) || !ulong.TryParse(SelectedShortcut.GameId, out var id))
            {
                GameShortcutStatus = "Invalid Game ID.";
                return;
            }

            try
            {
                var request = new ThumbnailRequest
                {
                    TargetId = id,
                    Type = "PlaceIcon",
                    Size = "512x512",
                    Format = "Png",
                    IsCircular = false
                };

                string? imageUrl = await Thumbnails.GetThumbnailUrlAsync(request, CancellationToken.None);

                if (string.IsNullOrEmpty(imageUrl))
                {
                    GameShortcutStatus = $"No icon URL returned from Roblox for Game ID {id}. It may not exist or have no icon.";
                    return;
                }

                using var http = new System.Net.Http.HttpClient();
                var imageBytes = await http.GetByteArrayAsync(imageUrl);

                string cacheDir = Paths.ShortcutIcons;
                Directory.CreateDirectory(cacheDir);

                string timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
                string icoPath = Path.Combine(cacheDir, $"{id}_{timestamp}.ico");

                using (var ms = new MemoryStream(imageBytes))
                using (var bitmap = new Bitmap(ms))
                using (var iconStream = new FileStream(icoPath, FileMode.Create))
                {
                    SaveBitmapAsIcon(bitmap, iconStream);
                }

                SelectedShortcut.IconPath = icoPath;
                GameShortcutStatus = $"Game Icon Successfully Applied";
                SaveShortcuts();
            }
            catch (Exception ex)
            {
                GameShortcutStatus = $"Failed to download icon: {ex.Message}";
            }
        }

        private void BrowseIcon()
        {
            if (SelectedShortcut == null) return;

            var dlg = new OpenFileDialog
            {
                Filter = "Icon Files (*.ico)|*.ico|All Files (*.*)|*.*",
                Title = "Select an icon file"
            };
            if (dlg.ShowDialog() == true)
            {
                SelectedShortcut.IconPath = dlg.FileName;
                GameShortcutStatus = "Custom icon set.";
                SaveShortcuts();
            }
        }

        private void CreateShortcut()
        {
            if (SelectedShortcut == null)
            {
                GameShortcutStatus = "Select a shortcut first.";
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedShortcut.GameId))
            {
                GameShortcutStatus = "Game ID is required.";
                return;
            }

            try
            {
                string url = $"roblox://placeId={SelectedShortcut.GameId}/";
                string safeName = SanitizeFileName(SelectedShortcut.GameName);
                string timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
                string shortcutPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    $"{safeName}.url"
                );

                using var writer = new StreamWriter(shortcutPath, false, System.Text.Encoding.UTF8);
                writer.WriteLine("[InternetShortcut]");
                writer.WriteLine($"URL={url}");

                if (!string.IsNullOrWhiteSpace(SelectedShortcut.IconPath) &&
                    File.Exists(SelectedShortcut.IconPath) &&
                    Path.GetExtension(SelectedShortcut.IconPath).Equals(".ico", StringComparison.OrdinalIgnoreCase))
                {
                    writer.WriteLine($"IconFile=\"{SelectedShortcut.IconPath}\"");
                    writer.WriteLine("IconIndex=0");
                }

                writer.WriteLine("HotKey=0");
                writer.WriteLine("IDList=");
                writer.WriteLine("");

                GameShortcutStatus = $"Shortcut created: {shortcutPath}";
            }
            catch (Exception ex)
            {
                GameShortcutStatus = $"Failed to create shortcut: {ex.Message}";
            }
        }

        private static void SaveBitmapAsIcon(Bitmap bmp, Stream outputStream)
        {
            using var resized = new Bitmap(bmp, new Size(256, 256));
            using var iconBitmap = new Bitmap(resized.Width, resized.Height, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(iconBitmap))
                g.DrawImage(resized, 0, 0, resized.Width, resized.Height);

            using var msPng = new MemoryStream();
            iconBitmap.Save(msPng, ImageFormat.Png);
            byte[] pngData = msPng.ToArray();

            using var writer = new BinaryWriter(outputStream);

            writer.Write((short)0);
            writer.Write((short)1);
            writer.Write((short)1);

            writer.Write((byte)iconBitmap.Width);
            writer.Write((byte)iconBitmap.Height);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((short)1);
            writer.Write((short)32);
            writer.Write(pngData.Length);
            writer.Write(6 + 16);

            writer.Write(pngData);
        }

        private static string SanitizeFileName(string input)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                input = input.Replace(c, '_');

            return input.Trim();
        }
    }

    public class GameShortcut : INotifyPropertyChanged
    {
        private string _gameName = "";
        private string _gameId = "";
        private string _iconPath = "";

        public string GameName
        {
            get => _gameName;
            set { if (_gameName != value) { _gameName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GameName))); } }
        }

        public string GameId
        {
            get => _gameId;
            set { if (_gameId != value) { _gameId = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GameId))); } }
        }

        public string IconPath
        {
            get => _iconPath;
            set { if (_iconPath != value) { _iconPath = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconPath))); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}