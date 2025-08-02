using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
            ApplyTheme();
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
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad; // avoid file lock
                        image.UriSource = new Uri(App.Settings.Prop.ImageBackgroundPath);
                        image.EndInit();

                        // Determine stretch mode from settings
                        Stretch stretch = Stretch.Fill;
                        switch (App.Settings.Prop.BackgroundImageStretch)
                        {
                            case BackgroundImageStretchMode.Fill:
                                stretch = Stretch.Fill;
                                break;
                            case BackgroundImageStretchMode.Uniform:
                                stretch = Stretch.Uniform;
                                break;
                            case BackgroundImageStretchMode.UniformToFill:
                                stretch = Stretch.UniformToFill;
                                break;
                        }

                        var imageBrush = new ImageBrush(image)
                        {
                            Stretch = stretch,
                            AlignmentX = AlignmentX.Center,
                            AlignmentY = AlignmentY.Center
                        };

                        Application.Current.Resources["WindowBackgroundGradient"] = imageBrush;

                        Application.Current.Resources["NewTextEditorBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D2D"));
                        Application.Current.Resources["NewTextEditorForeground"] = new SolidColorBrush(Colors.White);
                        Application.Current.Resources["NewTextEditorLink"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A9CEA"));
                        Application.Current.Resources["PrimaryBackgroundColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0FFFFFFF"));
                        Application.Current.Resources["NormalDarkAndLightBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0FFFFFFF"));
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Resources["WindowBackgroundGradient"] = null;
                        Debug.WriteLine("Failed to load background image: " + ex.Message);
                    }
                }
                else
                {
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

                    var startPoint = App.Settings.Prop.GradientStartPoint;
                    var endPoint = App.Settings.Prop.GradientEndPoint;
                    if (startPoint == default) startPoint = new Point(1, 1);
                    if (endPoint == default) endPoint = new Point(0, 0);

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
                        catch
                        {
                            // Skip invalid colors
                        }
                    }

                    Application.Current.Resources["WindowBackgroundGradient"] = customBrush;

                    Application.Current.Resources["NewTextEditorBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#59000000"));
                    Application.Current.Resources["NewTextEditorForeground"] = new SolidColorBrush(Colors.White);
                    Application.Current.Resources["NewTextEditorLink"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A9CEA"));
                    Application.Current.Resources["PrimaryBackgroundColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#19000000"));
                    Application.Current.Resources["NormalDarkAndLightBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0FFFFFFF"));
                    Application.Current.Resources["ControlFillColorDefault"] = (Color)ColorConverter.ConvertFromString("#19000000");
                }

                Application.Current.Resources.MergedDictionaries[customThemeIndex] = new ResourceDictionary();
            }
            else
            {
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
            }

#if QA_BUILD
    this.BorderBrush = Brushes.Red;
    this.BorderThickness = new Thickness(4);
#endif
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
