using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;
using Wpf.Ui.Appearance;
using System.Diagnostics;
using System.Linq;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class MainWindowViewModel : NotifyPropertyChangedViewModel
    {
        public ICommand OpenAboutCommand => new RelayCommand(OpenAbout);
        public ICommand SaveSettingsCommand => new RelayCommand(SaveSettings);
        public ICommand SaveAndLaunchSettingsCommand => new RelayCommand(SaveAndLaunchSettings);
        public ICommand RestartAppCommand => new RelayCommand(RestartApp);
        public ICommand CloseWindowCommand => new RelayCommand(CloseWindow);

        public EventHandler? RequestSaveNoticeEvent;
        public EventHandler? RequestCloseWindowEvent;

        public bool TestModeEnabled
        {
            get => App.LaunchSettings.TestModeFlag.Active;
            set
            {
                if (value && !App.State.Prop.TestModeWarningShown)
                {
                    var result = Frontend.ShowMessageBox(Strings.Menu_TestMode_Prompt, MessageBoxImage.Information, MessageBoxButton.YesNo);
                    if (result != MessageBoxResult.Yes)
                        return;

                    App.State.Prop.TestModeWarningShown = true;
                }
                App.LaunchSettings.TestModeFlag.Active = value;
            }
        }
        public void ApplyBackdrop(UIBackgroundType value)
        {
            var wpfBackdrop = value switch
            {
                UIBackgroundType.None => BackgroundType.None,
                UIBackgroundType.Mica => BackgroundType.Mica,
                UIBackgroundType.Acrylic => BackgroundType.Acrylic,
                UIBackgroundType.Aero => BackgroundType.Aero,
                _ => BackgroundType.None
            };

            foreach (Window window in Application.Current.Windows)
            {
                if (window is UiWindow uiWindow)
                {
                    bool isTransparentBackdrop = (wpfBackdrop == BackgroundType.Acrylic || wpfBackdrop == BackgroundType.Aero);

                    uiWindow.AllowsTransparency = isTransparentBackdrop;

                    uiWindow.WindowStyle = isTransparentBackdrop
                        ? WindowStyle.None
                        : WindowStyle.SingleBorderWindow;

                    uiWindow.WindowBackdropType = wpfBackdrop;
                }
            }
        }

        private void OpenAbout()
        {
            var app = (App.Current as App);
            app?._froststrapRPC?.UpdatePresence("Dialog: About");

            new Elements.About.MainWindow().ShowDialog();

            app?._froststrapRPC?.UpdatePresence("Page: Unknown");
        }

        private void CloseWindow() => RequestCloseWindowEvent?.Invoke(this, EventArgs.Empty);

        private void SaveSettings()
        {
            const string LOG_IDENT = "MainWindowViewModel::SaveSettings";

            App.Settings.Save();
            App.State.Save();
            App.FastFlags.Save();

            foreach (var pair in App.PendingSettingTasks)
            {
                var task = pair.Value;

                if (task.Changed)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Executing pending task '{task}'");
                    task.Execute();
                }
            }

            App.PendingSettingTasks.Clear();

            RequestSaveNoticeEvent?.Invoke(this, EventArgs.Empty);
        }

        public void SaveAndLaunchSettings()
        {
            SaveSettings();
            if (!App.LaunchSettings.TestModeFlag.Active) // test mode already launches an instance
                LaunchHandler.LaunchRoblox(LaunchMode.Player);

            CloseWindow();
        }

        private void RestartApp()
        {
            SaveSettings();

            var startInfo = new ProcessStartInfo(Environment.ProcessPath!)
            {
                Arguments = "-menu"
            };

            Process.Start(startInfo);

            Application.Current.Shutdown();
        }

        public ObservableCollection<GradientStopData> GradientStops { get; } =
            new(App.Settings.Prop.CustomGradientStops);
    }
}