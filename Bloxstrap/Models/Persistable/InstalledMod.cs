using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Bloxstrap.Models.Persistable
{
    public class InstalledMod : INotifyPropertyChanged
    {
        private string _name = "";
        private bool _enabled = true;
        private int _loadOrder;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public bool Enabled
        {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
        }

        public int LoadOrder
        {
            get => _loadOrder;
            set => SetProperty(ref _loadOrder, value);
        }

        public DateTime InstalledAt { get; set; } = DateTime.UtcNow;

        public List<string> Files { get; set; } = new();

        public string? SourcePath { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
