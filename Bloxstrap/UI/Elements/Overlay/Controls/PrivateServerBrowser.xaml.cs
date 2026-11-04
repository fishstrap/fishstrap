using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Bloxstrap.Integrations;
using Bloxstrap.UI.ViewModels.Overlay.Controls;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public partial class PrivateServerBrowser : UserControl
    {
        public static readonly DependencyProperty TopInsetProperty = DependencyProperty.Register(
            nameof(TopInset), typeof(double), typeof(PrivateServerBrowser), new PropertyMetadata(0d));

        public double TopInset
        {
            get => (double)GetValue(TopInsetProperty);
            set => SetValue(TopInsetProperty, value);
        }

        private PrivateServersViewModel _viewModel;

        public PrivateServerBrowser()
        {
            _viewModel = new PrivateServersViewModel(null);

            DataContext = _viewModel;

            InitializeComponent();

            IsVisibleChanged += (_, _) => _viewModel.SetVisible(IsVisible);
        }

        public void Attach(ActivityWatcher? activityWatcher)
        {
            _viewModel = new PrivateServersViewModel(activityWatcher);

            DataContext = _viewModel;

            _viewModel.SetVisible(IsVisible);
        }

        private void NameBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                _viewModel.SaveNameCommand.Execute(null);
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                _viewModel.CancelNameCommand.Execute(null);
            }
        }

        private void AddUsernameKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;

            _viewModel.AddUserCommand.Execute(null);
        }
    }
}
