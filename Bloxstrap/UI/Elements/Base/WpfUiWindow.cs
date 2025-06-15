using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
            const int customThemeIndex = 2; // index for CustomTheme merged dictionary

            _themeService.SetTheme(App.Settings.Prop.Theme.GetFinal() == Enums.Theme.Light ? ThemeType.Light : ThemeType.Dark);
            _themeService.SetSystemAccent();

            // there doesn't seem to be a way to query the name for merged dictionaries
            var dict = new ResourceDictionary { Source = new Uri($"pack://application:,,,/UI/Style/{Enum.GetName(App.Settings.Prop.Theme.GetFinal())}.xaml") };
            Application.Current.Resources.MergedDictionaries[customThemeIndex] = dict;

#if QA_BUILD
            this.BorderBrush = System.Windows.Media.Brushes.Red;
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
