using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Bloxstrap.UI.Elements.Overlay
{
    public partial class OverlayToast : Window
    {
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private static readonly Duration SlideIn = TimeSpan.FromMilliseconds(220);

        private static readonly Duration SlideOut = TimeSpan.FromMilliseconds(250);

        private readonly DispatcherTimer _timer;

        private readonly TranslateTransform _slide = new();

        private HWND _hwnd;

        private ToastAppearance _appearance = ToastAppearance.Default;

        public IntPtr Handle => _hwnd;

        public event EventHandler? Finished;

        public bool IsShowing { get; private set; }

        public OverlayToast()
        {
            InitializeComponent();

            CardView.RenderTransform = _slide;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(ToastAppearance.Default.Duration) };
            _timer.Tick += (_, _) => Dismiss();
        }

        public void Present(string title, string message, Rect gameBounds) => Present(new OverlayNotice(title, message), gameBounds);

        public void Present(OverlayNotice notice, Rect gameBounds)
        {
            _appearance = ToastAppearance.Current;

            CardView.Apply(_appearance);
            CardView.Margin = _appearance.Margin;
            CardView.Show(notice);
            CardView.SetHeader(_appearance.ShowsHeader(notice.Kind));

            IsShowing = true;

            _timer.Stop();
            _timer.Interval = TimeSpan.FromSeconds(_appearance.Duration);

            BeginAnimation(OpacityProperty, null);
            _slide.BeginAnimation(TranslateTransform.YProperty, null);

            Opacity = 0;

            if (Visibility != Visibility.Visible)
                Show();

            UpdateLayout();

            Place(gameBounds);

            _slide.Y = Hidden;

            _slide.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(0, SlideIn) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });

            BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(150)));

            _timer.Start();
        }

        private double Hidden => _appearance.AtBottom
            ? CardView.ActualHeight + CardView.Margin.Bottom
            : -(CardView.ActualHeight + CardView.Margin.Top);

        public void Dismiss()
        {
            _timer.Stop();

            if (!IsShowing)
                return;

            IsShowing = false;

            _slide.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(Hidden, SlideOut) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } });

            var fade = new DoubleAnimation(0, SlideOut);

            fade.Completed += (_, _) =>
            {
                if (IsShowing)
                    return;

                Hide();

                Finished?.Invoke(this, EventArgs.Empty);
            };

            BeginAnimation(OpacityProperty, fade);
        }

        private void Place(Rect gameBounds)
        {
            Point topLeft = gameBounds.TopLeft;
            Point bottomRight = gameBounds.BottomRight;

            if (PresentationSource.FromVisual(this)?.CompositionTarget is CompositionTarget target)
            {
                topLeft = target.TransformFromDevice.Transform(topLeft);
                bottomRight = target.TransformFromDevice.Transform(bottomRight);
            }

            Left = _appearance.AtRight ? bottomRight.X - ActualWidth : topLeft.X;
            Top = _appearance.AtBottom ? bottomRight.Y - ActualHeight : topLeft.Y;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _hwnd = new HWND(new WindowInteropHelper(this).Handle);

            int exStyle = PInvoke.GetWindowLong(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);

            PInvoke.SetWindowLong(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE,
                exStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
        }
    }
}
