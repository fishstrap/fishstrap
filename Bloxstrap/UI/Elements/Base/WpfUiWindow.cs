using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Mvvm.Contracts;
using Wpf.Ui.Mvvm.Services;

namespace Bloxstrap.UI.Elements.Base
{
    public abstract class WpfUiWindow : UiWindow
    {
        private readonly IThemeService _themeService = new ThemeService();

        private System.Windows.Controls.Image? _gifImage;
        private Uri? _currentImageUri;
        private bool _currentIsGif;
        private Brush? _currentBackgroundBrush;

        public WpfUiWindow()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public void ApplyTheme()
        {
            const int customThemeIndex = 2;

            var finalTheme = App.Settings.Prop.Theme.GetFinal();
            _themeService.SetTheme(finalTheme == Enums.Theme.Light ? ThemeType.Light : ThemeType.Dark);
            _themeService.SetSystemAccent();

            _gifImage ??= FindAnimatedGifImageControl();

            if (App.Settings.Prop.Theme == Enums.Theme.Custom)
            {
                if (App.Settings.Prop.BackgroundMode == CustomBackgroundMode.Image &&
                    !string.IsNullOrWhiteSpace(App.Settings.Prop.ImageBackgroundPath) &&
                    File.Exists(App.Settings.Prop.ImageBackgroundPath))
                {
                    try
                    {
                        var uri = new Uri(App.Settings.Prop.ImageBackgroundPath, UriKind.Absolute);
                        var isGif = string.Equals(Path.GetExtension(uri.LocalPath), ".gif", StringComparison.OrdinalIgnoreCase);

                        if (isGif)
                        {
                            SetBackgroundBrush(null);
                            EnsureGifVisible(uri);
                        }
                        else
                        {
                            EnsureGifHidden();
                            SetBackgroundImage(uri);
                        }

                        SetImageModeThemeColors();
                        SetBlackOverlay(App.Settings.Prop.BlackOverlayOpacity);
                    }
                    catch
                    {
                        EnsureGifHidden();
                        SetBackgroundBrush(null);
                        SetBlackOverlay(0);
                    }
                }
                else
                {
                    EnsureGifHidden();
                    SetBackgroundBrush(CreateCustomGradientBrush());
                    SetGradientModeThemeColors();
                    SetBlackOverlay(0);
                }

                EnsureMergedDictionary(customThemeIndex, null);
            }
            else
            {
                EnsureGifHidden();
                SetBackgroundBrush(null);
                ClearThemeOverrides();
                SetBlackOverlay(0);

                var dict = new ResourceDictionary
                {
                    Source = new Uri($"pack://application:,,,/UI/Style/{Enum.GetName(finalTheme)}.xaml")
                };
                EnsureMergedDictionary(customThemeIndex, dict);
            }

#if QA_BUILD
            BorderBrush = Brushes.Red;
            BorderThickness = new Thickness(4);
#endif
        }

        private void EnsureMergedDictionary(int index, ResourceDictionary? dict)
        {
            var md = Application.Current.Resources.MergedDictionaries;
            while (md.Count <= index)
                md.Add(new ResourceDictionary());
            md[index] = dict ?? new ResourceDictionary();
        }

        private void EnsureGifVisible(Uri uri)
        {
            if (_gifImage == null)
                return;

            if (_gifImage.Visibility != Visibility.Visible)
                _gifImage.Visibility = Visibility.Visible;

            var desiredStretch = App.Settings.Prop.BackgroundImageStretch switch
            {
                BackgroundImageStretchMode.Fill => Stretch.Fill,
                BackgroundImageStretchMode.Uniform => Stretch.Uniform,
                BackgroundImageStretchMode.UniformToFill => Stretch.UniformToFill,
                _ => Stretch.Fill
            };
            if (_gifImage.Stretch != desiredStretch)
                _gifImage.Stretch = desiredStretch;

            var current = XamlAnimatedGif.AnimationBehavior.GetSourceUri(_gifImage);
            if (!_currentIsGif || _currentImageUri != uri || current != uri)
            {
                try
                {
                    XamlAnimatedGif.AnimationBehavior.SetSourceUri(_gifImage, uri);
                }
                catch { /* ignore */ }
                _currentImageUri = uri;
                _currentIsGif = true;
            }

            RenderOptions.SetBitmapScalingMode(_gifImage, BitmapScalingMode.HighQuality);
        }

        private void EnsureGifHidden()
        {
            if (_gifImage == null)
                return;

            if (_gifImage.Visibility != Visibility.Collapsed)
                _gifImage.Visibility = Visibility.Collapsed;

            if (_currentIsGif)
            {
                try
                {
                    XamlAnimatedGif.AnimationBehavior.SetSourceUri(_gifImage, null);
                }
                catch { /* ignore */ }
                _currentIsGif = false;
            }
        }

        private void SetBackgroundImage(Uri uri)
        {
            if (_currentBackgroundBrush is ImageBrush existing &&
                _currentImageUri == uri && !_currentIsGif)
            {
                return;
            }

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bmp.UriSource = uri;
            bmp.EndInit();
            if (bmp.CanFreeze) bmp.Freeze();

            var stretch = App.Settings.Prop.BackgroundImageStretch switch
            {
                BackgroundImageStretchMode.Fill => Stretch.Fill,
                BackgroundImageStretchMode.Uniform => Stretch.Uniform,
                BackgroundImageStretchMode.UniformToFill => Stretch.UniformToFill,
                _ => Stretch.Fill
            };

            var brush = new ImageBrush(bmp)
            {
                Stretch = stretch,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };
            if (brush.CanFreeze) brush.Freeze();

            SetBackgroundBrush(brush);

            _currentImageUri = uri;
            _currentIsGif = false;
        }

