using System.ComponentModel;
using Bloxstrap;

public class AdvancedSettingViewModel : INotifyPropertyChanged
{
    public static event EventHandler? ShowPresetColumnChanged;
    public static event EventHandler? ShowFlagCountChanged;
    public event EventHandler? ShowAddWithIDChanged;

    private CopyFormatMode _selectedCopyFormat;
    private bool _showPresetColumnSetting;

    public AdvancedSettingViewModel()
    {
        LoadSettings();
    }

    public CopyFormatMode SelectedCopyFormat
    {
        get => _selectedCopyFormat;
        set
        {
            if (_selectedCopyFormat != value)
            {
                _selectedCopyFormat = value;
                OnPropertyChanged(nameof(SelectedCopyFormat));
            }
        }
    }

    public IEnumerable<CopyFormatMode> CopyFormatModes => Enum.GetValues(typeof(CopyFormatMode)).Cast<CopyFormatMode>();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void SaveSettings()
    {
        var settings = App.Settings.Prop;
        settings.SelectedCopyFormat = SelectedCopyFormat;
        settings.ShowPresetColumn = ShowPresetColumnSetting;
        App.Settings.Save();
    }

    public void LoadSettings()
    {
        var settings = App.Settings.Prop;
        SelectedCopyFormat = settings.SelectedCopyFormat;
        ShowPresetColumnSetting = settings.ShowPresetColumn;
        ShowFlagCount = settings.ShowFlagCount;
        ShowAddWithID = settings.ShowAddWithID;
    }

    public bool ShowPresetColumnSetting
    {
        get => _showPresetColumnSetting;
        set
        {
            if (_showPresetColumnSetting != value)
            {
                _showPresetColumnSetting = value;
                OnPropertyChanged(nameof(ShowPresetColumnSetting));
                SaveSettings(); // persist the change
                ShowPresetColumnChanged?.Invoke(this, EventArgs.Empty); // notify UI
            }
        }
    }

    private bool _showFlagCount;
    public bool ShowFlagCount
    {
        get => _showFlagCount;
        set
        {
            if (_showFlagCount != value)
            {
                _showFlagCount = value;
                OnPropertyChanged(nameof(ShowFlagCount));
                var settings = App.Settings.Prop;
                settings.ShowFlagCount = value;
                App.Settings.Save();
                ShowFlagCountChanged?.Invoke(this, EventArgs.Empty); // Notify listeners
            }
        }
    }

    private bool _showAddWithID;

    public bool ShowAddWithID
    {
        get => _showAddWithID;
        set
        {
            if (_showAddWithID != value)
            {
                _showAddWithID = value;
                OnPropertyChanged(nameof(ShowAddWithID));

                var settings = App.Settings.Prop;
                settings.ShowAddWithID = value;
                App.Settings.Save();

                ShowAddWithIDChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
