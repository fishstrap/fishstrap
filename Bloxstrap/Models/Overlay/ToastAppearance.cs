using System.Windows;

using Bloxstrap.Enums.Overlay;

namespace Bloxstrap.Models.Overlay
{
    public record ToastAppearance(ToastStyle Style, double X, double Y, double CornerRadius, double Scale, double TextScale, double BackgroundOpacity, int Duration, bool HeaderServer, bool HeaderFriends, ToastAnimation Animation)
    {
        public const double MinCornerRadius = 0;
        public const double MaxCornerRadius = 20;
        public const double MinScale = 0.8;
        public const double MaxScale = 1.3;
        public const double MinTextScale = 0.8;
        public const double MaxTextScale = 1.5;
        public const double MinBackgroundOpacity = 0.5;
        public const double MaxBackgroundOpacity = 1;
        public const int MinDuration = 3;
        public const int MaxDuration = 15;

        private const double Inset = 16;
        private const double ShadowRoom = 24;

        public static readonly ToastAppearance Default = new(ToastStyle.Corner, 1, 1, 8, 1, 1, 1, 6, false, false, ToastAnimation.Slide);

        public static ToastAppearance Current
        {
            get
            {
                var settings = App.Settings.Prop;

                return new ToastAppearance(
                    Enum.IsDefined(settings.OverlayToastStyle) ? settings.OverlayToastStyle : Default.Style,
                    Math.Clamp(settings.OverlayToastX, 0, 1),
                    Math.Clamp(settings.OverlayToastY, 0, 1),
                    Math.Clamp(settings.OverlayToastCornerRadius, MinCornerRadius, MaxCornerRadius),
                    Math.Clamp(settings.OverlayToastScale, MinScale, MaxScale),
                    Math.Clamp(settings.OverlayToastTextScale, MinTextScale, MaxTextScale),
                    Math.Clamp(settings.OverlayToastOpacity, MinBackgroundOpacity, MaxBackgroundOpacity),
                    Math.Clamp(settings.OverlayToastDuration, MinDuration, MaxDuration),
                    settings.OverlayToastHeaderServer,
                    settings.OverlayToastHeaderFriends,
                    Enum.IsDefined(settings.OverlayToastAnimation) ? settings.OverlayToastAnimation : Default.Animation);
            }
        }

        public bool ShowsHeader(NoticeKind kind) => kind == NoticeKind.Friend ? HeaderFriends : HeaderServer;

        public bool AtBottom => Y >= 0.5;

        public bool AtRight => X >= 0.5;

        public bool Docked => Style == ToastStyle.Corner;

        private bool FlushLeft => Docked && X <= 0;

        private bool FlushRight => Docked && X >= 1;

        private bool FlushTop => Docked && Y <= 0;

        private bool FlushBottom => Docked && Y >= 1;

        public CornerRadius Corners => new(
            FlushLeft || FlushTop ? 0 : CornerRadius,
            FlushTop || FlushRight ? 0 : CornerRadius,
            FlushRight || FlushBottom ? 0 : CornerRadius,
            FlushBottom || FlushLeft ? 0 : CornerRadius);

        public Thickness Edges => new(FlushLeft ? 0 : 1, FlushTop ? 0 : 1, FlushRight ? 0 : 1, FlushBottom ? 0 : 1);

        public Thickness Margin => new(
            FlushLeft ? 0 : AtRight ? ShadowRoom : Inset,
            FlushTop ? 0 : AtBottom ? ShadowRoom : Inset,
            FlushRight ? 0 : AtRight ? Inset : ShadowRoom,
            FlushBottom ? 0 : AtBottom ? Inset : ShadowRoom);
    }
}
