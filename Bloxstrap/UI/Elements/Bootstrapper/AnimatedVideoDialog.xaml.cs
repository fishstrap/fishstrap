using System.ComponentModel;
using System.Windows;
using System.Windows.Shell;
using System.Windows.Forms;
using Bloxstrap.UI.Elements.Bootstrapper.Base;
using Bloxstrap.UI.Elements.Base;
using Bloxstrap.UI.ViewModels.Bootstrapper;

namespace Bloxstrap.UI.Elements.Bootstrapper
{
    public partial class AnimatedVideoDialog : WpfUiWindow, IBootstrapperDialog
    {
        private readonly BootstrapperDialogViewModel _viewModel;
        private bool _isClosing;

        public Bloxstrap.Bootstrapper? Bootstrapper { get; set; }

        public string Message
        {
            get => _viewModel.Message;
            set { _viewModel.Message = value; _viewModel.OnPropertyChanged(nameof(_viewModel.Message)); }
        }

        public ProgressBarStyle ProgressStyle
        {
            get => _viewModel.ProgressIndeterminate ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
            set { _viewModel.ProgressIndeterminate = value == ProgressBarStyle.Marquee; _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressIndeterminate)); }
        }

        public int ProgressMaximum
        {
            get => _viewModel.ProgressMaximum;
            set { _viewModel.ProgressMaximum = value; _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressMaximum)); }
        }

        public int ProgressValue
        {
            get => _viewModel.ProgressValue;
            set { _viewModel.ProgressValue = value; _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressValue)); }
        }

        public TaskbarItemProgressState TaskbarProgressState
        {
            get => _viewModel.TaskbarProgressState;
            set { _viewModel.TaskbarProgressState = value; _viewModel.OnPropertyChanged(nameof(_viewModel.TaskbarProgressState)); }
        }

        public double TaskbarProgressValue
        {
            get => _viewModel.TaskbarProgressValue;
            set { _viewModel.TaskbarProgressValue = value; _viewModel.OnPropertyChanged(nameof(_viewModel.TaskbarProgressValue)); }
        }

        public bool CancelEnabled
        {
            get => _viewModel.CancelEnabled;
            set
            {
                _viewModel.CancelEnabled = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.CancelButtonVisibility));
                _viewModel.OnPropertyChanged(nameof(_viewModel.CancelEnabled));
            }
        }

        public AnimatedVideoDialog()
        {
            InitializeComponent();
            _viewModel = new BootstrapperDialogViewModel(this);
            DataContext = _viewModel;
            Title = App.ProjectName;

            Windows11Indicator.Visibility = App.Settings.Prop.BootstrapperVideoIndicatorStyle == BootstrapperVideoIndicatorStyle.Windows11 ? Visibility.Visible : Visibility.Collapsed;
            Windows10Indicator.Visibility = App.Settings.Prop.BootstrapperVideoIndicatorStyle == BootstrapperVideoIndicatorStyle.Windows10 ? Visibility.Visible : Visibility.Collapsed;

            try
            {
                string path = App.Settings.Prop.BootstrapperVideoPath;
                if (!File.Exists(path))
                    throw new FileNotFoundException("The selected video file no longer exists.", path);

                VideoSurface.Source = new Uri(path, UriKind.Absolute);
            }
            catch (Exception ex)
            {
                ShowVideoFallback(ex);
            }
        }

        private void VideoSurface_MediaOpened(object sender, RoutedEventArgs e)
        {
            VideoSurface.Play();
        }

        private void VideoSurface_MediaEnded(object sender, RoutedEventArgs e)
        {
            VideoSurface.Position = TimeSpan.Zero;
            VideoSurface.Play();
        }

        private void VideoSurface_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ShowVideoFallback(e.ErrorException);
        }

        private void ShowVideoFallback(Exception exception)
        {
            App.Logger.WriteException("AnimatedVideoDialog::ShowVideoFallback", exception);
            VideoSurface.Stop();
            VideoSurface.Visibility = Visibility.Collapsed;
            Windows11Indicator.Visibility = Visibility.Collapsed;
            Windows10Indicator.Visibility = Visibility.Collapsed;
            FallbackPanel.Visibility = Visibility.Visible;
        }

        private void UiWindow_Closing(object sender, CancelEventArgs e)
        {
            VideoSurface.Stop();
            if (!_isClosing)
                Bootstrapper?.Cancel();
        }

        public void ShowBootstrapper() => ShowDialog();

        public void CloseBootstrapper()
        {
            _isClosing = true;
            Dispatcher.BeginInvoke(Close);
        }

        public void ShowSuccess(string message, Action? callback) => BaseFunctions.ShowSuccess(message, callback);
    }
}
