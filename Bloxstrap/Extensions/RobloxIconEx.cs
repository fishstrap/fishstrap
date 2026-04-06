using System.Drawing;

namespace Bloxstrap.Extensions
{
    static class RobloxIconEx
    {
        public static IReadOnlyCollection<RobloxIcon> Selections => new RobloxIcon[]
        {
            RobloxIcon.IconDefault,
            RobloxIcon.Icon2022,
            RobloxIcon.Icon2019,
            RobloxIcon.Icon2017,
            RobloxIcon.IconLate2015,
            RobloxIcon.IconEarly2015,
            RobloxIcon.Icon2011,
            RobloxIcon.Icon2008,
            RobloxIcon.IconCustom
        };

        // small note on handling icon sizes
        // i'm using multisize icon packs here with sizes 16, 24, 32, 48, 64 and 128
        // use this for generating multisize packs: https://www.aconvert.com/icon/

        public static Icon GetIcon(this RobloxIcon icon)
        {
            const string LOG_IDENT = "RobloxIconEx::GetIcon";

            // load the custom icon file
            if (icon == RobloxIcon.IconCustom)
            {
                Icon? customIcon = null;
                string location = App.Settings.Prop.RobloxIconCustomLocation;

                if (String.IsNullOrEmpty(location)) 
                {
                    App.Logger.WriteLine(LOG_IDENT, "Warning: custom icon is not set.");
                }
                else
                {
                    try
                    {
                        customIcon = new Icon(location);
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine(LOG_IDENT, $"Failed to load custom icon!");
                        App.Logger.WriteException(LOG_IDENT, ex);
                    }
                }

                return customIcon ?? Properties.Resources.IconBloxstrap;
            }

            return icon switch
            {
                RobloxIcon.Icon2008 => Properties.Resources.Icon2008,
                RobloxIcon.Icon2011 => Properties.Resources.Icon2011,
                RobloxIcon.IconEarly2015 => Properties.Resources.IconEarly2015,
                RobloxIcon.IconLate2015 => Properties.Resources.IconLate2015,
                RobloxIcon.Icon2017 => Properties.Resources.Icon2017,
                RobloxIcon.Icon2019 => Properties.Resources.Icon2019,
                RobloxIcon.Icon2022 => Properties.Resources.Icon2022,
                _ => Properties.Resources.IconBloxstrap
            };
        }
    }
}
