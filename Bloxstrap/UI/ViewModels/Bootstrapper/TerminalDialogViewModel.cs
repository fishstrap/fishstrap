using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;

using CommunityToolkit.Mvvm.Input;

namespace Bloxstrap.UI.ViewModels.Bootstrapper
{
    public class TerminalDialogViewModel : NotifyPropertyChangedViewModel
    {
        private readonly IBootstrapperDialog _dialog;

        public ICommand CancelInstallCommand => new RelayCommand(CancelInstall);

        public Brush MainTerminalBackground { get; set; } = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
        public Brush TerminalTitleBarBackground { get; set; } = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        public string Title => App.Settings.Prop.BootstrapperTitle;
        public string LaunchArgText { get; set; } = "PS> ./Fishstrap.exe ";
        public ImageSource Icon { get; set; } = App.Settings.Prop.BootstrapperIcon.GetIcon().GetImageSource();
        public string Message { get; set; } = string.Empty;
        public bool ProgressIndeterminate { get; set; } = true;
        public int ProgressMaximum { get; set; } = 0;
        public int ProgressValue { get; set; } = 0;

        public TaskbarItemProgressState TaskbarProgressState { get; set; } = TaskbarItemProgressState.Indeterminate;
        public double TaskbarProgressValue { get; set; } = 0;

        public bool CancelEnabled { get; set; } = false;
        public Visibility CancelButtonVisibility => CancelEnabled ? Visibility.Visible : Visibility.Collapsed;

        [Obsolete("Do not use this! This is for the designer only.", true)]
        public TerminalDialogViewModel()
        {
            _dialog = null!;
        }

        public TerminalDialogViewModel(IBootstrapperDialog dialog)
        {
            if (App.Settings.Prop.UseAcrylicBackground)
            {
                MainTerminalBackground = new SolidColorBrush(Color.FromArgb(App.Settings.Prop.AcrylicBackgroundOpacity, 32, 32, 32));

                if (App.Settings.Prop.Theme.GetFinal() == Enums.Theme.Light)
                    TerminalTitleBarBackground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                else
                    TerminalTitleBarBackground = new SolidColorBrush(Color.FromArgb(255, 51, 51, 51));
            }

            string args = "-launch";

            if (App.LaunchSettings.RobloxLaunchMode == LaunchMode.Player)
                args = "-player";

            if (App.LaunchSettings.RobloxLaunchMode == LaunchMode.Studio || App.LaunchSettings.RobloxLaunchMode == LaunchMode.StudioAuth)
                args = "-studio";

            LaunchArgText += args;

            _dialog = dialog;
        }

        private void CancelInstall()
        {
            _dialog.Bootstrapper?.Cancel();
            _dialog.CloseBootstrapper();
        }
    }
}
