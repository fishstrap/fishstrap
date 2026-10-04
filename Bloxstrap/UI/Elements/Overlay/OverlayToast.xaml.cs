using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

using Bloxstrap.UI.Elements.Overlay.Controls;

namespace Bloxstrap.UI.Elements.Overlay
{
    public partial class OverlayToast : Window
    {
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private static readonly TimeSpan LingerAfterHover = TimeSpan.FromSeconds(2);

        private readonly DispatcherTimer _timer;

        private HWND _hwnd;

        private ToastAppearance _appearance = ToastAppearance.Default;

        private OverlayNotice? _notice;

        public event EventHandler? Finished;

        public bool IsShowing { get; private set; }

        public bool IsClickable => _notice?.IsClickable == true;

        public OverlayToast()
        {
            InitializeComponent();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(ToastAppearance.Default.Duration) };
            _timer.Tick += (_, _) => Dismiss();

            CardView.MouseEnter += (_, _) =>
            {
                if (IsShowing && IsClickable)
                    _timer.Stop();
            };

            CardView.MouseLeave += (_, _) =>
            {
                if (!IsShowing || !IsClickable)
                    return;

                _timer.Interval = LingerAfterHover;
                _timer.Start();
            };

            CardView.MouseLeftButtonUp += (_, _) => Click();
        }

        public void Present(OverlayNotice notice, Rect gameBounds)
        {
            _appearance = ToastAppearance.Current;
            _notice = notice;

            CardView.Apply(_appearance);
            CardView.Margin = _appearance.Margin;
            CardView.Show(notice);
            CardView.SetHeader(_appearance.ShowsHeader(notice.Kind));
            CardView.Cursor = notice.IsClickable ? Cursors.Hand : null;

            SetClickThrough(!notice.IsClickable);

            IsShowing = true;

            _timer.Stop();
            _timer.Interval = TimeSpan.FromSeconds(_appearance.Duration);

            ToastMotion.Hide(CardView);

            if (Visibility != Visibility.Visible)
                Show();

            UpdateLayout();

            Place(gameBounds);

            ToastMotion.Enter(CardView, _appearance);

            _timer.Start();
        }

        public void Click()
        {
            const string LOG_IDENT = "OverlayToast::Click";

            if (!IsShowing || _notice?.OnClick is not Action click)
                return;

            try
            {
                click();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "The notification's action failed");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            Dismiss();
        }

        public void Dismiss()
        {
            _timer.Stop();

            if (!IsShowing)
                return;

            IsShowing = false;

            ToastMotion.Leave(CardView, _appearance, () =>
            {
                if (IsShowing)
                    return;

                Hide();

                Finished?.Invoke(this, EventArgs.Empty);
            });
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

            Point origin = OverlayPlacement.Place(new Rect(topLeft, bottomRight), new Size(ActualWidth, ActualHeight), _appearance.X, _appearance.Y);

            Left = origin.X;
            Top = origin.Y;
        }

        private void SetClickThrough(bool clickThrough)
        {
            if (_hwnd == HWND.Null)
                return;

            int exStyle = PInvoke.GetWindowLong(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);

            exStyle = clickThrough ? exStyle | WS_EX_TRANSPARENT : exStyle & ~WS_EX_TRANSPARENT;

            PInvoke.SetWindowLong(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, exStyle);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _hwnd = new HWND(new WindowInteropHelper(this).Handle);

            int exStyle = PInvoke.GetWindowLong(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);

            PInvoke.SetWindowLong(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE,
                exStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | (IsClickable ? 0 : WS_EX_TRANSPARENT));
        }
    }
}
