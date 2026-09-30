using System.Windows;

using Bloxstrap.Enums.Overlay;

namespace Bloxstrap.Models.Overlay
{
    public record ToastAppearance(ToastStyle Style, ToastPosition Position, double CornerRadius, double Scale, double BackgroundOpacity, int Duration, bool HeaderServer, bool HeaderFriends, ToastAnimation Animation)
    {
        public const double MinCornerRadius = 0;
        public const double MaxCornerRadius = 20;
        public const double MinScale = 0.8;
        public const double MaxScale = 1.3;
        public const double MinBackgroundOpacity = 0.5;
        public const double MaxBackgroundOpacity = 1;
        public const int MinDuration = 3;
        public const int MaxDuration = 15;

        private const double Inset = 16;
        private const double ShadowRoom = 24;

        public static readonly ToastAppearance Default = new(ToastStyle.Corner, ToastPosition.BottomRight, 8, 1, 1, 6, false, false, ToastAnimation.Slide);

        public static ToastAppearance Current
        {
            get
            {
                var settings = App.Settings.Prop;

                return new ToastAppearance(
                    Enum.IsDefined(settings.OverlayToastStyle) ? settings.OverlayToastStyle : Default.Style,
                    Enum.IsDefined(settings.OverlayToastPosition) ? settings.OverlayToastPosition : Default.Position,
                    Math.Clamp(settings.OverlayToastCornerRadius, MinCornerRadius, MaxCornerRadius),
                    Math.Clamp(settings.OverlayToastScale, MinScale, MaxScale),
                    Math.Clamp(settings.OverlayToastOpacity, MinBackgroundOpacity, MaxBackgroundOpacity),
                    Math.Clamp(settings.OverlayToastDuration, MinDuration, MaxDuration),
                    settings.OverlayToastHeaderServer,
                    settings.OverlayToastHeaderFriends,
                    Enum.IsDefined(settings.OverlayToastAnimation) ? settings.OverlayToastAnimation : Default.Animation);
            }
        }

        public bool ShowsHeader(NoticeKind kind) => kind == NoticeKind.Friend ? HeaderFriends : HeaderServer;

        public bool AtBottom => Position is ToastPosition.BottomRight or ToastPosition.BottomLeft;

        public bool AtRight => Position is ToastPosition.BottomRight or ToastPosition.TopRight;

        public bool Docked => Style == ToastStyle.Corner;

        public CornerRadius Corners
        {
            get
            {
                if (!Docked)
                    return new CornerRadius(CornerRadius);

                return Position switch
                {
                    ToastPosition.BottomLeft => new CornerRadius(0, CornerRadius, 0, 0),
                    ToastPosition.TopRight => new CornerRadius(0, 0, 0, CornerRadius),
                    ToastPosition.TopLeft => new CornerRadius(0, 0, CornerRadius, 0),
                    _ => new CornerRadius(CornerRadius, 0, 0, 0)
                };
            }
        }

        public Thickness Edges
        {
            get
            {
                if (!Docked)
                    return new Thickness(1);

                return new Thickness(AtRight ? 1 : 0, AtBottom ? 1 : 0, AtRight ? 0 : 1, AtBottom ? 0 : 1);
            }
        }

        public Thickness Margin
        {
            get
            {
                double edge = Docked ? 0 : Inset;

                return new Thickness(AtRight ? ShadowRoom : edge, AtBottom ? ShadowRoom : edge, AtRight ? edge : ShadowRoom, AtBottom ? edge : ShadowRoom);
            }
        }
    }
}
