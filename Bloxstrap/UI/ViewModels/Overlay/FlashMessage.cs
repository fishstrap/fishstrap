using System.Windows.Threading;

namespace Bloxstrap.UI.ViewModels.Overlay
{
    public sealed class FlashMessage
    {
        private readonly DispatcherTimer _timer;

        private readonly Action _changed;

        public string? Text { get; private set; }

        public FlashMessage(TimeSpan duration, Action changed)
        {
            _changed = changed;

            _timer = new DispatcherTimer { Interval = duration };
            _timer.Tick += (_, _) => Set(null);
        }

        public void Show(string message)
        {
            _timer.Stop();

            Set(message);

            _timer.Start();
        }

        public void Clear()
        {
            if (Text is not null)
                Set(null);
        }

        private void Set(string? text)
        {
            if (text is null)
                _timer.Stop();

            Text = text;

            _changed();
        }
    }
}
