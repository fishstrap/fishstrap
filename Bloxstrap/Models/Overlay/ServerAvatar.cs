namespace Bloxstrap.Models.Overlay
{
    public record ServerAvatar(string? ImageUrl, string? OverflowText)
    {
        public bool IsOverflow => OverflowText is not null;
    }
}
