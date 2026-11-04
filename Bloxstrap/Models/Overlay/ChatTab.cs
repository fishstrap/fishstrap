using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Bloxstrap.Models.Overlay
{
    public class ChatTab : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ChatFriend Friend { get; init; } = null!;

        public ObservableCollection<object> Rows { get; } = new();

        public HashSet<string> KnownMessages { get; } = new(StringComparer.Ordinal);

        public bool Loaded { get; set; }

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                Changed(nameof(IsSelected));
            }
        }

        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                Changed(nameof(IsLoading));
                Changed(nameof(ShowEmpty));
            }
        }

        private string _draft = String.Empty;

        public string Draft
        {
            get => _draft;
            set
            {
                _draft = value;
                Changed(nameof(Draft));
                Changed(nameof(CanSend));
            }
        }

        public bool CanSend => !String.IsNullOrWhiteSpace(_draft);

        public bool ShowEmpty => !_isLoading && Rows.Count == 0;

        public string EmptyText => String.Format(Strings.Menu_Overlay_Messages_NoHistory, Friend.DisplayName);

        public void RowsChanged() => Changed(nameof(ShowEmpty));

        private void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
