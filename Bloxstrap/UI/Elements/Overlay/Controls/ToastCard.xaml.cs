using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public partial class ToastCard : UserControl
    {
        private const string BackgroundKey = "SolidBackgroundFillColorTertiaryBrush";

        public ToastCard()
        {
            InitializeComponent();

            Apply(ToastAppearance.Default);
        }

        public double CardHeight => Card.ActualHeight * Scale.ScaleY;

        public void Apply(ToastAppearance appearance)
        {
            Card.CornerRadius = appearance.Corners;
            Card.BorderThickness = appearance.Edges;

            Scale.ScaleX = appearance.Scale;
            Scale.ScaleY = appearance.Scale;

            if (TryFindResource(BackgroundKey) is SolidColorBrush background)
                Card.Background = new SolidColorBrush(background.Color) { Opacity = appearance.BackgroundOpacity };
        }

        public void SetHeader(bool visible) => ToastHeader.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

        public void Show(OverlayNotice notice)
        {
            ToastTitle.Text = notice.Title;
            ToastMessage.Text = notice.Message;
            ToastMessage.Visibility = String.IsNullOrEmpty(notice.Message) ? Visibility.Collapsed : Visibility.Visible;

            ToastAction.Text = notice.ActionText ?? String.Empty;
            ToastAction.Visibility = String.IsNullOrEmpty(notice.ActionText) ? Visibility.Collapsed : Visibility.Visible;

            if (String.IsNullOrEmpty(notice.ImageUrl) || !Uri.TryCreate(notice.ImageUrl, UriKind.Absolute, out Uri? source))
            {
                ToastPicture.Visibility = Visibility.Collapsed;
                ToastPictureImage.Background = null;
                return;
            }

            ToastPicture.CornerRadius = new CornerRadius(notice.RoundImage ? 22 : 6);
            ToastPictureImage.Background = new ImageBrush(new BitmapImage(source)) { Stretch = Stretch.UniformToFill };
            ToastPicture.Visibility = Visibility.Visible;
        }
    }
}
