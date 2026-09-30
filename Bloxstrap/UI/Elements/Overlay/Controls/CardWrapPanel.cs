using System.Windows;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public class CardWrapPanel : Wpf.Ui.Controls.VirtualizingWrapPanel
    {
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
