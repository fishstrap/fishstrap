using System.ComponentModel;
using System.Windows;
using System.Windows.Shell;
using System.Windows.Forms;
using System.Windows.Threading;
using Bloxstrap.UI.Elements.Bootstrapper.Base;
using Bloxstrap.UI.Elements.Base;
using Bloxstrap.UI.ViewModels.Bootstrapper;
using LibVLCSharp.Shared;

namespace Bloxstrap.UI.Elements.Bootstrapper
{
    public partial class AnimatedVideoDialog : WpfUiWindow, IBootstrapperDialog
    {
        private readonly BootstrapperDialogViewModel _viewModel;
        private LibVLC? _libVlc;
        private MediaPlayer? _mediaPlayer;
        private Media? _media;
        private bool _isClosing;
        private bool _playbackStarted;
        private readonly string _videoPath;

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

        public static string? SelectVideoPath()
        {
            var settings = App.Settings.Prop;
            IEnumerable<string> configuredPaths = settings.BootstrapperVideoPaths.Count > 0
                ? settings.BootstrapperVideoPaths
                : new[] { settings.BootstrapperVideoPath };

            List<string> availablePaths = configuredPaths
                .Where(path => !String.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Where(path =>
                {
                    string extension = Path.GetExtension(path);
                    return extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase)
                        || extension.Equals(".mov", StringComparison.OrdinalIgnoreCase);
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (availablePaths.Count == 0)
                return null;

            if (settings.BootstrapperVideoShuffleEnabled && availablePaths.Count > 1)
                return availablePaths[Random.Shared.Next(availablePaths.Count)];

            return availablePaths[0];
        }

        public AnimatedVideoDialog(string videoPath)
        {
            _videoPath = videoPath;
            InitializeComponent();
            _viewModel = new BootstrapperDialogViewModel(this);
            DataContext = _viewModel;
            Title = App.ProjectName;

            Windows11Indicator.Visibility = App.Settings.Prop.BootstrapperVideoIndicatorStyle == BootstrapperVideoIndicatorStyle.Windows11 ? Visibility.Visible : Visibility.Collapsed;
            Windows10Indicator.Visibility = App.Settings.Prop.BootstrapperVideoIndicatorStyle == BootstrapperVideoIndicatorStyle.Windows10 ? Visibility.Visible : Visibility.Collapsed;

            try
            {
                if (!File.Exists(_videoPath))
                    throw new FileNotFoundException("The selected video file no longer exists.", _videoPath);

                // LibVLC ships its own codec stack, so playback does not depend on
                // the Windows Media Foundation codecs installed on the user's PC.
                Core.Initialize();
                _libVlc = new LibVLC("--quiet");
                _mediaPlayer = new MediaPlayer(_libVlc)
                {
                    Mute = !App.Settings.Prop.BootstrapperVideoAudioEnabled
                };
                _mediaPlayer.EndReached += MediaPlayer_EndReached;
                _mediaPlayer.EncounteredError += MediaPlayer_EncounteredError;

                // Do not start VLC until the WPF VideoView has loaded and attached
                // its native video host HWND. Starting earlier can make LibVLC
                // create its own top-level video window instead of using the
                // bootstrapper's embedded video surface.
                VideoSurface.Loaded += VideoSurface_Loaded;
            }
            catch (Exception ex)
            {
                ShowVideoFallback(ex);
            }
        }

        private void VideoSurface_Loaded(object sender, RoutedEventArgs e)
        {
            VideoSurface.Loaded -= VideoSurface_Loaded;

            if (_isClosing || _playbackStarted || _libVlc is null || _mediaPlayer is null)
                return;

            // Let WPF finish creating and attaching the native child video host before
            // LibVLC starts. Loaded alone can fire before the host HWND is ready.
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(StartPlayback));
        }

        private void StartPlayback()
        {
            if (_isClosing || _playbackStarted || _libVlc is null || _mediaPlayer is null)
                return;

            try
            {
                VideoSurface.UpdateLayout();
                VideoSurface.MediaPlayer = _mediaPlayer;
                _media = new Media(_libVlc, _videoPath, FromType.FromPath);
                _playbackStarted = true;

                if (!_mediaPlayer.Play(_media))
                    throw new InvalidOperationException("VLC could not start the selected video in the embedded bootstrapper surface.");
            }
            catch (Exception ex)
            {
                ShowVideoFallback(ex);
            }
        }

        private void MediaPlayer_EndReached(object? sender, EventArgs e)
        {
            // LibVLC raises EndReached from a worker thread; marshal looping to WPF.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_isClosing || _mediaPlayer is null || _media is null || !App.Settings.Prop.BootstrapperVideoLoopEnabled)
                    return;

                // Replay the same media explicitly; Play() without a media instance
                // can leave the player stopped after EndReached on some VLC builds.
                if (!_mediaPlayer.Play(_media))
                    ShowVideoFallback(new InvalidOperationException("VLC could not restart the selected video."));
            }));
        }

        private void MediaPlayer_EncounteredError(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
                ShowVideoFallback(new InvalidOperationException("The selected video could not be decoded by the VLC playback engine."))));
        }

        private void ShowVideoFallback(Exception exception)
        {
            App.Logger.WriteException("AnimatedVideoDialog::ShowVideoFallback", exception);
            _mediaPlayer?.Stop();
            if (VideoSurface is not null)
                VideoSurface.Visibility = Visibility.Collapsed;
            Windows11Indicator.Visibility = Visibility.Collapsed;
            Windows10Indicator.Visibility = Visibility.Collapsed;
            FallbackPanel.Visibility = Visibility.Visible;
        }

        private void UiWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!_isClosing)
                Bootstrapper?.Cancel();

            _isClosing = true;
            VideoSurface.Loaded -= VideoSurface_Loaded;

            if (_mediaPlayer is not null)
            {
                _mediaPlayer.EndReached -= MediaPlayer_EndReached;
                _mediaPlayer.EncounteredError -= MediaPlayer_EncounteredError;
                _mediaPlayer.Stop();
            }

            VideoSurface.MediaPlayer = null;
            _media?.Dispose();
            _mediaPlayer?.Dispose();
            _libVlc?.Dispose();

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