        private void SetBackgroundBrush(Brush? brush)
        {
            if (ReferenceEquals(_currentBackgroundBrush, brush))
                return;

            _currentBackgroundBrush = brush;
            Application.Current.Resources["WindowBackgroundGradient"] = brush;

            if (brush == null &&
                Application.Current.Resources["WindowBackgroundGradient"] is ImageBrush ib)
            {
                ib.ImageSource = null;
            }
        }

        private LinearGradientBrush CreateCustomGradientBrush()
        {
            if (App.Settings.Prop.CustomGradientStops == null || App.Settings.Prop.CustomGradientStops.Count == 0)
            {
                App.Settings.Prop.CustomGradientStops = new()
                {
                    new GradientStopData { Offset = 0.0, Color = "#4D5560" },
                    new GradientStopData { Offset = 0.5, Color = "#383F47" },
                    new GradientStopData { Offset = 1.0, Color = "#252A30" }
                };
            }

            var startPoint = App.Settings.Prop.GradientStartPoint == default ? new Point(1, 1) : App.Settings.Prop.GradientStartPoint;
            var endPoint = App.Settings.Prop.GradientEndPoint == default ? new Point(0, 0) : App.Settings.Prop.GradientEndPoint;

            var brush = new LinearGradientBrush
            {
                StartPoint = startPoint,
                EndPoint = endPoint
            };

            foreach (var stop in App.Settings.Prop.CustomGradientStops.OrderBy(s => s.Offset))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(stop.Color);
                    brush.GradientStops.Add(new GradientStop(color, stop.Offset));
                }
                catch { /* ignore invalid color */ }
            }

            if (brush.CanFreeze) brush.Freeze();
            return brush;
        }

        private void SetBlackOverlay(double opacity0to1)
        {
            var alpha = (byte)Math.Max(0, Math.Min(255, (int)(opacity0to1 * 255)));
            var brush = new SolidColorBrush(Color.FromArgb(alpha, 0, 0, 0));
            if (brush.CanFreeze) brush.Freeze();
            Application.Current.Resources["WindowBackgroundBlackOverlay"] = brush;
        }

        private static void SetImageModeThemeColors()
        {
            SetSolidBrush("NewTextEditorBackground", "#2D2D2D");
            SetSolidBrush("NewTextEditorForeground", Colors.White);
            SetSolidBrush("NewTextEditorLink", "#3A9CEA");
            SetSolidBrush("PrimaryBackgroundColor", "#0FFFFFFF");
            SetSolidBrush("NormalDarkAndLightBackground", "#0FFFFFFF");
        }

        private static void SetGradientModeThemeColors()
        {
            SetSolidBrush("NewTextEditorBackground", "#59000000");
            SetSolidBrush("NewTextEditorForeground", Colors.White);
            SetSolidBrush("NewTextEditorLink", "#3A9CEA");
            SetSolidBrush("PrimaryBackgroundColor", "#19000000");
            SetSolidBrush("NormalDarkAndLightBackground", "#0FFFFFFF");

            Application.Current.Resources["ControlFillColorDefault"] =
                (Color)ColorConverter.ConvertFromString("#19000000");
        }

        private static void ClearThemeOverrides()
        {
            Application.Current.Resources.Remove("NewTextEditorBackground");
            Application.Current.Resources.Remove("NewTextEditorForeground");
            Application.Current.Resources.Remove("NewTextEditorLink");
            Application.Current.Resources.Remove("PrimaryBackgroundColor");
            Application.Current.Resources.Remove("NormalDarkAndLightBackground");
            Application.Current.Resources.Remove("ControlFillColorDefault");
            Application.Current.Resources.Remove("WindowBackgroundBlackOverlay");
        }

        private static void SetSolidBrush(string key, string hex)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            SetSolidBrush(key, color);
        }

        private static void SetSolidBrush(string key, Color color)
        {
            var brush = new SolidColorBrush(color);
            if (brush.CanFreeze) brush.Freeze();
            Application.Current.Resources[key] = brush;
        }

        private System.Windows.Controls.Image? FindAnimatedGifImageControl()
        {
            foreach (Window window in Application.Current.Windows)
            {
                var gifImage = FindElementByName<System.Windows.Controls.Image>(window, "AnimatedGifBackground");
                if (gifImage != null)
                    return gifImage;
            }
            return null;
        }

        private static T? FindElementByName<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            if (parent == null) return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T element && element.Name == name)
                    return element;

                var result = FindElementByName<T>(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            ApplyTheme();
            Loaded -= OnLoaded;
        }

        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            Unloaded -= OnUnloaded;

            if (_gifImage != null)
            {
                try { XamlAnimatedGif.AnimationBehavior.SetSourceUri(_gifImage, null); } catch { }
                _gifImage.Source = null;
                _gifImage = null;
            }

            if (Application.Current.Resources["WindowBackgroundGradient"] is ImageBrush ib)
                ib.ImageSource = null;

            Application.Current.Resources["WindowBackgroundGradient"] = null;
            Application.Current.Resources.Remove("WindowBackgroundBlackOverlay");
            ClearThemeOverrides();

            _currentBackgroundBrush = null;
            _currentImageUri = null;
            _currentIsGif = false;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            if (App.Settings.Prop.WPFSoftwareRender || App.LaunchSettings.NoGPUFlag.Active)
            {
                if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
                    hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
            }

            var fontPath = App.Settings.Prop.CustomFontPath;
            if (!string.IsNullOrWhiteSpace(fontPath) && File.Exists(fontPath))
            {
                var font = FontManager.LoadFontFromFile(fontPath);
                if (font != null)
                    FontFamily = font;
            }
        }
    }
}