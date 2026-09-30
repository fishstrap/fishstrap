using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public class DockHintLayer : Canvas
    {
        private static readonly TimeSpan ShowDelay = TimeSpan.FromMilliseconds(300);

        private static readonly TimeSpan HideGrace = TimeSpan.FromMilliseconds(150);

        private static readonly Duration Fade = TimeSpan.FromMilliseconds(120);

        private const double Gap = 10;

        private const double Edge = 4;

        public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
            "Text", typeof(string), typeof(DockHintLayer), new PropertyMetadata(null, OnTextChanged));

        public static string? GetText(DependencyObject target) => (string?)target.GetValue(TextProperty);

        public static void SetText(DependencyObject target, string? value) => target.SetValue(TextProperty, value);

        private readonly Border _bubble;

        private readonly TextBlock _label;

        private readonly DispatcherTimer _showTimer;

        private readonly DispatcherTimer _hideTimer;

        private FrameworkElement? _target;

        private bool _shown;

        public bool IsShowing => _shown;

        public string? ShowingText => _shown ? _label.Text : null;

        public DockHintLayer()
        {
            IsHitTestVisible = false;

            _label = new TextBlock { FontSize = 13 };
            _label.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");

            _bubble = new Border
            {
                Child = _label,
                Padding = new Thickness(10, 5, 10, 6),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                Opacity = 0,
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 12, ShadowDepth = 2, Direction = 270, Opacity = 0.4, Color = Colors.Black }
            };
            _bubble.SetResourceReference(Border.BackgroundProperty, "SolidBackgroundFillColorBaseBrush");
            _bubble.SetResourceReference(Border.BorderBrushProperty, "SurfaceStrokeColorDefaultBrush");

            Children.Add(_bubble);

            _showTimer = new DispatcherTimer { Interval = ShowDelay };
            _showTimer.Tick += (_, _) => Show();

            _hideTimer = new DispatcherTimer { Interval = HideGrace };
            _hideTimer.Tick += (_, _) => Hide(false);

            IsVisibleChanged += (_, _) =>
            {
                if (!IsVisible)
                    Hide(true);
            };
        }

        private static void OnTextChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is not FrameworkElement element)
                return;

            element.MouseEnter -= OnEnter;
            element.MouseLeave -= OnLeave;
            element.IsVisibleChanged -= OnTargetVisibilityChanged;

            if (e.NewValue is not string text || String.IsNullOrEmpty(text))
                return;

            element.MouseEnter += OnEnter;
            element.MouseLeave += OnLeave;
            element.IsVisibleChanged += OnTargetVisibilityChanged;
        }

        private static void OnEnter(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element)
                FindLayer(element)?.Hover(element);
        }

        private static void OnLeave(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element)
                FindLayer(element)?.Unhover(element, false);
        }

        private static void OnTargetVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is FrameworkElement { IsVisible: false } element)
                FindLayer(element)?.Unhover(element, true);
        }

        private static DockHintLayer? FindLayer(DependencyObject element)
        {
            for (DependencyObject? current = element; current is not null; current = VisualTreeHelper.GetParent(current))
            {
                if (current is Panel panel && panel.Children.OfType<DockHintLayer>().FirstOrDefault() is DockHintLayer layer)
                    return layer;
            }

            return null;
        }

        private void Hover(FrameworkElement target)
        {
            _hideTimer.Stop();

            _target = target;
            _label.Text = GetText(target);

            if (_shown)
            {
                Place();
                return;
            }

            _showTimer.Stop();
            _showTimer.Start();
        }

        private void Unhover(FrameworkElement target, bool instant)
        {
            if (_target != target)
                return;

            _showTimer.Stop();

            if (instant || !_shown)
            {
                Hide(instant);
                return;
            }

            _hideTimer.Stop();
            _hideTimer.Start();
        }

        private void Show()
        {
            _showTimer.Stop();

            if (_target is null || !_target.IsVisible || !IsVisible)
                return;

            Place();

            _shown = true;
            _bubble.BeginAnimation(OpacityProperty, new DoubleAnimation(1, Fade));
        }

        private void Hide(bool instant)
        {
            _showTimer.Stop();
            _hideTimer.Stop();

            _target = null;

            if (!_shown && !instant)
                return;

            _shown = false;

            if (instant)
            {
                _bubble.BeginAnimation(OpacityProperty, null);
                _bubble.Opacity = 0;
            }
            else
            {
                _bubble.BeginAnimation(OpacityProperty, new DoubleAnimation(0, Fade));
            }
        }

        private void Place()
        {
            if (_target is null)
                return;

            _bubble.InvalidateMeasure();
            _bubble.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            Size size = _bubble.DesiredSize;

            Point centre = _target.TranslatePoint(new Point(_target.ActualWidth / 2, 0), this);
            double top = BarOf(_target).TranslatePoint(new Point(0, 0), this).Y;

            double left = Math.Clamp(centre.X - size.Width / 2, Edge, Math.Max(Edge, ActualWidth - size.Width - Edge));

            SetLeft(_bubble, Math.Round(left));
            SetTop(_bubble, Math.Round(top - size.Height - Gap));
        }

        private FrameworkElement BarOf(FrameworkElement target)
        {
            FrameworkElement bar = target;

            for (DependencyObject? current = target; current is not null && current != Parent; current = VisualTreeHelper.GetParent(current))
            {
                if (current is FrameworkElement element)
                    bar = element;
            }

            return bar;
        }
    }
}
