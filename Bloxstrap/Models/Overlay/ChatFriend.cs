using System.ComponentModel;

using Bloxstrap.Enums.Overlay;

namespace Bloxstrap.Models.Overlay
{
    public class ChatFriend : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public long UserId { get; init; }

        public bool IsGroup { get; init; }

        public int MemberCount { get; init; }

        public string DisplayName { get; init; } = String.Empty;

        public string Username { get; init; } = String.Empty;

        public string Handle => IsGroup || String.IsNullOrEmpty(Username) ? String.Empty : $"@{Username}";

        private string? _conversationId;

        public string? ConversationId
        {
            get => _conversationId;
            set
            {
                _conversationId = value;
                Changed(nameof(ConversationId));
            }
        }

        private string? _headshot;

        public string? Headshot
        {
            get => _headshot;
            set
            {
                _headshot = value;
                Changed(nameof(Headshot));
            }
        }

        private static readonly string[] PresenceProperties =
        {
            nameof(Status), nameof(GameName), nameof(PlaceId), nameof(RootPlaceId), nameof(ServerId), nameof(CanJoin), nameof(CanViewGame), nameof(StatusText)
        };

        public FriendStatus Status { get; private set; } = FriendStatus.Offline;

        public string? GameName { get; private set; }

        public long PlaceId { get; private set; }

        public long RootPlaceId { get; private set; }

        public string? ServerId { get; private set; }

        public bool CanJoin => Status == FriendStatus.InGame && PlaceId > 0 && !String.IsNullOrEmpty(ServerId);

        public bool CanViewGame => Status == FriendStatus.InGame && !CanJoin && (RootPlaceId > 0 || PlaceId > 0);

        public string StatusText
        {
            get
            {
                if (IsGroup)
                    return String.Format(Strings.Menu_Overlay_Messages_Members, MemberCount);

                return Status switch
                {
                    FriendStatus.InGame => String.IsNullOrEmpty(GameName)
                        ? Strings.Menu_Overlay_Messages_InAGame
                        : String.Format(Strings.Menu_Overlay_Messages_Playing, GameName),
                    FriendStatus.InStudio => Strings.Menu_Overlay_Messages_InStudio,
                    FriendStatus.Online => Strings.Menu_Overlay_Messages_Online,
                    _ => Strings.Menu_Overlay_Messages_Offline
                };
            }
        }

        private int _unread;

        public int Unread
        {
            get => _unread;
            set
            {
                _unread = Math.Max(value, 0);
                Changed(nameof(Unread));
                Changed(nameof(HasUnread));
                Changed(nameof(UnreadText));
            }
        }

        public bool HasUnread => _unread > 0;

        public string UnreadText => _unread > 99 ? "99+" : _unread.ToString();

        public bool Matches(string search) =>
            DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) || Username.Contains(search, StringComparison.OrdinalIgnoreCase);

        public void Update(UserPresence? presence)
        {
            FriendStatus status = presence?.UserPresenceType switch
            {
                UserPresence.InGameType => FriendStatus.InGame,
                3 => FriendStatus.InStudio,
                1 => FriendStatus.Online,
                _ => FriendStatus.Offline
            };

            string? game = status == FriendStatus.InGame && !String.IsNullOrWhiteSpace(presence?.LastLocation) ? presence!.LastLocation : null;
            long place = presence?.PlaceId ?? 0;
            long root = presence?.RootPlaceId ?? 0;
            string? server = String.IsNullOrEmpty(presence?.GameId) ? null : presence!.GameId;

            if (status == Status && game == GameName && place == PlaceId && root == RootPlaceId && server == ServerId)
                return;

            Status = status;
            GameName = game;
            PlaceId = place;
            RootPlaceId = root;
            ServerId = server;

            foreach (string name in PresenceProperties)
                Changed(name);
        }

        private void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
