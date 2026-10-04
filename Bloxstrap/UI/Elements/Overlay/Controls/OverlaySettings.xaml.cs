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

            PreviewCard.SizeChanged += (_, _) => PlacePreview();

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

            PlacePreview();

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

        private void PreviewSizeChanged(object sender, SizeChangedEventArgs e)
        {
            PreviewArea.Clip = new RectangleGeometry(new Rect(e.NewSize), 7, 7);

            PlacePreview();
        }

        private void PlacePreview()
        {
            ToastAppearance appearance = _viewModel.Appearance;
            Thickness margin = appearance.Margin;

            var card = new Size(PreviewCard.ActualWidth + margin.Left + margin.Right, PreviewCard.ActualHeight + margin.Top + margin.Bottom);
            var area = new Rect(0, 0, PreviewArea.ActualWidth, PreviewArea.ActualHeight);

            Point origin = OverlayPlacement.Place(area, card, appearance.X, appearance.Y);

            Canvas.SetLeft(PreviewCard, origin.X + margin.Left);
            Canvas.SetTop(PreviewCard, origin.Y + margin.Top);
        }
    }
}
