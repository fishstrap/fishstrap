using Bloxstrap.AppData;
using CommunityToolkit.Mvvm.Input;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

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

        public ICommand AddCustomShiftlockModCommand => new RelayCommand(AddCustomShiftlockMod);

        public ICommand RemoveCustomShiftlockModCommand => new RelayCommand(RemoveCustomShiftlockMod);
        public ICommand AddCustomDeathSoundCommand => new RelayCommand(AddCustomDeathSound);
        public ICommand RemoveCustomDeathSoundCommand => new RelayCommand(RemoveCustomDeathSound);

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

        private Visibility GetVisibility(string directory, string[] filenames, bool checkExist)
        {
            bool anyExist = filenames.Any(name => File.Exists(Path.Combine(directory, name)));
            return (checkExist ? anyExist : !anyExist) ? Visibility.Visible : Visibility.Collapsed;
        }

        public Visibility ChooseCustomCursorVisibility =>
    GetVisibility(Path.Combine(Paths.Modifications, "Content", "textures", "Cursors", "KeyboardMouse"),
                  new[] { "ArrowCursor.png", "ArrowFarCursor.png", "MouseLockedCursor.png" }, checkExist: false);

        public Visibility DeleteCustomCursorVisibility =>
            GetVisibility(Path.Combine(Paths.Modifications, "Content", "textures", "Cursors", "KeyboardMouse"),
                          new[] { "ArrowCursor.png", "ArrowFarCursor.png", "MouseLockedCursor.png" }, checkExist: true);

        public Visibility ChooseCustomShiftlockVisibility =>
            GetVisibility(Path.Combine(Paths.Modifications, "Content", "textures"),
                          new[] { "MouseLockedCursor.png" }, checkExist: false);

        public Visibility DeleteCustomShiftlockVisibility =>
            GetVisibility(Path.Combine(Paths.Modifications, "Content", "textures"),
                          new[] { "MouseLockedCursor.png" }, checkExist: true);

        public Visibility ChooseCustomDeathSoundVisibility =>
            GetVisibility(Path.Combine(Paths.Modifications, "Content", "sounds"),
                          new[] { "ouch.ogg" }, checkExist: false);

        public Visibility DeleteCustomDeathSoundVisibility =>
            GetVisibility(Path.Combine(Paths.Modifications, "Content", "sounds"),
                          new[] { "ouch.ogg" }, checkExist: true);

        private void AddCustomFile(string[] targetFiles, string targetDir, string dialogTitle, string filter, string failureText, Action postAction = null!)
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                Title = dialogTitle
            };

            if (dialog.ShowDialog() != true)
                return;

            string sourcePath = dialog.FileName;
            Directory.CreateDirectory(targetDir);

            try
            {
                foreach (var name in targetFiles)
                {
                    string destPath = Path.Combine(targetDir, name);
                    File.Copy(sourcePath, destPath, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Failed to add {failureText}:\n{ex.Message}", MessageBoxImage.Error);
                return;
            }

            postAction?.Invoke();
        }

        private void RemoveCustomFile(string[] targetFiles, string targetDir, string notFoundMessage, Action postAction = null!)
        {
            bool anyDeleted = false;

            foreach (var name in targetFiles)
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
                        Frontend.ShowMessageBox($"Failed to remove {name}:\n{ex.Message}", MessageBoxImage.Error);
                    }
                }
            }

            if (!anyDeleted)
            {
                Frontend.ShowMessageBox(notFoundMessage, MessageBoxImage.Information);
            }

            postAction?.Invoke();
        }

        public void AddCustomCursorMod()
        {
            AddCustomFile(
                new[] { "ArrowCursor.png", "ArrowFarCursor.png", "IBeamCursor.png" },
                Path.Combine(Paths.Modifications, "Content", "textures", "Cursors", "KeyboardMouse"),
                "Select a PNG Cursor Image",
                "PNG Images (*.png)|*.png",
                "cursors",
                () =>
                {
                    OnPropertyChanged(nameof(ChooseCustomCursorVisibility));
                    OnPropertyChanged(nameof(DeleteCustomCursorVisibility));
                });
        }

        public void RemoveCustomCursorMod()
        {
            RemoveCustomFile(
                new[] { "ArrowCursor.png", "ArrowFarCursor.png", "IBeamCursor.png" },
                Path.Combine(Paths.Modifications, "Content", "textures", "Cursors", "KeyboardMouse"),
                "No custom cursors found to remove.",
                () =>
                {
                    OnPropertyChanged(nameof(ChooseCustomCursorVisibility));
                    OnPropertyChanged(nameof(DeleteCustomCursorVisibility));
                });
        }

        public void AddCustomShiftlockMod()
        {
            AddCustomFile(
                new[] { "MouseLockedCursor.png" },
                Path.Combine(Paths.Modifications, "Content", "textures"),
                "Select a PNG Shiftlock Image",
                "PNG Images (*.png)|*.png",
                "Shiftlock",
                () =>
                {
                    OnPropertyChanged(nameof(ChooseCustomShiftlockVisibility));
                    OnPropertyChanged(nameof(DeleteCustomShiftlockVisibility));
                });
        }

        public void RemoveCustomShiftlockMod()
        {
            RemoveCustomFile(
                new[] { "MouseLockedCursor.png" },
                Path.Combine(Paths.Modifications, "Content", "textures"),
                "No custom Shiftlock found to remove.",
                () =>
                {
                    OnPropertyChanged(nameof(ChooseCustomShiftlockVisibility));
                    OnPropertyChanged(nameof(DeleteCustomShiftlockVisibility));
                });
        }

        public void AddCustomDeathSound()
        {
            AddCustomFile(
                new[] { "ouch.ogg" },
                Path.Combine(Paths.Modifications, "Content", "sounds"),
                "Select a Custom Death Sound",
                "OGG Audio (*.ogg)|*.ogg",
                "death sound",
                () =>
                {
                    OnPropertyChanged(nameof(ChooseCustomDeathSoundVisibility));
                    OnPropertyChanged(nameof(DeleteCustomDeathSoundVisibility));
                });
        }

        public void RemoveCustomDeathSound()
        {
            RemoveCustomFile(
                new[] { "ouch.ogg" },
                Path.Combine(Paths.Modifications, "Content", "sounds"),
                "No custom death sound found to remove.",
                () =>
                {
                    OnPropertyChanged(nameof(ChooseCustomDeathSoundVisibility));
                    OnPropertyChanged(nameof(DeleteCustomDeathSoundVisibility));
                });
        }


        #region Custom Cursor Set Related Code
        public ObservableCollection<CustomCursorSet> CustomCursorSets { get; } = new();

        private int _selectedCustomCursorSetIndex;
        public int SelectedCustomCursorSetIndex
        {
            get => _selectedCustomCursorSetIndex;
            set
            {
                if (_selectedCustomCursorSetIndex != value)
                {
                    _selectedCustomCursorSetIndex = value;
                    OnPropertyChanged(nameof(SelectedCustomCursorSetIndex));
                    OnPropertyChanged(nameof(SelectedCustomCursorSet));
                    OnPropertyChanged(nameof(IsCustomCursorSetSelected));
                    SelectedCustomCursorSetName = SelectedCustomCursorSet?.Name ?? "";

                    SelectedCustomCursorSetIndex = value;
                    NotifyCursorVisibilities();
                    LoadCursorPathsForSelectedSet();
                }
            }
        }

        public CustomCursorSet? SelectedCustomCursorSet =>
            SelectedCustomCursorSetIndex >= 0 && SelectedCustomCursorSetIndex < CustomCursorSets.Count
                ? CustomCursorSets[SelectedCustomCursorSetIndex]
                : null;

        public bool IsCustomCursorSetSelected => SelectedCustomCursorSet is not null;

        private string _selectedCustomCursorSetName = string.Empty;
        public string SelectedCustomCursorSetName
        {
            get => _selectedCustomCursorSetName;
            set
            {
                if (_selectedCustomCursorSetName != value)
                {
                    _selectedCustomCursorSetName = value;
                    OnPropertyChanged(nameof(SelectedCustomCursorSetName));
                }
            }
        }

        public ICommand AddCustomCursorSetCommand => new RelayCommand(AddCustomCursorSet);
        public ICommand DeleteCustomCursorSetCommand => new RelayCommand(DeleteCustomCursorSet);
        public ICommand RenameCustomCursorSetCommand => new RelayCommand(RenameCustomCursorSet);
        public ICommand ApplyCursorSetCommand => new RelayCommand(ApplyCursorSet);
        public ICommand GetCurrentCursorSetCommand => new RelayCommand(GetCurrentCursorSet);
        public ICommand ExportCursorSetCommand => new RelayCommand(ExportCursorSet);
        public ICommand ImportCursorSetCommand => new RelayCommand(ImportCursorSet);
        public ICommand AddArrowCursorCommand => new RelayCommand(() => AddCursorImage("ArrowCursor.png", "Select Arrow Cursor PNG"));
        public ICommand AddArrowFarCursorCommand => new RelayCommand(() => AddCursorImage("ArrowFarCursor.png", "Select Arrow Far Cursor PNG"));
        public ICommand AddIBeamCursorCommand => new RelayCommand(() => AddCursorImage("IBeamCursor.png", "Select IBeam Cursor PNG"));
        public ICommand AddShiftlockCursorCommand => new RelayCommand(AddShiftlockCursor);
        public ICommand DeleteArrowCursorCommand => new RelayCommand(() => DeleteCursorImage("ArrowCursor.png"));
        public ICommand DeleteArrowFarCursorCommand => new RelayCommand(() => DeleteCursorImage("ArrowFarCursor.png"));
        public ICommand DeleteIBeamCursorCommand => new RelayCommand(() => DeleteCursorImage("IBeamCursor.png"));
        public ICommand DeleteShiftlockCursorCommand => new RelayCommand(() => DeleteCursorImage("MouseLockedCursor.png"));

        public ModsViewModel()
        {
            LoadCustomCursorSets();

            LoadCursorPathsForSelectedSet();

            NotifyCursorVisibilities();
        }
        #endregion

        #region Button Logic
        private void LoadCustomCursorSets()
        {
            CustomCursorSets.Clear();

            if (!Directory.Exists(Paths.CustomCursors))
                Directory.CreateDirectory(Paths.CustomCursors);

            foreach (var dir in Directory.GetDirectories(Paths.CustomCursors))
            {
                var name = Path.GetFileName(dir);

                CustomCursorSets.Add(new CustomCursorSet
                {
                    Name = name,
                    FolderPath = dir
                });
            }

            if (CustomCursorSets.Any())
                SelectedCustomCursorSetIndex = 0;

            OnPropertyChanged(nameof(IsCustomCursorSetSelected));
        }

        private void AddCustomCursorSet()
        {
            string basePath = Paths.CustomCursors;
            int index = 1;
            string newFolderPath;

            do
            {
                string folderName = $"Custom Cursor Set {index}";
                newFolderPath = Path.Combine(basePath, folderName);
                index++;
            }
            while (Directory.Exists(newFolderPath));

            try
            {
                Directory.CreateDirectory(newFolderPath);

                var newSet = new CustomCursorSet
                {
                    Name = Path.GetFileName(newFolderPath),
                    FolderPath = newFolderPath
                };

                CustomCursorSets.Add(newSet);
                SelectedCustomCursorSetIndex = CustomCursorSets.Count - 1;
                OnPropertyChanged(nameof(IsCustomCursorSetSelected));
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ModsViewModel::AddCustomCursorSet", ex);
                Frontend.ShowMessageBox($"Failed to create cursor set:\n{ex.Message}", MessageBoxImage.Error);
            }
        }

        private void DeleteCustomCursorSet()
        {
            if (SelectedCustomCursorSet is null)
                return;

            try
            {
                if (Directory.Exists(SelectedCustomCursorSet.FolderPath))
                    Directory.Delete(SelectedCustomCursorSet.FolderPath, true);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ModsViewModel::DeleteCustomCursorSet", ex);
                Frontend.ShowMessageBox($"Failed to delete cursor set:\n{ex.Message}", MessageBoxImage.Error);
                return;
            }

            CustomCursorSets.Remove(SelectedCustomCursorSet);

            if (CustomCursorSets.Any())
            {
                SelectedCustomCursorSetIndex = CustomCursorSets.Count - 1;
                OnPropertyChanged(nameof(SelectedCustomCursorSet));
            }

            OnPropertyChanged(nameof(IsCustomCursorSetSelected));
        }

        private void RenameCustomCursorSetStructure(string oldName, string newName)
        {
            string oldDir = Path.Combine(Paths.CustomCursors, oldName);
            string newDir = Path.Combine(Paths.CustomCursors, newName);

            if (Directory.Exists(newDir))
                throw new IOException("A folder with the new name already exists.");

            Directory.Move(oldDir, newDir);
        }

        private void RenameCustomCursorSet()
        {
            const string LOG_IDENT = "ModsViewModel::RenameCustomCursorSet";

            if (SelectedCustomCursorSet is null || SelectedCustomCursorSet.Name == SelectedCustomCursorSetName)
                return;

            if (string.IsNullOrWhiteSpace(SelectedCustomCursorSetName))
            {
                Frontend.ShowMessageBox("Name cannot be empty.", MessageBoxImage.Error);
                return;
            }

            var validationResult = PathValidator.IsFileNameValid(SelectedCustomCursorSetName);

            if (validationResult != PathValidator.ValidationResult.Ok)
            {
                string msg = validationResult switch
                {
                    PathValidator.ValidationResult.IllegalCharacter => "Name contains illegal characters.",
                    PathValidator.ValidationResult.ReservedFileName => "Name is reserved.",
                    _ => "Unknown validation error."
                };

                App.Logger.WriteLine(LOG_IDENT, $"Validation result: {validationResult}");
                Frontend.ShowMessageBox(msg, MessageBoxImage.Error);
                return;
            }

            try
            {
                RenameCustomCursorSetStructure(SelectedCustomCursorSet.Name, SelectedCustomCursorSetName);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
                Frontend.ShowMessageBox($"Failed to rename:\n{ex.Message}", MessageBoxImage.Error);
                return;
            }

            int idx = CustomCursorSets.IndexOf(SelectedCustomCursorSet);
            CustomCursorSets[idx] = new CustomCursorSet
            {
                Name = SelectedCustomCursorSetName,
                FolderPath = Path.Combine(Paths.CustomCursors, SelectedCustomCursorSetName)
            };

            SelectedCustomCursorSetIndex = idx;
            OnPropertyChanged(nameof(SelectedCustomCursorSetIndex));
        }

        private void ApplyCursorSet()
        {
            if (SelectedCustomCursorSet is null)
            {
                Frontend.ShowMessageBox("Please select a cursor set first.", MessageBoxImage.Warning);
                return;
            }

            string sourceDir = SelectedCustomCursorSet.FolderPath;
            string targetDir = Path.Combine(Paths.Modifications, "content", "textures");
            string targetKeyboardMouse = Path.Combine(targetDir, "Cursors", "KeyboardMouse");

            try
            {
                if (!Directory.Exists(sourceDir))
                {
                    Frontend.ShowMessageBox("Selected cursor set folder does not exist.", MessageBoxImage.Error);
                    return;
                }

                Directory.CreateDirectory(targetDir);
                Directory.CreateDirectory(targetKeyboardMouse);

                var filesToDelete = new[]
                {
                    Path.Combine(targetDir, "MouseLockedCursor.png"),
                    Path.Combine(targetKeyboardMouse, "ArrowCursor.png"),
                    Path.Combine(targetKeyboardMouse, "ArrowFarCursor.png"),
                    Path.Combine(targetKeyboardMouse, "IBeamCursor.png")
                };

                foreach (var file in filesToDelete)
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }

                foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(sourceDir, file);
                    string destPath = Path.Combine(targetDir, relativePath);

                    Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                    File.Copy(file, destPath, overwrite: true);
                }

                Frontend.ShowMessageBox($"Cursor set '{SelectedCustomCursorSet.Name}' applied successfully!", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ModsViewModel::ApplyCursorSet", ex);
                Frontend.ShowMessageBox($"Failed to apply cursor set:\n{ex.Message}", MessageBoxImage.Error);
            }

            LoadCursorPathsForSelectedSet();

            OnPropertyChanged(nameof(ChooseCustomShiftlockVisibility));
            OnPropertyChanged(nameof(DeleteCustomShiftlockVisibility));
            OnPropertyChanged(nameof(ChooseCustomCursorVisibility));
            OnPropertyChanged(nameof(DeleteCustomCursorVisibility));
        }

        private void GetCurrentCursorSet()
        {
            if (SelectedCustomCursorSet is null)
            {
                Frontend.ShowMessageBox("Please select a cursor set first.", MessageBoxImage.Warning);
                return;
            }

            string sourceMouseLocked = Path.Combine(Paths.Modifications, "content", "textures", "MouseLockedCursor.png");
            string sourceKeyboardMouse = Path.Combine(Paths.Modifications, "content", "textures", "Cursors", "KeyboardMouse");

            string targetBase = SelectedCustomCursorSet.FolderPath;
            string targetMouseLocked = Path.Combine(targetBase, "MouseLockedCursor.png");
            string targetKeyboardMouse = Path.Combine(targetBase, "Cursors", "KeyboardMouse");

            try
            {
                Directory.CreateDirectory(targetBase);
                Directory.CreateDirectory(targetKeyboardMouse);

                var filesToDelete = new[]
                {
                    targetMouseLocked,
                    Path.Combine(targetKeyboardMouse, "ArrowCursor.png"),
                    Path.Combine(targetKeyboardMouse, "ArrowFarCursor.png"),
                    Path.Combine(targetKeyboardMouse, "IBeamCursor.png")
                };

                foreach (var file in filesToDelete)
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }

                if (File.Exists(sourceMouseLocked))
                    File.Copy(sourceMouseLocked, targetMouseLocked, overwrite: true);

                if (Directory.Exists(sourceKeyboardMouse))
                {
                    foreach (var fileName in new[] { "ArrowCursor.png", "ArrowFarCursor.png", "IBeamCursor.png" })
                    {
                        string source = Path.Combine(sourceKeyboardMouse, fileName);
                        string dest = Path.Combine(targetKeyboardMouse, fileName);

                        if (File.Exists(source))
                            File.Copy(source, dest, overwrite: true);
                    }
                }

                Frontend.ShowMessageBox("Current cursor set copied into selected folder.", MessageBoxImage.Information);
                NotifyCursorVisibilities();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ModsViewModel::GetCurrentCursorSet", ex);
                Frontend.ShowMessageBox($"Failed to get current cursor set:\n{ex.Message}", MessageBoxImage.Error);
            }

            LoadCursorPathsForSelectedSet();
            NotifyCursorVisibilities();
        }

        private void ExportCursorSet()
        {
            if (SelectedCustomCursorSet is null)
                return;

            var dialog = new SaveFileDialog
            {
                FileName = $"{SelectedCustomCursorSet.Name}.zip",
                Filter = $"{Strings.FileTypes_ZipArchive}|*.zip"
            };

            if (dialog.ShowDialog() != true)
                return;

            string cursorDir = SelectedCustomCursorSet.FolderPath;

            try
            {
                using var memStream = new MemoryStream();
                using var zipStream = new ZipOutputStream(memStream);

                foreach (var filePath in Directory.EnumerateFiles(cursorDir, "*.*", SearchOption.AllDirectories))
                {
                    string relativePath = filePath[(cursorDir.Length + 1)..].Replace('\\', '/');

                    var entry = new ZipEntry(relativePath)
                    {
                        DateTime = DateTime.Now,
                        Size = new FileInfo(filePath).Length
                    };

                    zipStream.PutNextEntry(entry);

                    using var fileStream = File.OpenRead(filePath);
                    fileStream.CopyTo(zipStream);

                    zipStream.CloseEntry();
                }

                zipStream.Finish();
                memStream.Position = 0;

                using var outputStream = File.OpenWrite(dialog.FileName);
                memStream.CopyTo(outputStream);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ModsViewModel::ExportCursorSet", ex);
                Frontend.ShowMessageBox($"Failed to export cursor set:\n{ex.Message}", MessageBoxImage.Error);
                return;
            }

            Process.Start("explorer.exe", $"/select,\"{dialog.FileName}\"");
        }

        private void ImportCursorSet()
        {
            if (SelectedCustomCursorSet is null)
            {
                Frontend.ShowMessageBox("Please select a cursor set first.", MessageBoxImage.Warning);
                return;
            }

            var dialog = new OpenFileDialog
            {
                Title = "Import Cursor Set",
                Filter = $"{Strings.FileTypes_ZipArchive}|*.zip",
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempPath);

                ExtractZipToDirectory(dialog.FileName, tempPath);

                string mouseLockedDest = Path.Combine(SelectedCustomCursorSet.FolderPath, "MouseLockedCursor.png");
                string destKeyboardMouseFolder = Path.Combine(SelectedCustomCursorSet.FolderPath, "Cursors", "KeyboardMouse");

                if (File.Exists(mouseLockedDest))
                    File.Delete(mouseLockedDest);

                foreach (var fileName in new[] { "ArrowCursor.png", "ArrowFarCursor.png", "IBeamCursor.png" })
                {
                    string filePath = Path.Combine(destKeyboardMouseFolder, fileName);
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }

                string? mouseLockedSource = Directory.GetFiles(tempPath, "MouseLockedCursor.png", SearchOption.AllDirectories).FirstOrDefault();

                if (mouseLockedSource != null)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(mouseLockedDest)!);
                    File.Copy(mouseLockedSource, mouseLockedDest, overwrite: true);
                }

                Directory.CreateDirectory(destKeyboardMouseFolder);

                foreach (var fileName in new[] { "ArrowCursor.png", "ArrowFarCursor.png", "IBeamCursor.png" })
                {
                    string? sourceFile = Directory.GetFiles(tempPath, fileName, SearchOption.AllDirectories).FirstOrDefault();
                    if (sourceFile != null)
                    {
                        string destFile = Path.Combine(destKeyboardMouseFolder, fileName);
                        File.Copy(sourceFile, destFile, overwrite: true);
                    }
                }

                Directory.Delete(tempPath, recursive: true);

                Frontend.ShowMessageBox("Cursor set imported successfully.", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ModsViewModel::ImportCursorSet", ex);
                Frontend.ShowMessageBox($"Failed to import cursor set:\n{ex.Message}", MessageBoxImage.Error);
            }

            LoadCursorPathsForSelectedSet();
        }

        private void ExtractZipToDirectory(string zipFilePath, string extractPath)
        {
            using var zipInputStream = new ZipInputStream(File.OpenRead(zipFilePath));

            ZipEntry? entry;
            while ((entry = zipInputStream.GetNextEntry()) != null)
            {
                if (entry.IsDirectory)
                    continue;

                string filePath = Path.Combine(extractPath, entry.Name);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                using var outputStream = File.Create(filePath);
                zipInputStream.CopyTo(outputStream);
            }
        }

        private string? GetCursorTargetPath(string fileName)
        {
            if (SelectedCustomCursorSet is null)
                return null;

            string dir = fileName == "MouseLockedCursor.png"
                ? SelectedCustomCursorSet.FolderPath
                : Path.Combine(SelectedCustomCursorSet.FolderPath, "Cursors", "KeyboardMouse");

            Directory.CreateDirectory(dir);
            return Path.Combine(dir, fileName);
        }

        private void DeleteCursorImage(string fileName)
        {
            string? destPath = GetCursorTargetPath(fileName);
            if (destPath is null || !File.Exists(destPath))
                return;

            try
            {
                File.Delete(destPath);

                UpdateCursorPathProperty(fileName, "");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException($"ModsViewModel::Delete{fileName}", ex);
                Frontend.ShowMessageBox($"Failed to delete {fileName}:\n{ex.Message}", MessageBoxImage.Error);
            }

            LoadCursorPathsForSelectedSet();
            NotifyCursorVisibilities();

            OnPropertyChanged(nameof(ChooseCustomShiftlockVisibility));
            OnPropertyChanged(nameof(DeleteCustomShiftlockVisibility));
            OnPropertyChanged(nameof(ChooseCustomCursorVisibility));
            OnPropertyChanged(nameof(DeleteCustomCursorVisibility));
        }

        private void AddShiftlockCursor()
        {
            AddCursorImage("MouseLockedCursor.png", "Select Shiftlock PNG");
            OnPropertyChanged(nameof(ChooseCustomShiftlockVisibility));
            OnPropertyChanged(nameof(DeleteCustomShiftlockVisibility));
            OnPropertyChanged(nameof(ChooseCustomCursorVisibility));
            OnPropertyChanged(nameof(DeleteCustomCursorVisibility));
        }

        private void AddCursorImage(string fileName, string dialogTitle)
        {
            if (SelectedCustomCursorSet is null)
            {
                Frontend.ShowMessageBox("Please select a cursor set first.", MessageBoxImage.Warning);
                return;
            }

            var dialog = new OpenFileDialog
            {
                Title = dialogTitle,
                Filter = "PNG files (*.png)|*.png",
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
                return;

            string? destPath = GetCursorTargetPath(fileName);
            if (destPath is null)
                return;

            try
            {
                if (File.Exists(destPath))
                    File.Delete(destPath);

                File.Copy(dialog.FileName, destPath);
                UpdateCursorPathAndPreview(fileName, dialog.FileName);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException($"ModsViewModel::Add{fileName}", ex);
                Frontend.ShowMessageBox($"Failed to add {fileName}:\n{ex.Message}", MessageBoxImage.Error);
            }

            LoadCursorPathsForSelectedSet();
            NotifyCursorVisibilities();

            OnPropertyChanged(nameof(ChooseCustomShiftlockVisibility));
            OnPropertyChanged(nameof(DeleteCustomShiftlockVisibility));
            OnPropertyChanged(nameof(ChooseCustomCursorVisibility));
            OnPropertyChanged(nameof(DeleteCustomCursorVisibility));
        }
#endregion

        #region Preview Images
        private void UpdateCursorPathProperty(string fileName, string path)
        {
            switch (fileName)
            {
                case "MouseLockedCursor.png":
                    ShiftlockCursorSelectedPath = path;
                    break;
                case "ArrowCursor.png":
                    ArrowCursorSelectedPath = path;
                    break;
                case "ArrowFarCursor.png":
                    ArrowFarCursorSelectedPath = path;
                    break;
                case "IBeamCursor.png":
                    IBeamCursorSelectedPath = path;
                    break;
            }
        }

        private void UpdateCursorPathAndPreview(string fileName, string fullPath)
        {
            if (!File.Exists(fullPath))
                fullPath = "";

            ImageSource? image = LoadImageSafely(fullPath);

            switch (fileName)
            {
                case "MouseLockedCursor.png":
                    ShiftlockCursorSelectedPath = fullPath;
                    ShiftlockCursorPreview = image;
                    App.Settings.Prop.ShiftlockCursorSelectedPath = fullPath;
                    break;

                case "ArrowCursor.png":
                    ArrowCursorSelectedPath = fullPath;
                    ArrowCursorPreview = image;
                    App.Settings.Prop.ArrowCursorSelectedPath = fullPath;
                    break;

                case "ArrowFarCursor.png":
                    ArrowFarCursorSelectedPath = fullPath;
                    ArrowFarCursorPreview = image;
                    App.Settings.Prop.ArrowFarCursorSelectedPath = fullPath;
                    break;

                case "IBeamCursor.png":
                    IBeamCursorSelectedPath = fullPath;
                    IBeamCursorPreview = image;
                    App.Settings.Prop.IBeamCursorSelectedPath = fullPath;
                    break;
            }

            App.Settings.Save();
        }

        private void LoadCursorPathsForSelectedSet()
        {
            if (SelectedCustomCursorSet == null)
            {
                UpdateCursorPathAndPreview("MouseLockedCursor.png", "");
                UpdateCursorPathAndPreview("ArrowCursor.png", "");
                UpdateCursorPathAndPreview("ArrowFarCursor.png", "");
                UpdateCursorPathAndPreview("IBeamCursor.png", "");
                return;
            }

            string baseDir = SelectedCustomCursorSet.FolderPath;
            string kbMouseDir = Path.Combine(baseDir, "Cursors", "KeyboardMouse");

            UpdateCursorPathAndPreview("MouseLockedCursor.png", Path.Combine(baseDir, "MouseLockedCursor.png"));
            UpdateCursorPathAndPreview("ArrowCursor.png", Path.Combine(kbMouseDir, "ArrowCursor.png"));
            UpdateCursorPathAndPreview("ArrowFarCursor.png", Path.Combine(kbMouseDir, "ArrowFarCursor.png"));
            UpdateCursorPathAndPreview("IBeamCursor.png", Path.Combine(kbMouseDir, "IBeamCursor.png"));
        }

        private string _shiftlockCursorSelectedPath = "";
        public string ShiftlockCursorSelectedPath
        {
            get => _shiftlockCursorSelectedPath;
            set
            {
                if (_shiftlockCursorSelectedPath != value)
                {
                    _shiftlockCursorSelectedPath = value;
                    OnPropertyChanged(nameof(ShiftlockCursorSelectedPath));
                }
            }
        }

        private string _arrowCursorSelectedPath = "";
        public string ArrowCursorSelectedPath
        {
            get => _arrowCursorSelectedPath;
            set
            {
                if (_arrowCursorSelectedPath != value)
                {
                    _arrowCursorSelectedPath = value;
                    OnPropertyChanged(nameof(ArrowCursorSelectedPath));
                }
            }
        }

        private string _arrowFarCursorSelectedPath = "";
        public string ArrowFarCursorSelectedPath
        {
            get => _arrowFarCursorSelectedPath;
            set
            {
                if (_arrowFarCursorSelectedPath != value)
                {
                    _arrowFarCursorSelectedPath = value;
                    OnPropertyChanged(nameof(ArrowFarCursorSelectedPath));
                }
            }
        }

        private string _iBeamCursorSelectedPath = "";
        public string IBeamCursorSelectedPath
        {
            get => _iBeamCursorSelectedPath;
            set
            {
                if (_iBeamCursorSelectedPath != value)
                {
                    _iBeamCursorSelectedPath = value;
                    OnPropertyChanged(nameof(IBeamCursorSelectedPath));
                }
            }
        }

        private ImageSource? _shiftlockCursorPreview;
        public ImageSource? ShiftlockCursorPreview
        {
            get => _shiftlockCursorPreview;
            set { _shiftlockCursorPreview = value; OnPropertyChanged(nameof(ShiftlockCursorPreview)); }
        }

        private ImageSource? _arrowCursorPreview;
        public ImageSource? ArrowCursorPreview
        {
            get => _arrowCursorPreview;
            set { _arrowCursorPreview = value; OnPropertyChanged(nameof(ArrowCursorPreview)); }
        }

        private ImageSource? _arrowFarCursorPreview;
        public ImageSource? ArrowFarCursorPreview
        {
            get => _arrowFarCursorPreview;
            set { _arrowFarCursorPreview = value; OnPropertyChanged(nameof(ArrowFarCursorPreview)); }
        }

        private ImageSource? _iBeamCursorPreview;
        public ImageSource? IBeamCursorPreview
        {
            get => _iBeamCursorPreview;
            set { _iBeamCursorPreview = value; OnPropertyChanged(nameof(IBeamCursorPreview)); }
        }

        private static BitmapImage LoadImageSafely(string path)
        {
            if (!File.Exists(path))
                return null!;

            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null!;
            }
        }
