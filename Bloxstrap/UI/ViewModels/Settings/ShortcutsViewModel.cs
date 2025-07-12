using Bloxstrap.Models.APIs.Roblox;
using Bloxstrap.Utility;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

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
        private void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

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
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsShortcutSelected));
                    GameShortcutStatus = "";
                    RefreshCommandStates();
                }
            }
        }

        private string _gameShortcutStatus = "";
        public string GameShortcutStatus
        {
            get => _gameShortcutStatus;
            set => SetField(ref _gameShortcutStatus, value);
        }

        public bool IsShortcutSelected => SelectedShortcut != null;

        public RelayCommand AddShortcutCommand { get; }
        public RelayCommand RemoveShortcutCommand { get; }
        public RelayCommand DownloadIconCommand { get; }
        public RelayCommand BrowseIconCommand { get; }
        public RelayCommand CreateShortcutCommand { get; }

        public ShortcutsViewModel()
        {
            AddShortcutCommand = new RelayCommand(AddShortcut);
            RemoveShortcutCommand = new RelayCommand(RemoveShortcut, () => SelectedShortcut != null);
            DownloadIconCommand = new RelayCommand(async () => await DownloadIconAsync(), () => SelectedShortcut != null);
            BrowseIconCommand = new RelayCommand(BrowseIcon, () => SelectedShortcut != null);
            CreateShortcutCommand = new RelayCommand(CreateShortcut, () => SelectedShortcut != null);

            LoadShortcuts();

            GameShortcuts.CollectionChanged += GameShortcuts_CollectionChanged;
        }

        private void RefreshCommandStates()
        {
            RemoveShortcutCommand?.NotifyCanExecuteChanged();
            DownloadIconCommand?.NotifyCanExecuteChanged();
            BrowseIconCommand?.NotifyCanExecuteChanged();
            CreateShortcutCommand?.NotifyCanExecuteChanged();
        }

        private void AddShortcut()
        {
            var shortcut = new GameShortcut { GameName = "New Game", GameId = "" };
            shortcut.PropertyChanged += GameShortcut_PropertyChanged;
            GameShortcuts.Add(shortcut);
            SelectedShortcut = shortcut;
            SaveShortcuts();
        }

        private void RemoveShortcut()
        {
            if (SelectedShortcut == null)
                return;

            SelectedShortcut.PropertyChanged -= GameShortcut_PropertyChanged;
            GameShortcuts.Remove(SelectedShortcut);
            SelectedShortcut = null;

            SaveShortcuts();
            CleanupUnusedIcons();
        }

        private void LoadShortcuts()
        {
            try
            {
                string json = App.Settings.Prop.GameShortcutsJson;
                var shortcuts = JsonSerializer.Deserialize<List<GameShortcut>>(json) ?? new();

                foreach (var shortcut in shortcuts)
                {
                    shortcut.PropertyChanged += GameShortcut_PropertyChanged;
                    GameShortcuts.Add(shortcut);
                }

                if (GameShortcuts.Count == 0)
                    AddShortcut();
            }
            catch
            {
                AddShortcut();
            }
        }

        private void SaveShortcuts()
        {
            try
            {
                string json = JsonSerializer.Serialize(GameShortcuts.ToList(), new JsonSerializerOptions { WriteIndented = true });
                App.Settings.Prop.GameShortcutsJson = json;
                App.Settings.Save();
            }
            catch { }
        }

        private void GameShortcuts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is IEnumerable<GameShortcut> oldItems)
                foreach (var item in oldItems)
                    item.PropertyChanged -= GameShortcut_PropertyChanged;

            if (e.NewItems is IEnumerable<GameShortcut> newItems)
                foreach (var item in newItems)
                    item.PropertyChanged += GameShortcut_PropertyChanged;

            SaveShortcuts();
            CleanupUnusedIcons();
            RefreshCommandStates();
        }

        private void GameShortcut_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            SaveShortcuts();
        }

        private async Task DownloadIconAsync()
        {
            if (SelectedShortcut == null || !ulong.TryParse(SelectedShortcut.GameId, out ulong id))
            {
                GameShortcutStatus = "Invalid or missing Game ID.";
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

                string? url = await Thumbnails.GetThumbnailUrlAsync(request, CancellationToken.None);
                if (string.IsNullOrWhiteSpace(url))
                {
                    GameShortcutStatus = "No icon found for this game.";
                    return;
                }

                using var http = new HttpClient();
                var imageBytes = await http.GetByteArrayAsync(url);

                string hash = ComputeHash(imageBytes);
                string iconDir = Paths.ShortcutIcons;
                Directory.CreateDirectory(iconDir);

                string icoPath = Path.Combine(iconDir, $"{hash}.ico");

                if (!File.Exists(icoPath))
                {
                    using var ms = new MemoryStream(imageBytes);
                    using var bitmap = new Bitmap(ms);
                    using var fs = new FileStream(icoPath, FileMode.Create, FileAccess.Write);
                    SaveBitmapAsIcon(bitmap, fs);
                }

                SelectedShortcut.IconPath = icoPath;
                GameShortcutStatus = "Game icon applied.";
                SaveShortcuts();
            }
            catch (Exception ex)
            {
                GameShortcutStatus = $"Failed to download icon: {ex.Message}";
            }
        }

        private void BrowseIcon()
        {
            if (SelectedShortcut == null)
                return;

            var dlg = new OpenFileDialog
            {
                Filter = "Icon Files (*.ico)|*.ico|All Files (*.*)|*.*",
                Title = "Select Icon"
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
            if (SelectedShortcut == null || string.IsNullOrWhiteSpace(SelectedShortcut.GameId))
            {
                GameShortcutStatus = "Game ID is required.";
                return;
            }

            try
            {
                string url = $"roblox://placeId={SelectedShortcut.GameId}/";
                string safeName = SanitizeFileName(SelectedShortcut.GameName);
                string shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{safeName}.url");

                if (File.Exists(shortcutPath))
                    File.Delete(shortcutPath);

                using var writer = new StreamWriter(shortcutPath, false);
                writer.WriteLine("[InternetShortcut]");
                writer.WriteLine($"URL={url}");

                if (!string.IsNullOrWhiteSpace(SelectedShortcut.IconPath) && File.Exists(SelectedShortcut.IconPath))
                {
                    writer.WriteLine($"IconFile={SelectedShortcut.IconPath}");
                    writer.WriteLine("IconIndex=0");
                }

                writer.WriteLine("HotKey=0");
                writer.WriteLine("IDList=");

                GameShortcutStatus = "Shortcut created on desktop.";
            }
            catch (Exception ex)
            {
                GameShortcutStatus = $"Failed to create shortcut: {ex.Message}";
            }
        }

        private static void SaveBitmapAsIcon(Bitmap bmp, Stream output)
        {
            using var resized = new Bitmap(bmp, new Size(64, 64));
            using var iconBitmap = new Bitmap(resized.Width, resized.Height, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(iconBitmap))
                g.DrawImage(resized, 0, 0, resized.Width, resized.Height);

            using var ms = new MemoryStream();
            iconBitmap.Save(ms, ImageFormat.Png);
            var pngBytes = ms.ToArray();

            using var writer = new BinaryWriter(output);
            writer.Write((short)0);
            writer.Write((short)1);
            writer.Write((short)1);

            writer.Write((byte)iconBitmap.Width);
            writer.Write((byte)iconBitmap.Height);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((short)1);
            writer.Write((short)32);
            writer.Write(pngBytes.Length);
            writer.Write(6 + 16);

            writer.Write(pngBytes);
        }

        private static string SanitizeFileName(string input)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                input = input.Replace(c, '_');

            return input.Trim();
        }

        private static string ComputeHash(byte[] data)
        {
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(data);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private void CleanupUnusedIcons()
        {
            try
            {
                string[] usedIcons = GameShortcuts
                    .Where(x => !string.IsNullOrWhiteSpace(x.IconPath))
                    .Select(x => Path.GetFullPath(x.IconPath))
                    .ToArray();

                string[] allIcons = Directory.Exists(Paths.ShortcutIcons)
                    ? Directory.GetFiles(Paths.ShortcutIcons, "*.ico")
                    : Array.Empty<string>();

                foreach (var icon in allIcons)
                {
                    if (!usedIcons.Contains(Path.GetFullPath(icon)))
                    {
                        File.Delete(icon);
                    }
                }
            }
            catch
            {
                // Ignore cleanup issues
            }
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propName);
                return true;
            }

            return false;
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
            set => SetField(ref _gameName, value, nameof(GameName));
        }

        public string GameId
        {
            get => _gameId;
            set => SetField(ref _gameId, value, nameof(GameId));
        }

        public string IconPath
        {
            get => _iconPath;
            set => SetField(ref _iconPath, value, nameof(IconPath));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool SetField<T>(ref T field, T value, string propName)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propName);
                return true;
            }
            return false;
        }
    }

}