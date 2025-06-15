using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using SWM = System.Windows.Media;  // did this so i dont have to keep writing system.Windows.Media

namespace Bloxstrap.Models
{
    public static class FontManager
    {
        public static bool IsCustomFontApplied { get; private set; }

        public static SWM.FontFamily? LoadFontFromFile(string fontFilePath)
        {
            if (!File.Exists(fontFilePath))
                return null;

            string tempFontsRoot = Path.Combine(Path.GetTempPath(), "BloxstrapFonts");

            string uniqueFontFolder = Path.Combine(tempFontsRoot, Guid.NewGuid().ToString());
            Directory.CreateDirectory(uniqueFontFolder);

            string destFontPath = Path.Combine(uniqueFontFolder, Path.GetFileName(fontFilePath));
            File.Copy(fontFilePath, destFontPath, overwrite: true);

            var fontDirectoryUri = new Uri(uniqueFontFolder + Path.DirectorySeparatorChar);
            var fontFamilies = Fonts.GetFontFamilies(fontDirectoryUri);

            return fontFamilies.FirstOrDefault();
        }

        public static bool ApplySavedCustomFont()
        {
            string? savedFontPath = App.Settings.Prop.CustomFontPath;

            if (!string.IsNullOrWhiteSpace(savedFontPath) && File.Exists(savedFontPath))
            {
                try
                {
                    var font = LoadFontFromFile(savedFontPath);
                    if (font != null)
                    {
                        ApplyFontGlobally(font);
                        IsCustomFontApplied = true;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine("FontManager", $"Failed to load saved font: {ex}");
                }
            }

            return false;
        }

        public static void ApplyFontGlobally(SWM.FontFamily fontFamily)
        {
            Application.Current.Resources[SystemFonts.MessageFontFamilyKey] = fontFamily;

            foreach (Window window in Application.Current.Windows)
                window.FontFamily = fontFamily;

            IsCustomFontApplied = fontFamily.Source != "Segoe UI";
        }

        public static void RemoveCustomFont()
        {
            var defaultFont = new SWM.FontFamily("Segoe UI");
            ApplyFontGlobally(defaultFont);
            IsCustomFontApplied = false;
            App.Settings.Prop.CustomFontPath = null;
            App.Settings.Save();
        }
    }
}
