using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using Bloxstrap.UI.ViewModels.Overlay.Controls;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public partial class OverlaySettings : UserControl
    {
        private const double PreviewInset = 12;

        private OverlaySettingsViewModel _viewModel;

        private Func<string?> _backdrop = () => null;

        private GameOverlay? _window;

        private OverlayNotice? _shownSample;

        private string? _shownBackdrop;

        public OverlaySettings()
        {
            _viewModel = new OverlaySettingsViewModel(null, null, () => null);

            DataContext = _viewModel;

            InitializeComponent();

            _viewModel.AppearanceChanged += (_, _) => RefreshPreview();
            _viewModel.AnimationPicked += (_, _) => ReplayPreview();

            IsVisibleChanged += (_, _) =>
            {
                if (IsVisible)
                {
                    RefreshPreview();
                    _viewModel.RefreshHotkeyStatus();
                }
                else
                {
                    _viewModel.Save();
                }
            };

            ShortcutBox.RecordingChanged += (_, _) =>
            {
                if (ShortcutBox.IsRecording)
                    _window?.SuspendHotkey();
                else
                    _viewModel.RebindHotkey();
            };

            RefreshPreview();
        }

        public void Attach(Integrations.Overlay? overlay, GameOverlay? window, Func<string?> avatar, Func<string?> backdrop)
        {
            _window = window;

            _viewModel = new OverlaySettingsViewModel(overlay, window, avatar);
            _viewModel.AppearanceChanged += (_, _) => RefreshPreview();
            _viewModel.AnimationPicked += (_, _) => ReplayPreview();

            _backdrop = backdrop;

            DataContext = _viewModel;

            RefreshPreview();
        }

        public void Flush() => _viewModel.Save();

        private void RefreshPreview()
        {
            ToastAppearance appearance = _viewModel.Appearance;

            PreviewCard.Apply(appearance);

            OverlayNotice sample = _viewModel.Sample;

            if (sample != _shownSample)
            {
                PreviewCard.Show(sample);
                _shownSample = sample;
            }

            PreviewCard.SetHeader(appearance.ShowsHeader(sample.Kind));

            PreviewCard.HorizontalAlignment = appearance.AtRight ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            PreviewCard.VerticalAlignment = appearance.AtBottom ? VerticalAlignment.Bottom : VerticalAlignment.Top;
            PreviewCard.Margin = new Thickness(appearance.Docked ? 0 : PreviewInset);

            string? backdrop = _backdrop();

            if (backdrop == _shownBackdrop)
                return;

            _shownBackdrop = backdrop;

            Backdrop.Background = !String.IsNullOrEmpty(backdrop) && Uri.TryCreate(backdrop, UriKind.Absolute, out Uri? source)
                ? new ImageBrush(new BitmapImage(source)) { Stretch = Stretch.UniformToFill }
                : null;
        }

        public void ReplayPreview() =>
            Dispatcher.BeginInvoke(() => ToastMotion.Enter(PreviewCard, _viewModel.Appearance), DispatcherPriority.Loaded);

        private void PreviewClicked(object sender, MouseButtonEventArgs e) => ReplayPreview();

        private void PreviewSizeChanged(object sender, SizeChangedEventArgs e) =>
            PreviewArea.Clip = new RectangleGeometry(new Rect(e.NewSize), 7, 7);
    }
}
