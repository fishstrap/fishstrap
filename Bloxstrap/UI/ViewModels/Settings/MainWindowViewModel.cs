using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class MainWindowViewModel : NotifyPropertyChangedViewModel
    {
        public ICommand OpenAboutCommand => new RelayCommand(OpenAbout);
        public ICommand OpenHelpCommand => new RelayCommand(OpenHelp);
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

        private void OpenAbout()
        {
            var app = (App.Current as App);
            app?._froststrapRPC?.UpdatePresence("Dialog: About");

            new Elements.About.MainWindow().ShowDialog();

            app?._froststrapRPC?.UpdatePresence("Page: Unknown");
        }

        private void OpenHelp()
        {
            var app = (App.Current as App);
            app?._froststrapRPC?.UpdatePresence("Dialog: Help");

            new Elements.Help.MainWindow().ShowDialog();

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

        public void AddGradientStop()
        {
            var newStop = new GradientStopData { Offset = 0.5, Color = "#FFFFFF" };
            GradientStops.Add(newStop);
            App.Settings.Prop.CustomGradientStops = GradientStops.ToList();
            OnPropertyChanged(nameof(GradientStops));
        }

    }
}
