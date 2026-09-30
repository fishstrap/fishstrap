using Bloxstrap.Enums.Overlay;

namespace Bloxstrap.Models.Overlay
{
    public record FriendPresenceChange(long UserId, FriendPresenceKind Kind, string GameName, long PlaceId = 0, long RootPlaceId = 0, string? ServerId = null);
}
