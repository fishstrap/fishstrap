using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public class AcrylicBackdrop : Grid
    {
        private const int GrainSize = 96;

        private static readonly Lazy<Brush> Grain = new(CreateGrain);

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source), typeof(FrameworkElement), typeof(AcrylicBackdrop), new PropertyMetadata(null, OnSourceChanged));

        public static readonly DependencyProperty BlurRadiusProperty = DependencyProperty.Register(
            nameof(BlurRadius), typeof(double), typeof(AcrylicBackdrop), new PropertyMetadata(20d, OnLookChanged));

        private static readonly DependencyPropertyKey ContentInsetKey = DependencyProperty.RegisterReadOnly(
            nameof(ContentInset), typeof(Thickness), typeof(AcrylicBackdrop), new PropertyMetadata(new Thickness()));

        public static readonly DependencyProperty ContentInsetProperty = ContentInsetKey.DependencyProperty;

        public Thickness ContentInset => (Thickness)GetValue(ContentInsetProperty);

        public static readonly DependencyProperty TintOpacityProperty = DependencyProperty.Register(
            nameof(TintOpacity), typeof(double), typeof(AcrylicBackdrop), new PropertyMetadata(0.6, OnLookChanged));

        public FrameworkElement? Source
        {
            get => (FrameworkElement?)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public double BlurRadius
        {
            get => (double)GetValue(BlurRadiusProperty);
            set => SetValue(BlurRadiusProperty, value);
        }

        public double TintOpacity
        {
            get => (double)GetValue(TintOpacityProperty);
            set => SetValue(TintOpacityProperty, value);
        }

        private readonly VisualBrush _sample;

        private readonly BlurEffect _blur;

        private readonly Grid _frost;

        private readonly Rectangle _blurred;

        private readonly Border _tint;

        private readonly Rectangle _grain;

        private readonly Border _edge;

        private UIElement? _wheelHost;

        private ScrollViewer? _scroller;

        private Rect _viewbox = Rect.Empty;

        private Thickness _bleed = new(-1);

        private double _corner = -1;

        private Size _shapedSize = Size.Empty;

        private double _barTop = -1;

        private bool _syncQueued;

        public AcrylicBackdrop()
        {
            _sample = new VisualBrush
            {
                ViewboxUnits = BrushMappingMode.Absolute,
                ViewportUnits = BrushMappingMode.RelativeToBoundingBox,
                Viewport = new Rect(0, 0, 1, 1),
                Stretch = Stretch.Fill,
                TileMode = TileMode.None
            };

            _blur = new BlurEffect { KernelType = KernelType.Gaussian, RenderingBias = RenderingBias.Performance };

            _blurred = new Rectangle { Fill = _sample };

            _frost = new Grid
            {
                Effect = _blur,
                IsHitTestVisible = false,
                Children = { _blurred },
                CacheMode = new BitmapCache { EnableClearType = false }
            };
            _frost.SetResourceReference(Panel.BackgroundProperty, "ApplicationBackgroundBrush");

            _tint = new Border();
            _tint.SetResourceReference(Border.BackgroundProperty, "ApplicationBackgroundBrush");

            _grain = new Rectangle { Fill = Grain.Value, Opacity = 0.02, IsHitTestVisible = false };

            _edge = new Border { Height = 1, VerticalAlignment = VerticalAlignment.Bottom, Opacity = 0, IsHitTestVisible = false };
            _edge.SetResourceReference(Border.BackgroundProperty, "DividerStrokeColorDefaultBrush");

            Children.Add(_frost);
            Children.Add(_tint);
            Children.Add(_grain);
            Children.Add(_edge);

            ApplyLook();

            LayoutUpdated += (_, _) => QueueSync();
            SizeChanged += (_, e) =>
            {
                if (e.HeightChanged)
                    SetValue(ContentInsetKey, new Thickness(0, e.NewSize.Height, 0, 0));
            };
            Unloaded += (_, _) => _sample.Visual = null;
            Loaded += (_, _) => Attach(Source);
        }

        private static void OnSourceChanged(DependencyObject target, DependencyPropertyChangedEventArgs e) =>
            ((AcrylicBackdrop)target).Attach(e.NewValue as FrameworkElement);

        private static void OnLookChanged(DependencyObject target, DependencyPropertyChangedEventArgs e) => ((AcrylicBackdrop)target).ApplyLook();

        private void ApplyLook()
        {
            _blur.Radius = BlurRadius;
            _blurred.Margin = new Thickness(0, 0, 0, BlurRadius);
            _tint.Opacity = TintOpacity;
            _viewbox = Rect.Empty;
            _bleed = new Thickness(-1);
        }

        private void Shape(Thickness bleed, double corner)
        {
            _bleed = bleed;
            _corner = corner;
            _shapedSize = RenderSize;

            double pad = BlurRadius;
            var face = new Thickness(-bleed.Left, -bleed.Top, -bleed.Right, 0);

            _frost.Margin = new Thickness(-bleed.Left - pad, -bleed.Top - pad, -bleed.Right - pad, -pad);
            _tint.Margin = face;
            _grain.Margin = face;
            _edge.Margin = face;

            var area = new Rect(-bleed.Left, -bleed.Top, ActualWidth + bleed.Left + bleed.Right, ActualHeight + bleed.Top);

            Clip = corner <= 0
                ? new RectangleGeometry(area)
                : new CombinedGeometry(GeometryCombineMode.Intersect,
                    new RectangleGeometry(new Rect(area.X, area.Y, area.Width, area.Height + corner), corner, corner),
                    new RectangleGeometry(area));
        }

        private (Thickness Bleed, double Corner) Reach()
        {
            OverlayPanel? panel = null;

            for (DependencyObject? current = VisualParent; current is not null && panel is null; current = VisualTreeHelper.GetParent(current))
                panel = current as OverlayPanel;

            if (panel is null)
                return (new Thickness(0), 0);

            Point corner;

            try
            {
                corner = panel.Body.TranslatePoint(new Point(0, 0), this);
            }
            catch (InvalidOperationException)
            {
                return (new Thickness(0), 0);
            }

            bool atTop = Math.Abs(corner.Y) <= 1;

            var bleed = new Thickness(
                Math.Round(Math.Max(0, -corner.X)),
                atTop ? Math.Round(Math.Max(0, -corner.Y)) : 0,
                Math.Round(Math.Max(0, corner.X + panel.Body.ActualWidth - ActualWidth)),
                0);

            double radius = atTop ? Math.Max(0, panel.Frame.CornerRadius.TopLeft - panel.Frame.BorderThickness.Top) : 0;

            return (bleed, radius);
        }

        private void Attach(FrameworkElement? source)
        {
            if (_scroller is not null)
                _scroller.ScrollChanged -= OnScrollChanged;

            _scroller = null;
            _viewbox = Rect.Empty;
            _barTop = -1;
            _sample.Visual = source;
            _edge.Opacity = 0;

            FindScroller();
        }

        private void FindScroller()
        {
            if (_scroller is not null || Source is null)
                return;

            _scroller = Descendant<ScrollViewer>(Source);

            if (_scroller is null)
                return;

            _scroller.ScrollChanged += OnScrollChanged;
            _edge.Opacity = _scroller.VerticalOffset > 0.5 ? 1 : 0;
        }

        private static T? Descendant<T>(DependencyObject root) where T : DependencyObject
        {
            if (root is T match)
                return match;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                if (Descendant<T>(VisualTreeHelper.GetChild(root, i)) is T found)
                    return found;
            }

            return null;
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e) =>
            _edge.Opacity = ((ScrollViewer)sender).VerticalOffset > 0.5 ? 1 : 0;

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);

            if (_wheelHost is not null)
                _wheelHost.MouseWheel -= ForwardWheel;

            _wheelHost = VisualParent as UIElement;

            if (_wheelHost is not null)
                _wheelHost.MouseWheel += ForwardWheel;
        }

        private void ForwardWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled || _scroller is not ScrollViewer scroller)
                return;

            scroller.ScrollToVerticalOffset(scroller.VerticalOffset - e.Delta);

            e.Handled = true;
        }

        private void QueueSync()
        {
            if (_syncQueued)
                return;

            _syncQueued = true;

            Dispatcher.BeginInvoke(() =>
            {
                _syncQueued = false;
                Sync();
            }, DispatcherPriority.Render);
        }

        private void Sync()
        {
            if (Source is not FrameworkElement source || !IsVisible || ActualWidth <= 0 || ActualHeight <= 0)
                return;

            if (PresentationSource.FromVisual(this) is null)
                return;

            if (_sample.Visual is null)
                _sample.Visual = source;

            Point origin;

            try
            {
                origin = TranslatePoint(new Point(0, 0), source);
            }
            catch (InvalidOperationException)
            {
                return;
            }

            (Thickness bleed, double corner) = Reach();

            if (bleed != _bleed || corner != _corner || RenderSize != _shapedSize)
                Shape(bleed, corner);

            double pad = BlurRadius;
            var viewbox = new Rect(
                origin.X - bleed.Left - pad,
                origin.Y - bleed.Top - pad,
                ActualWidth + bleed.Left + bleed.Right + pad * 2,
                ActualHeight + bleed.Top + pad);

            if (viewbox != _viewbox)
            {
                _viewbox = viewbox;
                _sample.Viewbox = viewbox;
            }

            FindScroller();

            if (_scroller is not ScrollViewer scroller)
                return;

            double barTop;

            try
            {
                barTop = Math.Max(Math.Round(TranslatePoint(new Point(0, ActualHeight), scroller).Y), 0);
            }
            catch (InvalidOperationException)
            {
                return;
            }

            if (barTop == _barTop || scroller.Template?.FindName("PART_VerticalScrollBar", scroller) is not ScrollBar bar)
                return;

            _barTop = barTop;
            bar.Margin = new Thickness(0, barTop, 0, 0);
        }

        private static Brush CreateGrain()
        {
            var random = new Random(1337);
            byte[] pixels = new byte[GrainSize * GrainSize * 4];

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte shade = (byte)random.Next(256);

                pixels[i] = shade;
                pixels[i + 1] = shade;
                pixels[i + 2] = shade;
                pixels[i + 3] = 255;
            }

            var image = BitmapSource.Create(GrainSize, GrainSize, 96, 96, PixelFormats.Bgra32, null, pixels, GrainSize * 4);
            image.Freeze();

            var brush = new ImageBrush(image)
            {
                TileMode = TileMode.Tile,
                ViewportUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0, 0, GrainSize, GrainSize),
                Stretch = Stretch.None
            };

            brush.Freeze();

            return brush;
        }
    }
}
