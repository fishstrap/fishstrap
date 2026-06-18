using Bloxstrap.AppData;
using Bloxstrap.Models.SettingTasks;
using CommunityToolkit.Mvvm.Input;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Wpf.Ui.Mvvm.Interfaces;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class ModsViewModel : NotifyPropertyChangedViewModel
    {
        public ModsViewModel()
        {
            LoadInstalledMods();
        }

        private void OpenModsFolder() => Process.Start("explorer.exe", Paths.Modifications);

        private readonly Dictionary<string, byte[]> FontHeaders = new()
        {
            { "ttf", new byte[4] { 0x00, 0x01, 0x00, 0x00 } },
            { "otf", new byte[4] { 0x4F, 0x54, 0x54, 0x4F } },
            { "ttc", new byte[4] { 0x74, 0x74, 0x63, 0x66 } }
        };

        private void ManageCustomFont()
        {
            if (!String.IsNullOrEmpty(TextFontTask.NewState))
            {
                TextFontTask.NewState = "";
            }
            else
            {
                var dialog = new OpenFileDialog
                {
                    Filter = $"{Strings.Menu_FontFiles}|*.ttf;*.otf;*.ttc"
                };

                if (dialog.ShowDialog() != true)
                    return;

                string type = dialog.FileName.Substring(dialog.FileName.Length-3, 3).ToLowerInvariant();

                if (!FontHeaders.ContainsKey(type)
                    || !FontHeaders.Any(x => File.ReadAllBytes(dialog.FileName).Take(4).SequenceEqual(x.Value)))
                {
                    Frontend.ShowMessageBox(Strings.Menu_Mods_Misc_CustomFont_Invalid, MessageBoxImage.Error);
                    return;
                }

                TextFontTask.NewState = dialog.FileName;
            }

            OnPropertyChanged(nameof(ChooseCustomFontVisibility));
            OnPropertyChanged(nameof(DeleteCustomFontVisibility));
        }

        public ICommand OpenModsFolderCommand => new RelayCommand(OpenModsFolder);

        public Visibility ChooseCustomFontVisibility => !String.IsNullOrEmpty(TextFontTask.NewState) ? Visibility.Collapsed : Visibility.Visible;

        public Visibility DeleteCustomFontVisibility => !String.IsNullOrEmpty(TextFontTask.NewState) ? Visibility.Visible : Visibility.Collapsed;

        public ICommand ManageCustomFontCommand => new RelayCommand(ManageCustomFont);

        public ICommand OpenCompatSettingsCommand => new RelayCommand(OpenCompatSettings);

        public ICommand ImportModCommand => new RelayCommand(ImportMod);

        public ModPresetTask OldAvatarBackgroundTask { get; } = new("OldAvatarBackground", @"ExtraContent\places\Mobile.rbxl", "OldAvatarBackground.rbxl");

        public ModPresetTask OldCharacterSoundsTask { get; } = new("OldCharacterSounds", new()
        {
            { @"content\sounds\action_footsteps_plastic.mp3", "Sounds.OldWalk.mp3"  },
            { @"content\sounds\action_jump.mp3",              "Sounds.OldJump.mp3"  },
            { @"content\sounds\action_get_up.mp3",            "Sounds.OldGetUp.mp3" },
            { @"content\sounds\action_falling.mp3",           "Sounds.Empty.mp3"    },
            { @"content\sounds\action_jump_land.mp3",         "Sounds.Empty.mp3"    },
            { @"content\sounds\action_swim.mp3",              "Sounds.Empty.mp3"    },
            { @"content\sounds\impact_water.mp3",             "Sounds.Empty.mp3"    }
        });

        public EmojiModPresetTask EmojiFontTask { get; } = new();

        public EnumModPresetTask<Enums.CursorType> CursorTypeTask { get; } = new("CursorType", new()
        {
            {
                Enums.CursorType.From2006, new()
                {
                    { @"content\textures\Cursors\KeyboardMouse\ArrowCursor.png",    "Cursor.From2006.ArrowCursor.png"    },
                    { @"content\textures\Cursors\KeyboardMouse\ArrowFarCursor.png", "Cursor.From2006.ArrowFarCursor.png" }
                }
            },
            {
                Enums.CursorType.From2013, new()
                {
                    { @"content\textures\Cursors\KeyboardMouse\ArrowCursor.png",    "Cursor.From2013.ArrowCursor.png"    },
                    { @"content\textures\Cursors\KeyboardMouse\ArrowFarCursor.png", "Cursor.From2013.ArrowFarCursor.png" }
                }
            }
        });

        public FontModPresetTask TextFontTask { get; } = new();


        public ObservableCollection<InstalledMod> InstalledMods { get; } = new();

        public ICommand DeleteModCommand => new RelayCommand<InstalledMod>(DeleteMod);

        public ICommand MoveModUpCommand => new RelayCommand<InstalledMod>(MoveModUp);

        public ICommand MoveModDownCommand => new RelayCommand<InstalledMod>(MoveModDown);

        public void LoadInstalledMods()
        {
            foreach (var mod in InstalledMods)
                mod.PropertyChanged -= OnModPropertyChanged;
            InstalledMods.Clear();

            if (!App.RobloxState.Loaded)
                return;

            foreach (var mod in App.RobloxState.Prop.InstalledMods.OrderBy(m => m.LoadOrder))
            {
                mod.PropertyChanged += OnModPropertyChanged;
                InstalledMods.Add(mod);
            }
        }

        private void OnModPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            SaveInstalledMods();
        }

        private void SaveInstalledMods()
        {
            // doesnt work without it set to false so yeah...
            App.RobloxState.Load(alertFailure: false);

            App.RobloxState.Prop.InstalledMods = InstalledMods.ToList();
            App.RobloxState.Save();
        }

        // required folders that a valid mod zip must contain at root (or one level deeper as a safeguard)
        private static readonly HashSet<string> RequiredModFolders = new()
        {
            "ClientSettings",
            "content",
            "ExtraContent",
            "PlatformContent"
        };

        private static string? ValidateModStructure(string rootDir)
        {
            // check root
            foreach (string folder in RequiredModFolders)
            {
                if (Directory.Exists(Path.Combine(rootDir, folder)))
                    return rootDir;
            }

            // one level deeper
            var subDirs = Directory.GetDirectories(rootDir);
            foreach (string subDir in subDirs)
            {
                foreach (string folder in RequiredModFolders)
                {
                    if (Directory.Exists(Path.Combine(subDir, folder)))
                        return subDir;
                }
            }

            return null;
        }

        private void ImportMod()
        {
            const string LOG_IDENT = "ModsViewModel::ImportMod";

            var dialog = new OpenFileDialog
            {
                Filter = Strings.Menu_Mods_ImportMod_FileFilter
            };

            if (dialog.ShowDialog() != true)
                return;

            string filePath = dialog.FileName;
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (extension != ".zip")
            {
                Frontend.ShowMessageBox(Strings.Menu_Mods_ImportMod_InvalidFileType, MessageBoxImage.Error);
                return;
            }

            string modName = Path.GetFileNameWithoutExtension(filePath);

            if (InstalledMods.Any(m => String.Equals(m.Name, modName, StringComparison.OrdinalIgnoreCase)))
            {
                Frontend.ShowMessageBox(string.Format(Strings.Menu_Mods_ImportMod_AlreadyInstalled, modName), MessageBoxImage.Warning);
                return;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), $"fishstrap_mod_import_{Guid.NewGuid():N}");
            string finalModDir = Path.Combine(Paths.Modifications, modName);

            try
            {
                Directory.CreateDirectory(tempDir);

                var fastZip = new FastZip();
                fastZip.ExtractZip(filePath, tempDir, null);

                string? contentRoot = ValidateModStructure(tempDir);

                if (contentRoot is null)
                {
                    Frontend.ShowMessageBox(Strings.Menu_Mods_ImportMod_InvalidStructure, MessageBoxImage.Error);
                    return;
                }

                var files = new List<string>();
                foreach (string file in Directory.GetFiles(contentRoot, "*.*", SearchOption.AllDirectories))
                {
                    string relativeFile = file.Substring(contentRoot.Length + 1);
                    files.Add(relativeFile);
                }

                if (files.Count == 0)
                {
                    Frontend.ShowMessageBox(Strings.Menu_Mods_ImportMod_NoFiles, MessageBoxImage.Error);
                    return;
                }

                if (contentRoot != tempDir)
                {
                    Directory.CreateDirectory(finalModDir);

                    foreach (string file in Directory.GetFiles(contentRoot, "*.*", SearchOption.AllDirectories))
                    {
                        string relativeFile = file.Substring(contentRoot.Length + 1);
                        string destFile = Path.Combine(finalModDir, relativeFile);
                        Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                        File.Move(file, destFile);
                    }
                }
                else
                {
                    Directory.Move(tempDir, finalModDir);
                    Directory.CreateDirectory(tempDir);
                }

                int loadOrder = InstalledMods.Any() ? InstalledMods.Max(m => m.LoadOrder) + 1 : 0;

                var installedMod = new InstalledMod
                {
                    Name = modName,
                    Enabled = true,
                    LoadOrder = loadOrder,
                    InstalledAt = DateTime.UtcNow,
                    Files = files,
                    SourcePath = filePath
                };

                installedMod.PropertyChanged += OnModPropertyChanged;
                InstalledMods.Add(installedMod);
                SaveInstalledMods();

                OnPropertyChanged(nameof(InstalledMods));
            }
            catch (ZipException ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Invalid zip file: {ex.Message}");
                Frontend.ShowMessageBox(string.Format(Strings.Menu_Mods_ImportMod_InvalidZip, ex.Message), MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to import mod: {ex.Message}");
                App.Logger.WriteException(LOG_IDENT, ex);

                Frontend.ShowMessageBox(string.Format(Strings.Menu_Mods_ImportMod_Failed, ex.Message), MessageBoxImage.Error);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }

                if (!InstalledMods.Any(m => String.Equals(m.Name, modName, StringComparison.OrdinalIgnoreCase))
                    && Directory.Exists(finalModDir))
                {
                    try { Directory.Delete(finalModDir, true); } catch { }
                }
            }
        }

        private void DeleteMod(InstalledMod? mod)
        {
            const string LOG_IDENT = "ModsViewModel::DeleteMod";

            if (mod is null)
                return;

            var result = Frontend.ShowMessageBox(
                string.Format(Strings.Menu_Mods_DeleteMod_Confirm, mod.Name),
                MessageBoxImage.Warning,
                MessageBoxButton.YesNo);

            if (result != MessageBoxResult.Yes)
                return;

            string modDir = Path.Combine(Paths.Modifications, mod.Name);
            if (Directory.Exists(modDir))
            {
                try { Directory.Delete(modDir, true); } catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Failed to delete mod directory: {ex.Message}");
                }
            }

            mod.PropertyChanged -= OnModPropertyChanged;
            InstalledMods.Remove(mod);
            SaveInstalledMods();
            OnPropertyChanged(nameof(InstalledMods));
        }

        private void MoveModUp(InstalledMod? mod)
        {
            if (mod is null)
                return;

            int currentIndex = InstalledMods.IndexOf(mod);
            if (currentIndex <= 0)
                return;

            var above = InstalledMods[currentIndex - 1];

            (mod.LoadOrder, above.LoadOrder) = (above.LoadOrder, mod.LoadOrder);

            InstalledMods.Move(currentIndex, currentIndex - 1);

            SaveInstalledMods();
        }

        private void MoveModDown(InstalledMod? mod)
        {
            if (mod is null)
                return;

            int currentIndex = InstalledMods.IndexOf(mod);
            if (currentIndex < 0 || currentIndex >= InstalledMods.Count - 1)
                return;

            var below = InstalledMods[currentIndex + 1];

            (mod.LoadOrder, below.LoadOrder) = (below.LoadOrder, mod.LoadOrder);

            InstalledMods.Move(currentIndex, currentIndex + 1);

            SaveInstalledMods();
        }

        private void OpenCompatSettings()
        {
            string path = new RobloxPlayerData().ExecutablePath;

            if (File.Exists(path))
                PInvoke.SHObjectProperties(HWND.Null, SHOP_TYPE.SHOP_FILEPATH, path, "Compatibility");
            else
                Frontend.ShowMessageBox(Strings.Common_RobloxNotInstalled, MessageBoxImage.Error);

        }
    }
}
