using System.Windows.Media;

namespace Bloxstrap.Models
{
    public class ThemeEntry
    {
        public Theme IconType { get; set; }
        public ImageSource ImageSource => IconType.GetIcon().GetImageSource();
    }
}
