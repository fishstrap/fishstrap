using Fishstrap.Enums.GBSPresets;

namespace Fishstrap.UI.ViewModels.Settings
{
    public class GlobalSettingsViewModel : NotifyPropertyChangedViewModel
    {
        public bool ReadOnly
        {
            get => App.GlobalSettings.GetReadOnly();
            set => App.GlobalSettings.SetReadOnly(value);
        }

        public int FramerateCap
        {
            get
            {
                if (int.TryParse(App.GlobalSettings.GetPreset("Rendering.FramerateCap"), out int framerate))
                {
                    // -1 is default framerate cap set by `DFIntTaskSchedulerTargetFps`
                    if (framerate < 1)
                        return 60;
                    else
                        return framerate;
                }
                else
                    return 60;
            }
            set
            {
                // setting the framerate cap to 0 will break roblox's renderer so we want to avoid that
                if (value < 1)
                    value = -1;
                
                App.GlobalSettings.SetPreset("Rendering.FramerateCap", value);
            }
        }

        public string UITransparency
        {
            get => App.GlobalSettings.GetPreset("UI.Transparency")!;
            set
            {
                App.GlobalSettings.SetPreset("UI.Transparency", value.Length >= 3 ? value[..3] : value); // guhh??

                OnPropertyChanged(nameof(UITransparency));
            }
        }

        public string GraphicsQuality
        {
            get => App.GlobalSettings.GetPreset("Rendering.SavedQualityLevel")!;
            set
            {
                App.GlobalSettings.SetPreset("Rendering.SavedQualityLevel", value);

                OnPropertyChanged(nameof(GraphicsQuality));
            }
        }

        public bool ReducedMotion
        {
            get => App.GlobalSettings.GetPreset("UI.ReducedMotion")?.ToLower() == "true";
            set => App.GlobalSettings.SetPreset("UI.ReducedMotion", value);
        }

        public IReadOnlyDictionary<FontSize, string?> FontSizes => GlobalSettingsManager.FontSizes;
        public FontSize SelectedFontSize
        {
            get => FontSizes.FirstOrDefault(x => x.Value == App.GlobalSettings.GetPreset("UI.FontSize")).Key;
            set => App.GlobalSettings.SetPreset("UI.FontSize", FontSizes[value]);
        }

        public string MouseSensitivity
        {
            get => App.GlobalSettings.GetPreset("User.MouseSensitivity")!;
            set => App.GlobalSettings.SetPreset("User.MouseSensitivity", value);
        }

        public string VREnabled
        {
            get => App.GlobalSettings.GetPreset("User.VREnabled")!;
            set => App.GlobalSettings.SetPreset("User.VREnabled", value);
        }
    }
}