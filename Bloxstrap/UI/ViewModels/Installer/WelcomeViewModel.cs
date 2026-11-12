namespace Bloxstrap.UI.ViewModels.Installer
{
    public class WelcomeViewModel : NotifyPropertyChangedViewModel
    {
        // formatting is done here instead of in xaml, it's just a bit easier
        public string MainText => String.Format(
            Strings.Installer_Welcome_MainText,
            "[GitHub repository](https://github.com/sirxpl/xpl-fishstrap)",
            "[GitHub Releases](https://github.com/sirxpl/xpl-fishstrap/releases)"
        );

        public bool CanContinue { get; set; } = false;
    }
}
