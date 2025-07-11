namespace Bloxstrap.Models
{
    public class Tweak
    {
        public string Title { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string[] Category { get; set; } = Array.Empty<string>();
        public string Description { get; set; } = string.Empty;
    }
}