using System.Windows;
using System.Windows.Controls.Primitives;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public class CardWrapPanel : Wpf.Ui.Controls.VirtualizingWrapPanel
    {
        public static readonly DependencyProperty TopInsetProperty = DependencyProperty.Register(
            nameof(TopInset), typeof(double), typeof(CardWrapPanel),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public double TopInset
        {
            get => (double)GetValue(TopInsetProperty);
            set => SetValue(TopInsetProperty, value);
        }

        protected override Size CalculateExtent(Size availableSize)
        {
            Size extent = base.CalculateExtent(availableSize);

            return new Size(extent.Width, extent.Height + TopInset);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Size arranged = base.ArrangeOverride(finalSize);

            if (TopInset <= 0)
                return arranged;

            foreach (FrameworkElement child in InternalChildren.OfType<FrameworkElement>())
            {
                Rect slot = LayoutInformation.GetLayoutSlot(child);

                child.Arrange(new Rect(slot.X, slot.Y + TopInset, slot.Width, slot.Height));
            }

            return arranged;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (Double.IsInfinity(availableSize.Width))
                availableSize.Width = ItemSize.IsEmpty ? 0 : ItemSize.Width;

            Size desired = base.MeasureOverride(availableSize);

            Size cell = CalculateChildArrangeSize(availableSize);

            foreach (UIElement child in InternalChildren)
                child.Measure(cell);

            return desired;
        }
    }
}
