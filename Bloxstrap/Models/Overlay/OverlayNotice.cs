using Bloxstrap.Enums.Overlay;

namespace Bloxstrap.Models.Overlay
{
    public record OverlayNotice(string Title, string Message, string? ImageUrl = null, bool RoundImage = false, NoticeKind Kind = NoticeKind.General,
        string? ActionText = null, Action? OnClick = null)
    {
        public bool IsClickable => OnClick is not null;
    }
}
