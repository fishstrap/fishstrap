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

        public WpfUiWindow()
        {
            this.Loaded += WpfUiWindow_Loaded;
        }

        public void ApplyTheme()
        {
            const int customThemeIndex = 2;

            var finalTheme = App.Settings.Prop.Theme.GetFinal();
            _themeService.SetTheme(finalTheme == Enums.Theme.Light ? ThemeType.Light : ThemeType.Dark);
            _themeService.SetSystemAccent();

            if (App.Settings.Prop.Theme == Enums.Theme.Custom)
            {
                if (App.Settings.Prop.BackgroundMode == CustomBackgroundMode.Image &&
                    !string.IsNullOrWhiteSpace(App.Settings.Prop.ImageBackgroundPath) &&
                    File.Exists(App.Settings.Prop.ImageBackgroundPath))
                {
                    try
                    {
                        var uri = new Uri(App.Settings.Prop.ImageBackgroundPath, UriKind.Absolute);
                        var extension = Path.GetExtension(uri.LocalPath).ToLowerInvariant();
                        var isAnimatedGif = extension == ".gif";

                        if (isAnimatedGif)
                        {
                            Application.Current.Resources["WindowBackgroundGradient"] = null;

                            if (TryFindAnimatedGifImageControl(out var gifImage))
                            {
                                gifImage!.Visibility = Visibility.Visible;
                                gifImage.Stretch = App.Settings.Prop.BackgroundImageStretch switch
                                {
                                    BackgroundImageStretchMode.Fill => Stretch.Fill,
                                    BackgroundImageStretchMode.Uniform => Stretch.Uniform,
                                    BackgroundImageStretchMode.UniformToFill => Stretch.UniformToFill,
                                    _ => Stretch.Fill
                                };

                                XamlAnimatedGif.AnimationBehavior.SetSourceUri(gifImage, uri);
                                RenderOptions.SetBitmapScalingMode(gifImage, BitmapScalingMode.HighQuality);
                            }
                        }
                        else
                        {
                            if (TryFindAnimatedGifImageControl(out var gifImage))
                            {
                                gifImage!.Visibility = Visibility.Collapsed;
                                XamlAnimatedGif.AnimationBehavior.SetSourceUri(gifImage, null);
                            }

                            var image = new BitmapImage();
                            image.BeginInit();
                            image.CacheOption = BitmapCacheOption.OnLoad;
                            image.UriSource = uri;
                            image.EndInit();

                            var stretch = App.Settings.Prop.BackgroundImageStretch switch
                            {
                                BackgroundImageStretchMode.Fill => Stretch.Fill,
                                BackgroundImageStretchMode.Uniform => Stretch.Uniform,
                                BackgroundImageStretchMode.UniformToFill => Stretch.UniformToFill,
                                _ => Stretch.Fill
                            };

                            var imageBrush = new ImageBrush(image)
                            {
                                Stretch = stretch,
                                AlignmentX = AlignmentX.Center,
                                AlignmentY = AlignmentY.Center
                            };

                            Application.Current.Resources["WindowBackgroundGradient"] = imageBrush;
                        }

                        Application.Current.Resources["NewTextEditorBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D2D"));
                        Application.Current.Resources["NewTextEditorForeground"] = new SolidColorBrush(Colors.White);
                        Application.Current.Resources["NewTextEditorLink"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A9CEA"));
                        Application.Current.Resources["PrimaryBackgroundColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0FFFFFFF"));
                        Application.Current.Resources["NormalDarkAndLightBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0FFFFFFF"));

                        double overlayOpacity = App.Settings.Prop.BlackOverlayOpacity;
                        var overlayColor = Color.FromArgb((byte)(overlayOpacity * 255), 0, 0, 0);
                        Application.Current.Resources["WindowBackgroundBlackOverlay"] = new SolidColorBrush(overlayColor);
                    }
                    catch
                    {
                        Application.Current.Resources["WindowBackgroundGradient"] = null;
                        Application.Current.Resources["WindowBackgroundBlackOverlay"] = new SolidColorBrush(Colors.Transparent);

                        if (TryFindAnimatedGifImageControl(out var gifImage))
                        {
                            gifImage!.Visibility = Visibility.Collapsed;
                            XamlAnimatedGif.AnimationBehavior.SetSourceUri(gifImage, null);
                        }
                    }
                }
                else
                {
                    if (TryFindAnimatedGifImageControl(out var gifImage))
                    {
                        gifImage!.Visibility = Visibility.Collapsed;
                        XamlAnimatedGif.AnimationBehavior.SetSourceUri(gifImage, null);
                    }

                    Application.Current.Resources["WindowBackgroundGradient"] = null;

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

                    var customBrush = new LinearGradientBrush
                    {
                        StartPoint = startPoint,
                        EndPoint = endPoint
                    };

                    foreach (var stop in App.Settings.Prop.CustomGradientStops.OrderBy(s => s.Offset))
                    {
                        try
                        {
                            var color = (Color)ColorConverter.ConvertFromString(stop.Color);
                            customBrush.GradientStops.Add(new GradientStop(color, stop.Offset));
                        }
                        catch { }
                    }

                    Application.Current.Resources["WindowBackgroundGradient"] = customBrush;

                    Application.Current.Resources["NewTextEditorBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#59000000"));
                    Application.Current.Resources["NewTextEditorForeground"] = new SolidColorBrush(Colors.White);
                    Application.Current.Resources["NewTextEditorLink"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A9CEA"));
                    Application.Current.Resources["PrimaryBackgroundColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#19000000"));
                    Application.Current.Resources["NormalDarkAndLightBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0FFFFFFF"));
                    Application.Current.Resources["ControlFillColorDefault"] = (Color)ColorConverter.ConvertFromString("#19000000");

                    Application.Current.Resources["WindowBackgroundBlackOverlay"] = new SolidColorBrush(Colors.Transparent);
                }

                Application.Current.Resources.MergedDictionaries[customThemeIndex] = new ResourceDictionary();
            }
            else
            {
                if (TryFindAnimatedGifImageControl(out var gifImage))
                {
                    gifImage!.Visibility = Visibility.Collapsed;
                    XamlAnimatedGif.AnimationBehavior.SetSourceUri(gifImage, null);
                }

                var dict = new ResourceDictionary
                {
                    Source = new Uri($"pack://application:,,,/UI/Style/{Enum.GetName(finalTheme)}.xaml")
                };

                Application.Current.Resources.MergedDictionaries[customThemeIndex] = dict;

                Application.Current.Resources["WindowBackgroundGradient"] = null;
                Application.Current.Resources.Remove("NewTextEditorBackground");
                Application.Current.Resources.Remove("NewTextEditorForeground");
                Application.Current.Resources.Remove("NewTextEditorLink");
                Application.Current.Resources.Remove("PrimaryBackgroundColor");
                Application.Current.Resources.Remove("NormalDarkAndLightBackground");
                Application.Current.Resources.Remove("ControlFillColorDefault");
                Application.Current.Resources.Remove("WindowBackgroundBlackOverlay");
            }

