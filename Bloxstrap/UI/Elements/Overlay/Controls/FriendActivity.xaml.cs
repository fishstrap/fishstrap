using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using Bloxstrap.UI.ViewModels.Overlay.Controls;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public partial class FriendActivity : UserControl
    {
        private FriendActivityViewModel _viewModel;

        public FriendActivity()
        {
            _viewModel = new FriendActivityViewModel(null);

            DataContext = _viewModel;

            InitializeComponent();

            IsVisibleChanged += async (_, _) =>
            {
                if (IsVisible)
                    await _viewModel.LoadAsync();
            };
        }

        public void Attach(Integrations.Overlay? overlay)
        {
            _viewModel.MessagesAdded -= OnMessagesAdded;
            _viewModel.PropertyChanged -= OnViewModelChanged;

            _viewModel = new FriendActivityViewModel(overlay);
            _viewModel.MessagesAdded += OnMessagesAdded;
            _viewModel.PropertyChanged += OnViewModelChanged;

            DataContext = _viewModel;
        }

        private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(FriendActivityViewModel.SelectedTab))
                return;

            ScrollToEnd();

            if (_viewModel.HasTab)
                Dispatcher.BeginInvoke(() => DraftBox.Focus(), DispatcherPriority.Input);
        }

        private void OnMessagesAdded(object? sender, EventArgs e) => ScrollToEnd();

        private void ScrollToEnd() => Dispatcher.BeginInvoke(() => MessageScroller.ScrollToEnd(), DispatcherPriority.Loaded);

        private void DraftKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                return;

            e.Handled = true;

            _viewModel.SendCommand.Execute(null);
        }
    }
}
