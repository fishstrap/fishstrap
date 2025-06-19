using System.Drawing;

namespace Bloxstrap.Extensions
{
    static class RobloxIconEx
    {
        public static IReadOnlyCollection<RobloxIcon> Selections => new RobloxIcon[]
        {
            RobloxIcon.Default,
            RobloxIcon.Icon2022,
            RobloxIcon.Icon2019,
            RobloxIcon.Icon2017,
            RobloxIcon.IconEarly2015,
            RobloxIcon.IconLate2015,
            RobloxIcon.Icon2011,
            RobloxIcon.Icon2008,
            RobloxIcon.IconBloxstrap,
        };

        public static Icon GetIcon(this RobloxIcon icon)
        {
            return icon switch
            {
                RobloxIcon.IconBloxstrap => Properties.Resources.IconBloxstrap,
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
