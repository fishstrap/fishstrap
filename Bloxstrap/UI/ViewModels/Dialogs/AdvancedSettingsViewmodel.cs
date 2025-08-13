using System.ComponentModel;

namespace Bloxstrap.UI.ViewModels.Dialogs
{
    public class AdvancedSettingViewModel : INotifyPropertyChanged
    {
        public static event EventHandler? ShowPresetColumnChanged;
        public static event EventHandler? CtrlCJsonFormatChanged;
        public static event EventHandler? ShowFlagCountChanged;
        public event EventHandler? ShowAddWithIDChanged;

        public CopyFormatMode SelectedCopyFormat
        {
            get => App.Settings.Prop.SelectedCopyFormat;
            set
            {
                if (App.Settings.Prop.SelectedCopyFormat != value)
                {
                    App.Settings.Prop.SelectedCopyFormat = value;
                    OnPropertyChanged(nameof(SelectedCopyFormat));
                }
            }
        }

        public IEnumerable<CopyFormatMode> CopyFormatModes => Enum.GetValues(typeof(CopyFormatMode)).Cast<CopyFormatMode>();

        public bool ShowCtrlCJsonFormatSetting
        {
            get => App.Settings.Prop.CtrlCJsonFormat;
            set
            {
                if (App.Settings.Prop.CtrlCJsonFormat != value)
                {
                    App.Settings.Prop.CtrlCJsonFormat = value;
                    OnPropertyChanged(nameof(ShowCtrlCJsonFormatSetting));
                    CtrlCJsonFormatChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool ShowPresetColumnSetting
        {
            get => App.Settings.Prop.ShowPresetColumn;
            set
            {
                if (App.Settings.Prop.ShowPresetColumn != value)
                {
                    App.Settings.Prop.ShowPresetColumn = value;
                    OnPropertyChanged(nameof(ShowPresetColumnSetting));
                    ShowPresetColumnChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool ShowFlagCount
        {
            get => App.Settings.Prop.ShowFlagCount;
            set
            {
                if (App.Settings.Prop.ShowFlagCount != value)
                {
                    App.Settings.Prop.ShowFlagCount = value;
                    OnPropertyChanged(nameof(ShowFlagCount));
                    ShowFlagCountChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool ShowAddWithID
        {
            get => App.Settings.Prop.ShowAddWithID;
            set
            {
                if (App.Settings.Prop.ShowAddWithID != value)
                {
                    App.Settings.Prop.ShowAddWithID = value;
                    OnPropertyChanged(nameof(ShowAddWithID));
                    ShowAddWithIDChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}