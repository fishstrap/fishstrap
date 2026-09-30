using Bloxstrap.Enums.Overlay;

namespace Bloxstrap.Models.Overlay
{
    public record FriendPresenceChange(long UserId, FriendPresenceKind Kind, string GameName);
}
