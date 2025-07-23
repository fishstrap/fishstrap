using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Bloxstrap.Models
{
    public class GradientStopData : INotifyPropertyChanged
    {
        private double _offset;
        public double Offset
        {
            get => _offset;
            set
            {
                if (_offset != value)
                {
                    _offset = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Offset));
                }
            }
        }

        private string _color = "#";
        public string Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Color));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}