using System.ComponentModel;

namespace Bloxstrap.Models.Overlay
{
    public class PrivateServerItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public long VipServerId { get; init; }

        public string AccessCode { get; init; } = String.Empty;

        public string Name { get; init; } = String.Empty;

        public long OwnerId { get; init; }

        public string CapacityText { get; init; } = String.Empty;

        public bool IsMine { get; init; }

        public bool IsCurrent { get; init; }

        public bool CanJoin => !String.IsNullOrEmpty(AccessCode) && !IsCurrent && IsActive;

        private bool _isActive = true;

        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                Changed(nameof(IsActive));
                Changed(nameof(CanJoin));
            }
        }

        private string _link = String.Empty;

        public string Link
        {
            get => _link;
            set
            {
                _link = value;
                Changed(nameof(Link));
                Changed(nameof(HasLink));
            }
        }

        public bool HasLink => !String.IsNullOrEmpty(_link);

        private bool _isWorking;

        public bool IsWorking
        {
            get => _isWorking;
            set
            {
                _isWorking = value;
                Changed(nameof(IsWorking));
                Changed(nameof(IsIdle));
            }
        }

        public bool IsIdle => !_isWorking;

        private string? _ownerAvatar;

        public string? OwnerAvatar
        {
            get => _ownerAvatar;
            set
            {
                _ownerAvatar = value;
                Changed(nameof(OwnerAvatar));
            }
        }

        private void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
