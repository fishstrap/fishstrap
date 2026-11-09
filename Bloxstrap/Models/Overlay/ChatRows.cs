using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Bloxstrap.Models.Overlay
{
    public enum ChatLineState
    {
        Sent,
        Pending,
        Failed
    }

    public record ChatDayDivider(DateTime Day, string Text);

    public record ChatSystemLine(string Text);

    public class ChatMessageGroup
    {
        public long SenderId { get; init; }

        public string Sender { get; init; } = String.Empty;

        public string? Avatar { get; init; }

        public bool IsMine { get; init; }

        public DateTime? StartedAt { get; init; }

        public DateTime? LastAt { get; set; }

        public string TimeText => StartedAt is DateTime at ? at.ToLocalTime().ToString("t", Locale.CurrentCulture) : String.Empty;

        public ObservableCollection<ChatLine> Lines { get; } = new();
    }

    public class ChatLine : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Text { get; init; } = String.Empty;

        private ChatLineState _state = ChatLineState.Sent;

        public ChatLineState State
        {
            get => _state;
            set
            {
                _state = value;
                Changed(nameof(State));
                Changed(nameof(IsPending));
                Changed(nameof(IsFailed));
            }
        }

        public bool IsPending => _state == ChatLineState.Pending;

        public bool IsFailed => _state == ChatLineState.Failed;

        private string _failure = String.Empty;

        public string Failure
        {
            get => _failure;
            set
            {
                _failure = value;
                Changed(nameof(Failure));
            }
        }

        private void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
