using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public static class OverlayMotion
    {
        private const double ShadowRoom = 36;

        private static readonly Duration RiseTime = TimeSpan.FromMilliseconds(380);

        private static readonly Duration DropTime = TimeSpan.FromMilliseconds(200);

        private static readonly Duration FadeInTime = TimeSpan.FromMilliseconds(200);

        private static readonly Duration FadeOutTime = TimeSpan.FromMilliseconds(180);

        public static bool Enabled => SystemParameters.ClientAreaAnimation;

        public static void Lower(FrameworkElement dock)
        {
            TranslateTransform slide = Slide(dock);

            slide.BeginAnimation(TranslateTransform.YProperty, null);
            slide.Y = HiddenY(dock);
        }

        public static void Rise(FrameworkElement dock) =>
            Slide(dock).BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, RiseTime)
            {
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            });

        public static void Drop(FrameworkElement dock, Action done)
        {
            var slide = new DoubleAnimation(HiddenY(dock), DropTime) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
            slide.Completed += (_, _) => done();

            Slide(dock).BeginAnimation(TranslateTransform.YProperty, slide);
        }

        public static void Conceal(UIElement element)
        {
            element.BeginAnimation(UIElement.OpacityProperty, null);
            element.Opacity = 0;
        }

        public static void FadeIn(UIElement element) =>
            element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, FadeInTime));

        public static void FadeOut(UIElement element) =>
            element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, FadeOutTime));

        public static void Rest(UIElement element)
        {
            element.BeginAnimation(UIElement.OpacityProperty, null);
            element.Opacity = 1;

            if (element.RenderTransform is not TranslateTransform slide)
                return;

            slide.BeginAnimation(TranslateTransform.YProperty, null);
            slide.Y = 0;
        }

        private static TranslateTransform Slide(FrameworkElement dock)
        {
            if (dock.RenderTransform is TranslateTransform existing)
                return existing;

            var slide = new TranslateTransform();

            dock.RenderTransform = slide;

            return slide;
        }

        private static double HiddenY(FrameworkElement dock) =>
            (Double.IsNaN(dock.Height) ? dock.ActualHeight : dock.Height) + dock.Margin.Bottom + ShadowRoom;
    }
}
