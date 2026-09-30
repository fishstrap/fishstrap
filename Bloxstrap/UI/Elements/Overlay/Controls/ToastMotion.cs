using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

using Bloxstrap.Enums.Overlay;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public static class ToastMotion
    {
        private const double PopFrom = 0.85;

        private static readonly Duration EnterTime = TimeSpan.FromMilliseconds(220);

        private static readonly Duration PopTime = TimeSpan.FromMilliseconds(300);

        private static readonly Duration FadeInTime = TimeSpan.FromMilliseconds(150);

        private static readonly Duration FadeTime = TimeSpan.FromMilliseconds(220);

        private static readonly Duration LeaveTime = TimeSpan.FromMilliseconds(250);

        public static void Hide(FrameworkElement card)
        {
            (ScaleTransform scale, TranslateTransform slide) = Transforms(card);

            Stop(card, scale, slide);

            card.Opacity = 0;
        }

        public static void Enter(FrameworkElement card, ToastAppearance appearance)
        {
            (ScaleTransform scale, TranslateTransform slide) = Transforms(card);

            Stop(card, scale, slide);

            card.RenderTransformOrigin = new Point(appearance.AtRight ? 1 : 0, appearance.AtBottom ? 1 : 0);

            switch (appearance.Animation)
            {
                case ToastAnimation.None:
                    card.Opacity = 1;
                    return;

                case ToastAnimation.Fade:
                    card.Opacity = 0;
                    card.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, FadeTime));
                    return;

                case ToastAnimation.Pop:
                    scale.ScaleX = PopFrom;
                    scale.ScaleY = PopFrom;
                    Grow(scale, 1, PopTime, new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 });
                    break;

                case ToastAnimation.SlideSide:
                    slide.X = HiddenX(card, appearance);
                    slide.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, EnterTime) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                    break;

                default:
                    slide.Y = HiddenY(card, appearance);
                    slide.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, EnterTime) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                    break;
            }

            card.Opacity = 0;
            card.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, FadeInTime));
        }

        public static void Leave(FrameworkElement card, ToastAppearance appearance, Action done)
        {
            (ScaleTransform scale, TranslateTransform slide) = Transforms(card);

            if (appearance.Animation == ToastAnimation.None)
            {
                Stop(card, scale, slide);
                card.Opacity = 0;
                done();
                return;
            }

            var ease = new CubicEase { EasingMode = EasingMode.EaseIn };

            switch (appearance.Animation)
            {
                case ToastAnimation.Pop:
                    Grow(scale, PopFrom, LeaveTime, ease);
                    break;

                case ToastAnimation.SlideSide:
                    slide.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(HiddenX(card, appearance), LeaveTime) { EasingFunction = ease });
                    break;

                case ToastAnimation.Slide:
                    slide.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(HiddenY(card, appearance), LeaveTime) { EasingFunction = ease });
                    break;
            }

            var fade = new DoubleAnimation(0, LeaveTime);
            fade.Completed += (_, _) => done();

            card.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        private static void Grow(ScaleTransform scale, double to, Duration time, IEasingFunction ease)
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(to, time) { EasingFunction = ease });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(to, time) { EasingFunction = ease });
        }

        private static void Stop(FrameworkElement card, ScaleTransform scale, TranslateTransform slide)
        {
            card.BeginAnimation(UIElement.OpacityProperty, null);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            slide.BeginAnimation(TranslateTransform.XProperty, null);
            slide.BeginAnimation(TranslateTransform.YProperty, null);

            scale.ScaleX = 1;
            scale.ScaleY = 1;
            slide.X = 0;
            slide.Y = 0;
        }

        private static (ScaleTransform Scale, TranslateTransform Slide) Transforms(FrameworkElement card)
        {
            if (card.RenderTransform is TransformGroup group
                && group.Children.Count == 2
                && group.Children[0] is ScaleTransform existingScale
                && group.Children[1] is TranslateTransform existingSlide)
            {
                return (existingScale, existingSlide);
            }

            var scale = new ScaleTransform();
            var slide = new TranslateTransform();

            card.RenderTransform = new TransformGroup { Children = { scale, slide } };

            return (scale, slide);
        }

        private static double HiddenY(FrameworkElement card, ToastAppearance appearance) => appearance.AtBottom
            ? card.ActualHeight + card.Margin.Bottom
            : -(card.ActualHeight + card.Margin.Top);

        private static double HiddenX(FrameworkElement card, ToastAppearance appearance) => appearance.AtRight
            ? card.ActualWidth + card.Margin.Right
            : -(card.ActualWidth + card.Margin.Left);
    }
}
