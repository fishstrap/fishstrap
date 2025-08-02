using Bloxstrap.UI.Elements.Dialogs;
using Bloxstrap.UI.Elements.Editor;
using Bloxstrap.UI.Elements.Settings;
using CommunityToolkit.Mvvm.Input;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class AppearanceViewModel : NotifyPropertyChangedViewModel
    {
        private readonly Page _page;

        public ICommand PreviewBootstrapperCommand => new RelayCommand(PreviewBootstrapper);
        public ICommand BrowseCustomIconLocationCommand => new RelayCommand(BrowseCustomIconLocation);

        public ICommand AddCustomThemeCommand => new RelayCommand(AddCustomTheme);
        public ICommand DeleteCustomThemeCommand => new RelayCommand(DeleteCustomTheme);
        public ICommand RenameCustomThemeCommand => new RelayCommand(RenameCustomTheme);
        public ICommand EditCustomThemeCommand => new RelayCommand(EditCustomTheme);
        public ICommand ExportCustomThemeCommand => new RelayCommand(ExportCustomTheme);
        public ICommand ManageCustomFontCommand => new RelayCommand<string>(ManageCustomFont!);

        private void PreviewBootstrapper()
        {
            var app = (App.Current as App);
            app?._froststrapRPC?.UpdatePresence("Dialog: Preview Launcher");

            IBootstrapperDialog dialog = App.Settings.Prop.BootstrapperStyle.GetNew();

            if (App.Settings.Prop.BootstrapperStyle == BootstrapperStyle.ByfronDialog)
                dialog.Message = Strings.Bootstrapper_StylePreview_ImageCancel;
            else
                dialog.Message = Strings.Bootstrapper_StylePreview_TextCancel;

            dialog.CancelEnabled = true;
            dialog.ShowBootstrapper();

            app?._froststrapRPC?.UpdatePresence("Page: Appearance");
        }


        public bool IsCustomFontApplied => FontManager.IsCustomFontApplied;

        public Visibility ChooseCustomFontVisibility => IsCustomFontApplied ? Visibility.Collapsed : Visibility.Visible;

        public Visibility DeleteCustomFontVisibility => IsCustomFontApplied ? Visibility.Visible : Visibility.Collapsed;

        private void UpdateFontVisibility()
        {
            OnPropertyChanged(nameof(IsCustomFontApplied));
            OnPropertyChanged(nameof(ChooseCustomFontVisibility));
            OnPropertyChanged(nameof(DeleteCustomFontVisibility));
        }

        private void ManageCustomFont(string action)
        {
            if (action == "Choose")
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Font files (*.ttf;*.otf)|*.ttf;*.otf|All files (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    string fontPath = dialog.FileName;
                    try
                    {
                        var fontFamily = FontManager.LoadFontFromFile(fontPath);
                        if (fontFamily != null)
                        {
                            FontManager.ApplyFontGlobally(fontFamily);
                            App.Settings.Prop.CustomFontPath = fontPath;
                            App.Settings.Save();

                            UpdateFontVisibility();

                            foreach (Window window in Application.Current.Windows)
                            {
                                window.FontFamily = fontFamily;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to load font: {ex.Message}", "Font Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else if (action == "Remove")
            {
                FontManager.RemoveCustomFont();
                UpdateFontVisibility();

                var defaultFont = new System.Windows.Media.FontFamily("Segoe UI");
                foreach (Window window in Application.Current.Windows)
                {
                    window.FontFamily = defaultFont;
                }
            }
        }

        public void ApplySavedCustomFont()
        {
            bool applied = FontManager.ApplySavedCustomFont();
            UpdateFontVisibility();

            if (applied)
            {
                var fontFamily = FontManager.LoadFontFromFile(App.Settings.Prop.CustomFontPath!);
                if (fontFamily != null)
                {
                    foreach (Window window in Application.Current.Windows)
                        window.FontFamily = fontFamily;
                }
            }
        }

        private void BrowseCustomIconLocation()
        {
            var dialog = new OpenFileDialog
            {
                Filter = $"{Strings.Menu_IconFiles}|*.ico"
            };

            if (dialog.ShowDialog() != true)
                return;

            CustomIconLocation = dialog.FileName;
            OnPropertyChanged(nameof(CustomIconLocation));
        }

        private bool _hasInitializedCustomGradient = false;

        public AppearanceViewModel(Page page)
        {
            _page = page;

            foreach (var entry in BootstrapperIconEx.Selections)
                Icons.Add(new BootstrapperIconEntry { IconType = entry });

            PopulateCustomThemes();
            ApplySavedCustomFont();
            UpdateFontVisibility();

            if (App.Settings.Prop.CustomGradientStops != null)
            {
                foreach (var stop in App.Settings.Prop.CustomGradientStops)
                    GradientStops.Add(stop);
            }

            GradientStartPointX = App.Settings.Prop.GradientStartPoint.X != 0 ? App.Settings.Prop.GradientStartPoint.X : 1;
            GradientStartPointY = App.Settings.Prop.GradientStartPoint.Y != 0 ? App.Settings.Prop.GradientStartPoint.Y : 1;
            GradientEndPointX = App.Settings.Prop.GradientEndPoint.X != 0 ? App.Settings.Prop.GradientEndPoint.X : 0;
            GradientEndPointY = App.Settings.Prop.GradientEndPoint.Y != 0 ? App.Settings.Prop.GradientEndPoint.Y : 0;

            _backgroundMode = App.Settings.Prop.BackgroundMode;
            _imageBackgroundPath = App.Settings.Prop.ImageBackgroundPath;
            _backgroundImageStretch = App.Settings.Prop.BackgroundImageStretch;

            OnPropertyChanged(nameof(BackgroundMode));
            OnPropertyChanged(nameof(ImageBackgroundPath));
            OnPropertyChanged(nameof(BackgroundImageStretch));
            OnPropertyChanged(nameof(IsGradientMode));
            OnPropertyChanged(nameof(IsImageMode));

            if (_backgroundMode == CustomBackgroundMode.Image &&
                !string.IsNullOrWhiteSpace(_imageBackgroundPath) &&
                File.Exists(_imageBackgroundPath))
            {
                LoadPreviewImage(_imageBackgroundPath);
            }
            else
            {
                BackgroundPreviewImageSource = null;
            }

            GradientStops.CollectionChanged += (s, e) => { /* optionally handle changes */ };

            UpdateLivePreviewBrush();

            _blackOverlayOpacity = App.Settings.Prop.BlackOverlayOpacity;
            UpdateBlackOverlayBrush();
            OnPropertyChanged(nameof(BlackOverlayOpacity));
        }

        public ObservableCollection<GradientStopData> GradientStops { get; } = new();

        public IEnumerable<Theme> Themes { get; } = Enum.GetValues(typeof(Theme)).Cast<Theme>();

        public Theme Theme
        {
            get => App.Settings.Prop.Theme;
            set
            {
                if (App.Settings.Prop.Theme != value)
                {
                    App.Settings.Prop.Theme = value;

                    if (value == Theme.Custom && !_hasInitializedCustomGradient)
                    {
                        _hasInitializedCustomGradient = true;

                        var defaultStops = new List<GradientStopData>
                        {
                            new GradientStopData { Offset = 0.0, Color = "#4D5560" },
                            new GradientStopData { Offset = 0.5, Color = "#383F47" },
                            new GradientStopData { Offset = 1.0, Color = "#252A30" },
                        };

                        App.Settings.Prop.CustomGradientStops = defaultStops;

                        GradientStops.Clear();
                        foreach (var stop in defaultStops)
                        {
                            GradientStops.Add(stop);
                            stop.PropertyChanged += (s2, e2) => UpdateLivePreviewBrush();
                        }

                        UpdateLivePreviewBrush();
                    }

                    ((MainWindow)Window.GetWindow(_page)!).ApplyTheme();

                    OnPropertyChanged(nameof(Theme));
                    OnPropertyChanged(nameof(CustomGlobalThemeExpanded));
                }
            }
        }

        public bool CustomGlobalThemeExpanded => App.Settings.Prop.Theme == Theme.Custom;

        private Brush _livePreviewBrush = Brushes.Transparent;
        public Brush LivePreviewBrush
        {
            get => _livePreviewBrush;
            set
            {
                _livePreviewBrush = value;
                OnPropertyChanged(nameof(LivePreviewBrush));
            }
        }

        public void ResetGradientStops()
        {
            GradientStops.Clear();

            var defaultStops = new List<GradientStopData>
            {
                new GradientStopData { Offset = 0.0, Color = "#4D5560" },
                new GradientStopData { Offset = 0.5, Color = "#383F47" },
                new GradientStopData { Offset = 1.0, Color = "#252A30" },
            };

            foreach (var stop in defaultStops)
            {
                GradientStops.Add(stop);
                stop.PropertyChanged += (s, e) => UpdateLivePreviewBrush();
            }

            App.Settings.Prop.CustomGradientStops = GradientStops.ToList();

            UpdateLivePreviewBrush();
        }

        private void UpdateLivePreviewBrush()
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(1, 1),
                EndPoint = new Point(0, 0)
            };

            foreach (var stop in GradientStops.OrderBy(s => s.Offset))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(stop.Color);
                    brush.GradientStops.Add(new GradientStop(color, stop.Offset));
                }
                catch
                {
                    // Skip invalid stops
                }
            }

            LivePreviewBrush = brush;
        }

        private double _gradientStartPointX = 1;
        public double GradientStartPointX
        {
            get => _gradientStartPointX;
            set
            {
                if (_gradientStartPointX != value)
                {
                    _gradientStartPointX = value;
                    OnPropertyChanged(nameof(GradientStartPointX));
                    UpdateGradientPoints();
                }
            }
        }

        private double _gradientStartPointY = 1;
        public double GradientStartPointY
        {
            get => _gradientStartPointY;
            set
            {
                if (_gradientStartPointY != value)
                {
                    _gradientStartPointY = value;
                    OnPropertyChanged(nameof(GradientStartPointY));
                    UpdateGradientPoints();
                }
            }
        }

        private double _gradientEndPointX = 0;
        public double GradientEndPointX
        {
            get => _gradientEndPointX;
            set
            {
                if (_gradientEndPointX != value)
                {
                    _gradientEndPointX = value;
                    OnPropertyChanged(nameof(GradientEndPointX));
                    UpdateGradientPoints();
                }
            }
        }

        private double _gradientEndPointY = 0;
        public double GradientEndPointY
        {
            get => _gradientEndPointY;
            set
            {
                if (_gradientEndPointY != value)
                {
                    _gradientEndPointY = value;
                    OnPropertyChanged(nameof(GradientEndPointY));
                    UpdateGradientPoints();
                }
            }
        }

        private void UpdateGradientPoints()
        {
            App.Settings.Prop.GradientStartPoint = new Point(GradientStartPointX, GradientStartPointY);
            App.Settings.Prop.GradientEndPoint = new Point(GradientEndPointX, GradientEndPointY);

            ((MainWindow)Window.GetWindow(_page)!).ApplyTheme();
        }

        private CustomBackgroundMode _backgroundMode = CustomBackgroundMode.Gradient;
        public CustomBackgroundMode BackgroundMode
        {
            get => _backgroundMode;
            set
            {
                if (_backgroundMode != value)
                {
                    _backgroundMode = value;
                    App.Settings.Prop.BackgroundMode = value;
                    App.Settings.Save();

                    OnPropertyChanged(nameof(BackgroundMode));
                    OnPropertyChanged(nameof(IsGradientMode));
                    OnPropertyChanged(nameof(IsImageMode));

                    if (_backgroundMode == CustomBackgroundMode.Image)
                    {
                        if (!string.IsNullOrWhiteSpace(ImageBackgroundPath) && File.Exists(ImageBackgroundPath))
                            LoadPreviewImage(ImageBackgroundPath);
                        else
                            BackgroundPreviewImageSource = null;
                    }
                    else
                    {
                        BackgroundPreviewImageSource = null;
                    }

                    ((MainWindow)Window.GetWindow(_page)!)?.ApplyTheme();
                }
            }
        }

        public bool IsGradientMode
        {
            get => BackgroundMode == CustomBackgroundMode.Gradient;
            set
            {
                if (value)
                    BackgroundMode = CustomBackgroundMode.Gradient;
            }
        }

        public bool IsImageMode
        {
            get => BackgroundMode == CustomBackgroundMode.Image;
            set
            {
                if (value)
                    BackgroundMode = CustomBackgroundMode.Image;
            }
        }

        private string _imageBackgroundPath = string.Empty;
        public string ImageBackgroundPath
        {
            get => _imageBackgroundPath;
            set
            {
                if (_imageBackgroundPath != value)
                {
                    _imageBackgroundPath = value;
                    App.Settings.Prop.ImageBackgroundPath = value;
                    App.Settings.Save();

                    OnPropertyChanged(nameof(ImageBackgroundPath));

                    if (_backgroundMode == CustomBackgroundMode.Image && File.Exists(value))
                    {
                        LoadPreviewImage(value);
                        ((MainWindow)Window.GetWindow(_page)!)?.ApplyTheme();
                    }
                    else
                    {
                        BackgroundPreviewImageSource = null;
                    }
                }
            }
        }

        private ImageSource? _backgroundPreviewImageSource;
        public ImageSource? BackgroundPreviewImageSource
        {
            get => _backgroundPreviewImageSource;
            set
            {
                _backgroundPreviewImageSource = value;
                OnPropertyChanged(nameof(BackgroundPreviewImageSource));
            }
        }

        private double _blackOverlayOpacity = 0.2; // default 20%

        public double BlackOverlayOpacity
        {
            get => _blackOverlayOpacity;
            set
            {
                if (_blackOverlayOpacity != value)
                {
                    _blackOverlayOpacity = value;
                    App.Settings.Prop.BlackOverlayOpacity = value;
                    App.Settings.Save();
                    OnPropertyChanged(nameof(BlackOverlayOpacity));
                    UpdateBlackOverlayBrush();
                }
            }
        }

        private void UpdateBlackOverlayBrush()
        {
            var color = Color.FromArgb(
                (byte)(_blackOverlayOpacity * 255), // alpha
                0, 0, 0); // black

            var brush = new SolidColorBrush(color);
            brush.Freeze();

            Application.Current.Resources["WindowBackgroundBlackOverlay"] = brush;
        }

        public IEnumerable<BackgroundImageStretchMode> BackgroundImageStretchModes
            => Enum.GetValues(typeof(BackgroundImageStretchMode)).Cast<BackgroundImageStretchMode>();

        private BackgroundImageStretchMode _backgroundImageStretch;
        public BackgroundImageStretchMode BackgroundImageStretch
        {
            get => _backgroundImageStretch;
            set
            {
                if (_backgroundImageStretch != value)
                {
                    _backgroundImageStretch = value;
                    App.Settings.Prop.BackgroundImageStretch = value;
                    App.Settings.Save();

                    OnPropertyChanged(nameof(BackgroundImageStretch));
                    ((MainWindow)Window.GetWindow(_page)!)?.ApplyTheme();
                }
            }
        }

        public void BrowseImage()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.gif)|*.png;*.jpg;*.jpeg;*.gif|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                string appDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Froststrap", "Backgrounds");
                Directory.CreateDirectory(appDataFolder);

                string destFile = Path.Combine(appDataFolder, "background" + Path.GetExtension(openFileDialog.FileName));
                File.Copy(openFileDialog.FileName, destFile, overwrite: true);

                App.Settings.Prop.ImageBackgroundPath = destFile;
                App.Settings.Prop.BackgroundMode = CustomBackgroundMode.Image;
                App.Settings.Save();

                LoadPreviewImage(destFile);
                ((MainWindow)Window.GetWindow(_page)!).ApplyTheme();
            }
        }

        public void LoadPreviewImage(string path)
        {
            if (App.Settings.Prop.BackgroundMode != CustomBackgroundMode.Image)
                return;

            if (File.Exists(path))
            {
                try
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    image.UriSource = new Uri(path);
                    image.EndInit();
                    image.Freeze();

                    BackgroundPreviewImageSource = image;
                }
                catch (IOException)
                {
                    MessageBox.Show(
                        "The selected image is currently in use by another process. Please close any apps using it and try again.",
                        "Image Load Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
            }
            else
            {
                BackgroundPreviewImageSource = null;
            }
        }

        public void ClearBackgroundImage()
        {
            App.Settings.Prop.ImageBackgroundPath = string.Empty;
            App.Settings.Prop.BackgroundMode = CustomBackgroundMode.Gradient;
            App.Settings.Save();
            BackgroundPreviewImageSource = null;
            ((MainWindow)Window.GetWindow(_page)!).ApplyTheme();
        }

        public static List<string> Languages => Locale.GetLanguages();

        public string SelectedLanguage
        {
            get => Locale.SupportedLocales[App.Settings.Prop.Locale];
            set => App.Settings.Prop.Locale = Locale.GetIdentifierFromName(value);
        }

        public string DownloadingStatus
        {
            get => App.Settings.Prop.DownloadingStringFormat;
            set => App.Settings.Prop.DownloadingStringFormat = value;
        }

        public IEnumerable<BootstrapperStyle> Dialogs { get; } = BootstrapperStyleEx.Selections;

        public BootstrapperStyle Dialog
        {
            get => App.Settings.Prop.BootstrapperStyle;
            set
            {
                App.Settings.Prop.BootstrapperStyle = value;
                OnPropertyChanged(nameof(CustomThemesExpanded)); // TODO: only fire when needed
            }
        }

        public bool CustomThemesExpanded => App.Settings.Prop.BootstrapperStyle == BootstrapperStyle.CustomDialog;

        public ObservableCollection<BootstrapperIconEntry> Icons { get; set; } = new();

        public BootstrapperIcon Icon
        {
            get => App.Settings.Prop.BootstrapperIcon;
            set => App.Settings.Prop.BootstrapperIcon = value;
        }

        public string Title
        {
            get => App.Settings.Prop.BootstrapperTitle;
            set => App.Settings.Prop.BootstrapperTitle = value;
        }

        public string CustomIconLocation
        {
            get => App.Settings.Prop.BootstrapperIconCustomLocation;
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    if (App.Settings.Prop.BootstrapperIcon == BootstrapperIcon.IconCustom)
                        App.Settings.Prop.BootstrapperIcon = BootstrapperIcon.IconBloxstrap;
                }
                else
                {
                    App.Settings.Prop.BootstrapperIcon = BootstrapperIcon.IconCustom;
                }

                App.Settings.Prop.BootstrapperIconCustomLocation = value;

                OnPropertyChanged(nameof(Icon));
                OnPropertyChanged(nameof(Icons));
            }
        }

        private void DeleteCustomThemeStructure(string name)
        {
            string dir = Path.Combine(Paths.CustomThemes, name);
            Directory.Delete(dir, true);
        }

        private void RenameCustomThemeStructure(string oldName, string newName)
        {
            string oldDir = Path.Combine(Paths.CustomThemes, oldName);
            string newDir = Path.Combine(Paths.CustomThemes, newName);
            Directory.Move(oldDir, newDir);
        }

        private void AddCustomTheme()
        {
            (App.Current as App)?._froststrapRPC?.UpdatePresence("Dialog: Add Custom Launcher");
            var dialog = new AddCustomThemeDialog();
            dialog.ShowDialog();

            (App.Current as App)?._froststrapRPC?.UpdatePresence("Page: Appearance");

            if (dialog.Created)
            {
                CustomThemes.Add(dialog.ThemeName);
                SelectedCustomThemeIndex = CustomThemes.Count - 1;

                OnPropertyChanged(nameof(SelectedCustomThemeIndex));
                OnPropertyChanged(nameof(IsCustomThemeSelected));

                if (dialog.OpenEditor)
                    EditCustomTheme();
            }
        }

        private void DeleteCustomTheme()
        {
            if (SelectedCustomTheme is null)
                return;

            try
            {
                DeleteCustomThemeStructure(SelectedCustomTheme);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("AppearanceViewModel::DeleteCustomTheme", ex);
                Frontend.ShowMessageBox(string.Format(Strings.Menu_Appearance_CustomThemes_DeleteFailed, SelectedCustomTheme, ex.Message), MessageBoxImage.Error);
                return;
            }

            CustomThemes.Remove(SelectedCustomTheme);

            if (CustomThemes.Any())
            {
                SelectedCustomThemeIndex = CustomThemes.Count - 1;
                OnPropertyChanged(nameof(SelectedCustomThemeIndex));
            }

            OnPropertyChanged(nameof(IsCustomThemeSelected));
        }

        private void RenameCustomTheme()
        {
            const string LOG_IDENT = "AppearanceViewModel::RenameCustomTheme";

            if (SelectedCustomTheme is null || SelectedCustomTheme == SelectedCustomThemeName)
                return;

            if (string.IsNullOrEmpty(SelectedCustomThemeName))
            {
                Frontend.ShowMessageBox(Strings.CustomTheme_Add_Errors_NameEmpty, MessageBoxImage.Error);
                return;
            }

            var validationResult = PathValidator.IsFileNameValid(SelectedCustomThemeName);

            if (validationResult != PathValidator.ValidationResult.Ok)
            {
                switch (validationResult)
                {
                    case PathValidator.ValidationResult.IllegalCharacter:
                        Frontend.ShowMessageBox(Strings.CustomTheme_Add_Errors_NameIllegalCharacters, MessageBoxImage.Error);
                        break;
                    case PathValidator.ValidationResult.ReservedFileName:
                        Frontend.ShowMessageBox(Strings.CustomTheme_Add_Errors_NameReserved, MessageBoxImage.Error);
                        break;
                    default:
                        App.Logger.WriteLine(LOG_IDENT, $"Got unhandled PathValidator::ValidationResult {validationResult}");
                        Debug.Assert(false);

                        Frontend.ShowMessageBox(Strings.CustomTheme_Add_Errors_Unknown, MessageBoxImage.Error);
                        break;
                }
                return;
            }

            // better to check for the file instead of the directory so broken themes can be overwritten
            string path = Path.Combine(Paths.CustomThemes, SelectedCustomThemeName, "Theme.xml");
            if (File.Exists(path))
            {
                Frontend.ShowMessageBox(Strings.CustomTheme_Add_Errors_NameTaken, MessageBoxImage.Error);
                return;
            }

            try
            {
                RenameCustomThemeStructure(SelectedCustomTheme, SelectedCustomThemeName);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
                Frontend.ShowMessageBox(string.Format(Strings.Menu_Appearance_CustomThemes_RenameFailed, SelectedCustomTheme, ex.Message), MessageBoxImage.Error);
                return;
            }

            int idx = CustomThemes.IndexOf(SelectedCustomTheme);
            CustomThemes[idx] = SelectedCustomThemeName;

            SelectedCustomThemeIndex = idx;
            OnPropertyChanged(nameof(SelectedCustomThemeIndex));
        }

        private void EditCustomTheme()
        {
            if (SelectedCustomTheme is null)
                return;

            var app = (App.Current as App);
            app?._froststrapRPC?.UpdatePresence("Dialog: Edit Custom Theme");

            new BootstrapperEditorWindow(SelectedCustomTheme).ShowDialog();

            app?._froststrapRPC?.UpdatePresence("Page: Appearance");
        }

        private void ExportCustomTheme()
        {
            if (SelectedCustomTheme is null)
                return;

            var dialog = new SaveFileDialog
            {
                FileName = $"{SelectedCustomTheme}.zip",
                Filter = $"{Strings.FileTypes_ZipArchive}|*.zip"
            };

            if (dialog.ShowDialog() != true)
                return;

            string themeDir = Path.Combine(Paths.CustomThemes, SelectedCustomTheme);

            using var memStream = new MemoryStream();
            using var zipStream = new ZipOutputStream(memStream);

            foreach (var filePath in Directory.EnumerateFiles(themeDir, "*.*", SearchOption.AllDirectories))
            {
                string relativePath = filePath[(themeDir.Length + 1)..];

                var entry = new ZipEntry(relativePath);
                entry.DateTime = DateTime.Now;

                zipStream.PutNextEntry(entry);

                using var fileStream = File.OpenRead(filePath);
                fileStream.CopyTo(zipStream);
            }

            zipStream.CloseEntry();
            zipStream.Finish();
            memStream.Position = 0;

            using var outputStream = File.OpenWrite(dialog.FileName);
            memStream.CopyTo(outputStream);

            Process.Start("explorer.exe", $"/select,\"{dialog.FileName}\"");
        }

        private void PopulateCustomThemes()
        {
            string? selected = App.Settings.Prop.SelectedCustomTheme;

            Directory.CreateDirectory(Paths.CustomThemes);

            foreach (string directory in Directory.GetDirectories(Paths.CustomThemes))
            {
                if (!File.Exists(Path.Combine(directory, "Theme.xml")))
                    continue; // missing the main theme file, ignore

                string name = Path.GetFileName(directory);
                CustomThemes.Add(name);
            }

            if (selected != null)
            {
                int idx = CustomThemes.IndexOf(selected);

                if (idx != -1)
                {
                    SelectedCustomThemeIndex = idx;
                    OnPropertyChanged(nameof(SelectedCustomThemeIndex));
                }
                else
                {
                    SelectedCustomTheme = null;
                }
            }
        }

        public string? SelectedCustomTheme
        {
            get => App.Settings.Prop.SelectedCustomTheme;
            set => App.Settings.Prop.SelectedCustomTheme = value;
        }

        public string SelectedCustomThemeName { get; set; } = "";

        public int SelectedCustomThemeIndex { get; set; }

        public ObservableCollection<string> CustomThemes { get; set; } = new();
        public bool IsCustomThemeSelected => SelectedCustomTheme is not null;
    }
}