using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using Wpf.Ui.Common;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public partial class OverlayPanel : UserControl
    {
        public const double MinPanelWidth = 380;
        public const double MinPanelHeight = 260;

        private static int _topmostZIndex;

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(OverlayPanel), new PropertyMetadata(String.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon), typeof(SymbolRegular), typeof(OverlayPanel), new PropertyMetadata(SymbolRegular.Empty));

        public SymbolRegular Icon
        {
            get => (SymbolRegular)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly DependencyProperty PanelContentProperty = DependencyProperty.Register(
            nameof(PanelContent), typeof(object), typeof(OverlayPanel), new PropertyMetadata(null));

        public object? PanelContent
        {
            get => GetValue(PanelContentProperty);
            set => SetValue(PanelContentProperty, value);
        }

        public static readonly DependencyProperty ContentPaddingProperty = DependencyProperty.Register(
            nameof(ContentPadding), typeof(Thickness), typeof(OverlayPanel), new PropertyMetadata(new Thickness(12)));

        public Thickness ContentPadding
        {
            get => (Thickness)GetValue(ContentPaddingProperty);
            set => SetValue(ContentPaddingProperty, value);
        }

        public static readonly DependencyProperty CloseCommandProperty = DependencyProperty.Register(
            nameof(CloseCommand), typeof(ICommand), typeof(OverlayPanel), new PropertyMetadata(null));

        public ICommand? CloseCommand
        {
            get => (ICommand?)GetValue(CloseCommandProperty);
            set => SetValue(CloseCommandProperty, value);
        }

        public static readonly DependencyProperty CloseCommandParameterProperty = DependencyProperty.Register(
            nameof(CloseCommandParameter), typeof(object), typeof(OverlayPanel), new PropertyMetadata(null));

        public object? CloseCommandParameter
        {
            get => GetValue(CloseCommandParameterProperty);
            set => SetValue(CloseCommandParameterProperty, value);
        }

        public static readonly DependencyProperty IsPinnedProperty = DependencyProperty.Register(
            nameof(IsPinned), typeof(bool), typeof(OverlayPanel), new PropertyMetadata(false, (d, _) => ((OverlayPanel)d).ShowPin()));

        public bool IsPinned
        {
            get => (bool)GetValue(IsPinnedProperty);
            set => SetValue(IsPinnedProperty, value);
        }

        public static readonly DependencyProperty PinCommandProperty = DependencyProperty.Register(
            nameof(PinCommand), typeof(ICommand), typeof(OverlayPanel), new PropertyMetadata(null));

        public ICommand? PinCommand
        {
            get => (ICommand?)GetValue(PinCommandProperty);
            set => SetValue(PinCommandProperty, value);
        }

        public static readonly DependencyProperty PinCommandParameterProperty = DependencyProperty.Register(
            nameof(PinCommandParameter), typeof(object), typeof(OverlayPanel), new PropertyMetadata(null));

        public object? PinCommandParameter
        {
            get => GetValue(PinCommandParameterProperty);
            set => SetValue(PinCommandParameterProperty, value);
        }

        private Point _dragStart;
        private Point _dragOrigin;
        private bool _dragging;

        public OverlayPanel()
        {
            InitializeComponent();

            HeaderIcon.SetBinding(Wpf.Ui.Controls.SymbolIcon.SymbolProperty, new Binding(nameof(Icon)) { Source = this });
            HeaderTitle.SetBinding(TextBlock.TextProperty, new Binding(nameof(Title)) { Source = this });

            ShowPin();
        }

        private Canvas? Surface => Parent as Canvas;

        public Point Position => new(Or(Canvas.GetLeft(this), 0), Or(Canvas.GetTop(this), 0));

        public Size PanelSize => new(Or(Width, ActualWidth), Or(Height, ActualHeight));

        public int Depth => Panel.GetZIndex(this);

        public Size SurfaceSize => Surface is null ? Size.Empty : new Size(Surface.ActualWidth, Surface.ActualHeight);

        private static double Or(double value, double fallback) => Double.IsNaN(value) ? fallback : value;

        private void BringToFront(object sender, MouseButtonEventArgs e) => Raise();

        public void Raise() => Panel.SetZIndex(this, ++_topmostZIndex);

        public void PlaceAt(double left, double top, double width, double height)
        {
            Width = width;
            Height = height;

            Canvas.SetLeft(this, left);
            Canvas.SetTop(this, top);

            Clamp();
            Raise();
        }

        #region Dragging

        private void TitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Surface is null)
                return;

            _dragging = true;
            _dragStart = e.GetPosition(Surface);
            _dragOrigin = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));

            TitleBar.CaptureMouse();
        }

        private void TitleBarMouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging || Surface is null)
                return;

            Vector delta = e.GetPosition(Surface) - _dragStart;

            Canvas.SetLeft(this, _dragOrigin.X + delta.X);
            Canvas.SetTop(this, _dragOrigin.Y + delta.Y);

            Clamp();
        }

        private void TitleBarMouseUp(object sender, MouseButtonEventArgs e)
        {
            _dragging = false;

            TitleBar.ReleaseMouseCapture();
        }

        #endregion

        #region Resizing

        private void ResizeDragDelta(object sender, DragDeltaEventArgs e)
        {
            if (sender is not Thumb thumb || thumb.Tag is not string edge || Surface is null)
                return;

            if (edge.Contains("Left"))
            {
                double applied = Grow(ActualWidth, -e.HorizontalChange, MinPanelWidth, out double width);

                Width = width;

                Canvas.SetLeft(this, Canvas.GetLeft(this) - applied);
            }
            else if (edge.Contains("Right"))
            {
                Grow(ActualWidth, e.HorizontalChange, MinPanelWidth, out double width);

                Width = width;
            }

            if (edge.Contains("Top"))
            {
                double applied = Grow(ActualHeight, -e.VerticalChange, MinPanelHeight, out double height);

                Height = height;

                Canvas.SetTop(this, Canvas.GetTop(this) - applied);
            }
            else if (edge.Contains("Bottom"))
            {
                Grow(ActualHeight, e.VerticalChange, MinPanelHeight, out double height);

                Height = height;
            }

            Clamp();
        }

        private static double Grow(double current, double delta, double min, out double result)
        {
            result = Math.Max(current + delta, min);

            return result - current;
        }

        #endregion

        public void Clamp()
        {
            if (Surface is null || Surface.ActualWidth == 0)
                return;

            double width = Math.Min(Double.IsNaN(Width) ? ActualWidth : Width, Surface.ActualWidth);
            double height = Math.Min(Double.IsNaN(Height) ? ActualHeight : Height, Surface.ActualHeight);

            if (width > 0)
                Width = Math.Max(width, MinPanelWidth);

            if (height > 0)
                Height = Math.Max(height, MinPanelHeight);

            Canvas.SetLeft(this, Math.Clamp(Canvas.GetLeft(this), 0, Math.Max(Surface.ActualWidth - Width, 0)));
            Canvas.SetTop(this, Math.Clamp(Canvas.GetTop(this), 0, Math.Max(Surface.ActualHeight - Height, 0)));
        }

        private void BodySizeChanged(object sender, SizeChangedEventArgs e) =>
            Body.Clip = new RectangleGeometry(new Rect(e.NewSize), 7, 7);

        private void ShowPin()
        {
            PinIcon.Filled = IsPinned;
            PinButton.ToolTip = IsPinned ? Strings.Menu_Overlay_Unpin : Strings.Menu_Overlay_Pin;

            if (IsPinned)
                PinIcon.SetResourceReference(ForegroundProperty, "AccentFillColorDefaultBrush");
            else
                PinIcon.ClearValue(ForegroundProperty);
        }

        private void PinClicked(object sender, RoutedEventArgs e)
        {
            if (PinCommand?.CanExecute(PinCommandParameter) == true)
                PinCommand.Execute(PinCommandParameter);
        }

        private void CloseClicked(object sender, RoutedEventArgs e)
        {
            if (CloseCommand?.CanExecute(CloseCommandParameter) == true)
                CloseCommand.Execute(CloseCommandParameter);
        }
    }
}