#endregion

        #region Button Visibility
        public Visibility AddShiftlockCursorVisibility => GetCursorAddVisibility("MouseLockedCursor.png");
        public Visibility DeleteShiftlockCursorVisibility => GetCursorDeleteVisibility("MouseLockedCursor.png");
        public Visibility AddArrowCursorVisibility => GetCursorAddVisibility("ArrowCursor.png");
        public Visibility DeleteArrowCursorVisibility => GetCursorDeleteVisibility("ArrowCursor.png");
        public Visibility AddArrowFarCursorVisibility => GetCursorAddVisibility("ArrowFarCursor.png");
        public Visibility DeleteArrowFarCursorVisibility => GetCursorDeleteVisibility("ArrowFarCursor.png");
        public Visibility AddIBeamCursorVisibility => GetCursorAddVisibility("IBeamCursor.png");
        public Visibility DeleteIBeamCursorVisibility => GetCursorDeleteVisibility("IBeamCursor.png");

        private Visibility GetCursorAddVisibility(string fileName)
        {
            string? path = GetCursorTargetPath(fileName);
            return path is not null && File.Exists(path) ? Visibility.Collapsed : Visibility.Visible;
        }

        private Visibility GetCursorDeleteVisibility(string fileName)
        {
            string? path = GetCursorTargetPath(fileName);
            return path is not null && File.Exists(path) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void NotifyCursorVisibilities()
        {
            OnPropertyChanged(nameof(AddShiftlockCursorVisibility));
            OnPropertyChanged(nameof(DeleteShiftlockCursorVisibility));
            OnPropertyChanged(nameof(AddArrowCursorVisibility));
            OnPropertyChanged(nameof(DeleteArrowCursorVisibility));
            OnPropertyChanged(nameof(AddArrowFarCursorVisibility));
            OnPropertyChanged(nameof(DeleteArrowFarCursorVisibility));
            OnPropertyChanged(nameof(AddIBeamCursorVisibility));
            OnPropertyChanged(nameof(DeleteIBeamCursorVisibility));
        }
        #endregion
    }
}
