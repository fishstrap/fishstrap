namespace Bloxstrap.UI.ViewModels.Installer
{
    public class WelcomeViewModel : NotifyPropertyChangedViewModel
    {
        // formatting is done here instead of in xaml, it's just a bit easier
        public string MainText => String.Format(
            Strings.Installer_Welcome_MainText,
            "[github.com/fishstrap/fishstrap](https://github.com/fishstrap/fishstrap)",
            "[fishstrap.app](https://fishstrap.app)"
        );

        public bool CanContinue { get; set; } = false;
    }
}
