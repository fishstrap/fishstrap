using System.ComponentModel;

namespace Bloxstrap.Models.Overlay
{
    public class FriendListHeader : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Title { get; init; } = String.Empty;

        private int _count;

        public int Count
        {
            get => _count;
            set
            {
                _count = value;
                Changed(nameof(Count));
                Changed(nameof(CountText));
            }
        }

        public string CountText => $"({_count})";

        private bool _isExpanded = true;

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                Changed(nameof(IsExpanded));
            }
        }

        private void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
