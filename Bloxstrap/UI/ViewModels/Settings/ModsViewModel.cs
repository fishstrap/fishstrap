using System.Windows;
using System.Windows.Input;
using System.IO;

using Microsoft.Win32;

using Windows.Win32;
using Windows.Win32.UI.Shell;
using Windows.Win32.Foundation;

using CommunityToolkit.Mvvm.Input;

using Bloxstrap.AppData;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class ModsViewModel : NotifyPropertyChangedViewModel
    {
        private void OpenModsFolder() => Process.Start("explorer.exe", Paths.Modifications);

        private static readonly Dictionary<string, byte[]> FontHeaders = new()
        {
            { "ttf", new byte[] { 0x00, 0x01, 0x00, 0x00 } },
            { "otf", new byte[] { 0x4F, 0x54, 0x54, 0x4F } },
            { "ttc", new byte[] { 0x74, 0x74, 0x63, 0x66 } }
        };

        private void ManageCustomFont()
        {
            if (!string.IsNullOrEmpty(TextFontTask.NewState))
            {
                TextFontTask.NewState = string.Empty;
            }
            else
            {
                var dialog = new OpenFileDialog { Filter = $"{Strings.Menu_FontFiles}|*.ttf;*.otf;*.ttc" };

                if (dialog.ShowDialog() != true) return;

                string type = Path.GetExtension(dialog.FileName).TrimStart('.').ToLowerInvariant();
                byte[] fileHeader = File.ReadAllBytes(dialog.FileName).Take(4).ToArray();

                if (!FontHeaders.TryGetValue(type, out var expectedHeader) || !expectedHeader.SequenceEqual(fileHeader))
                {
                    Frontend.ShowMessageBox("Custom Font Invalid", MessageBoxImage.Error);
                    return;
                }

                TextFontTask.NewState = dialog.FileName;
            }

            OnPropertyChanged(nameof(ChooseCustomFontVisibility));
            OnPropertyChanged(nameof(DeleteCustomFontVisibility));
        }

        public ICommand OpenModsFolderCommand => new RelayCommand(OpenModsFolder);

        public ICommand AddCustomCursorModCommand => new RelayCommand(AddCustomCursorMod);

        public ICommand RemoveCustomCursorModCommand => new RelayCommand(RemoveCustomCursorMod);

        public Visibility ChooseCustomFontVisibility => !String.IsNullOrEmpty(TextFontTask.NewState) ? Visibility.Collapsed : Visibility.Visible;

        public Visibility DeleteCustomFontVisibility => !String.IsNullOrEmpty(TextFontTask.NewState) ? Visibility.Visible : Visibility.Collapsed;

        public ICommand ManageCustomFontCommand => new RelayCommand(ManageCustomFont);

        public ICommand OpenCompatSettingsCommand => new RelayCommand(OpenCompatSettings);

        public ModPresetTask OldDeathSoundTask { get; } = new("OldDeathSound", @"content\sounds\ouch.ogg", "Sounds.OldDeath.ogg");

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
            },
            {
                Enums.CursorType.BlackAndWhiteCross, new()
                {
                    { @"content\textures\Cursors\KeyboardMouse\ArrowCursor.png",    "Cursor.BlackAndWhiteCross.ArrowCursor.png"    },
                    { @"content\textures\Cursors\KeyboardMouse\ArrowFarCursor.png", "Cursor.BlackAndWhiteCross.ArrowFarCursor.png" },
                    { @"content\textures\Cursors\KeyboardMouse\IBeamCursor.png", "Cursor.BlackAndWhiteCross.IBeamCursor.png" }
                }
            },
            {
                Enums.CursorType.BlackAndWhiteDot, new()
                {
                    { @"content\textures\Cursors\KeyboardMouse\ArrowCursor.png",    "Cursor.BlackAndWhiteDot.ArrowCursor.png"    },
                    { @"content\textures\Cursors\KeyboardMouse\ArrowFarCursor.png", "Cursor.BlackAndWhiteDot.ArrowFarCursor.png" },
                    { @"content\textures\Cursors\KeyboardMouse\IBeamCursor.png", "Cursor.BlackAndWhiteDot.IBeamCursor.png" }
                }
            },
            {
                Enums.CursorType.PurpleCross, new()
                {
                    { @"content\textures\Cursors\KeyboardMouse\ArrowCursor.png",    "Cursor.PurpleCross.ArrowCursor.png"    },
                    { @"content\textures\Cursors\KeyboardMouse\ArrowFarCursor.png", "Cursor.PurpleCross.ArrowFarCursor.png" },
                    { @"content\textures\Cursors\KeyboardMouse\IBeamCursor.png", "Cursor.PurpleCross.IBeamCursor.png" }
                }
            }
        });

        public FontModPresetTask TextFontTask { get; } = new();

        private void OpenCompatSettings()
        {
            string path = new RobloxPlayerData().ExecutablePath;

            if (File.Exists(path))
                PInvoke.SHObjectProperties(HWND.Null, SHOP_TYPE.SHOP_FILEPATH, path, "Compatibility");
            else
                Frontend.ShowMessageBox(Strings.Common_RobloxNotInstalled, MessageBoxImage.Error);

        }

        public Visibility ChooseCustomCursorVisibility
        {
            get
            {
                string targetDir = Path.Combine(Paths.Modifications, "Content", "textures", "Cursors", "KeyboardMouse");
                string[] cursorNames = { "ArrowCursor.png", "ArrowFarCursor.png", "IBeamCursor.png" };
                bool anyExist = cursorNames.Any(name => File.Exists(Path.Combine(targetDir, name)));
                return anyExist ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public Visibility DeleteCustomCursorVisibility
        {
            get
            {
                string targetDir = Path.Combine(Paths.Modifications, "Content", "textures", "Cursors", "KeyboardMouse");
                string[] cursorNames = { "ArrowCursor.png", "ArrowFarCursor.png", "IBeamCursor.png" };
                bool anyExist = cursorNames.Any(name => File.Exists(Path.Combine(targetDir, name)));
                return anyExist ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void AddCustomCursorMod()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "PNG Images (*.png)|*.png",
                Title = "Select a PNG Cursor Image"
            };

            if (dialog.ShowDialog() != true)
                return;

            string sourcePath = dialog.FileName;
            string targetDir = Path.Combine(Paths.Modifications, "Content", "textures", "Cursors", "KeyboardMouse");
            Directory.CreateDirectory(targetDir);

            string[] cursorNames = { "ArrowCursor.png", "ArrowFarCursor.png", "IBeamCursor.png" };

            try
            {
                foreach (var name in cursorNames)
                {
                    string destPath = Path.Combine(targetDir, name);
                    File.Copy(sourcePath, destPath, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(
                    $"Failed to add cursors:\n{ex.Message}",
                    MessageBoxImage.Error
                );

            }

            OnPropertyChanged(nameof(ChooseCustomCursorVisibility));
            OnPropertyChanged(nameof(DeleteCustomCursorVisibility));
        }

        public void RemoveCustomCursorMod()
        {
            string targetDir = Path.Combine(Paths.Modifications, "Content", "textures", "Cursors", "KeyboardMouse");
            string[] cursorNames = { "ArrowCursor.png", "ArrowFarCursor.png", "IBeamCursor.png" };

            bool anyDeleted = false;
            foreach (var name in cursorNames)
            {
                string filePath = Path.Combine(targetDir, name);
                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                        anyDeleted = true;
                    }
                    catch (Exception ex)
                    {
                        Frontend.ShowMessageBox(
                            $"Failed to remove {name}:\n{ex.Message}",
                            MessageBoxImage.Error
                        );

                    }
                }
            }

            if (!anyDeleted)
                Frontend.ShowMessageBox(
                    "No custom cursors found to remove.",
                    MessageBoxImage.Information
                );

            OnPropertyChanged(nameof(ChooseCustomCursorVisibility));
            OnPropertyChanged(nameof(DeleteCustomCursorVisibility));
        }
    }
}
