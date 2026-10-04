using System.Windows;

namespace Bloxstrap.Models.Overlay
{
    public static class OverlayPlacement
    {
        public static Point Place(Rect area, Size element, double x, double y) => new(
            area.Left + Math.Clamp(x, 0, 1) * Math.Max(area.Width - element.Width, 0),
            area.Top + Math.Clamp(y, 0, 1) * Math.Max(area.Height - element.Height, 0));
    }
}
