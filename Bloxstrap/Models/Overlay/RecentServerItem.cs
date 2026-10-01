using System.ComponentModel;

namespace Bloxstrap.Models.Overlay
{
    public class RecentServerItem : INotifyPropertyChanged
    {
        public enum ServerStatus
        {
            Checking,
            Running,
            Missing
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public long PlaceId { get; init; }

        public string JobId { get; init; } = String.Empty;

        public DateTime JoinedAt { get; init; }

        public DateTime? LeftAt { get; init; }

        public string WhenText
        {
            get
            {
                DateTime local = JoinedAt.ToLocalTime();
                string time = local.ToString("t", Locale.CurrentCulture);

                if (local.Date == DateTime.Today)
                    return String.Format(Strings.Menu_Overlay_Servers_Recent_Today, time);

                if (local.Date == DateTime.Today.AddDays(-1))
                    return String.Format(Strings.Menu_Overlay_Servers_Recent_Yesterday, time);

                return $"{local.ToString("d", Locale.CurrentCulture)}, {time}";
            }
        }

        public string IdText => String.Format(Strings.Menu_Overlay_Servers_Id, JobId);

        private static readonly string[] StatusProperties =
        {
            nameof(Status), nameof(Playing), nameof(MaxPlayers), nameof(IsRunning), nameof(IsMissing), nameof(IsFull), nameof(CanJoin), nameof(JoinText), nameof(DetailText)
        };

        private ServerStatus _status = ServerStatus.Checking;

        public ServerStatus Status => _status;

        public int? Playing { get; private set; }

        public int? MaxPlayers { get; private set; }

        public bool IsRunning => _status == ServerStatus.Running;

        public bool IsMissing => _status == ServerStatus.Missing;

        public bool IsFull => Playing is not null && MaxPlayers is not null && Playing >= MaxPlayers;

        public bool CanJoin => !IsFull;

        public string JoinText => IsFull ? Strings.Menu_Overlay_Servers_Full : Strings.Menu_Overlay_Servers_Join;

        public string DetailText
        {
            get
            {
                string status = _status switch
                {
                    ServerStatus.Running when Playing is not null && MaxPlayers is not null
                        => String.Format(Strings.Menu_Overlay_Servers_Capacity, Playing, MaxPlayers),
                    ServerStatus.Running => Strings.Menu_Overlay_Servers_Recent_Running,
                    ServerStatus.Missing => Strings.Menu_Overlay_Servers_Recent_Missing,
                    _ => Strings.Menu_Overlay_Servers_Recent_Checking
                };

                if (LeftAt is not DateTime left || left - JoinedAt < TimeSpan.FromMinutes(1))
                    return status;

                return $"{status} · {String.Format(Strings.Menu_Overlay_Servers_Recent_Played, Time.FormatTimeSpan(left - JoinedAt))}";
            }
        }

        public void SetStatus(ServerStatus status, int? playing = null, int? maxPlayers = null)
        {
            if (_status == status && Playing == playing && MaxPlayers == maxPlayers)
                return;

            _status = status;
            Playing = playing;
            MaxPlayers = maxPlayers;

            foreach (string name in StatusProperties)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