#if QA_BUILD
    this.BorderBrush = Brushes.Red;
    this.BorderThickness = new Thickness(4);
#endif
        }

        private bool TryFindAnimatedGifImageControl(out System.Windows.Controls.Image? gifImage)
        {
            gifImage = null;

            foreach (Window window in Application.Current.Windows)
            {
                gifImage = FindElementByName<System.Windows.Controls.Image>(window, "AnimatedGifBackground");
                if (gifImage != null)
                    return true;
            }

            return false;
        }

        private static T? FindElementByName<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            if (parent == null)
                return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is T element && element.Name == name)
                    return element;

                T? result = FindElementByName<T>(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        private void WpfUiWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            ApplyTheme();
            this.Loaded -= WpfUiWindow_Loaded;
        }


        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Hardware Accel
            if (App.Settings.Prop.WPFSoftwareRender || App.LaunchSettings.NoGPUFlag.Active)
            {
                if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
                    hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
            }

            // CustomFont
            string? fontPath = App.Settings.Prop.CustomFontPath;
            if (!string.IsNullOrWhiteSpace(fontPath) && File.Exists(fontPath))
            {
                var font = FontManager.LoadFontFromFile(fontPath);
                if (font != null)
                {
                    this.FontFamily = font;
                }
            }
        }
    }
}
