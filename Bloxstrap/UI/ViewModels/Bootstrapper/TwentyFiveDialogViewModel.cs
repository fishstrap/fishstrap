using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualBasic.Logging;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;

namespace Bloxstrap.UI.ViewModels.Bootstrapper
{
    public class TwentyFiveDialogViewModel : NotifyPropertyChangedViewModel
    {
        private readonly IBootstrapperDialog _dialog;

        public ICommand CancelInstallCommand => new RelayCommand(CancelInstall);

        public string Title => App.Settings.Prop.BootstrapperTitle;

        private bool? IsStudioLaunch => App.Bootstrapper?.IsStudioLaunch;

        public ImageSource Icon { get; set; }
        public ImageSource Logo { get; set; }
        public string Message { get; set; } = Strings.Bootstrapper_Status_Connecting;
        public bool ProgressIndeterminate { get; set; } = true;
        public int ProgressMaximum { get; set; } = 0;
        public int ProgressValue { get; set; } = 0;

        public TaskbarItemProgressState TaskbarProgressState { get; set; } = TaskbarItemProgressState.Indeterminate;
        public double TaskbarProgressValue { get; set; } = 0;

        public bool CancelEnabled { get; set; } = false;
        public Visibility CancelButtonVisibility => CancelEnabled ? Visibility.Visible : Visibility.Collapsed;

        [Obsolete("Do not use this! This is for the designer only.", true)]
        public TwentyFiveDialogViewModel()
        {
            Uri icon = new("pack://application:,,,/Resources/BootstrapperStyles/TwentyFiveDialog/PlayerLogo.png");
            Uri logo = new("pack://application:,,,/Resources/BootstrapperStyles/TwentyFiveDialog/Roblox.png");

            _dialog = null!;

            Icon = new BitmapImage(icon);
            Logo = new BitmapImage(logo);
        }

        public TwentyFiveDialogViewModel(IBootstrapperDialog dialog)
        {
            _dialog = dialog;

            Uri icon = new Uri("pack://application:,,,/Resources/BootstrapperStyles/TwentyFiveDialog/PlayerLogo.png");
            Uri logo = new Uri("pack://application:,,,/Resources/BootstrapperStyles/TwentyFiveDialog/Roblox.png");

            if (IsStudioLaunch == true)
            {
                icon = new Uri("pack://application:,,,/Resources/BootstrapperStyles/TwentyFiveDialog/StudioLogo.png");
                logo = new Uri("pack://application:,,,/Resources/BootstrapperStyles/TwentyFiveDialog/Studio.png");
            }

            Icon = new BitmapImage(icon);
            Logo = new BitmapImage(logo);
        }

        private void CancelInstall()
        {
            _dialog.Bootstrapper?.Cancel();
            _dialog.CloseBootstrapper();
        }
    }
}
