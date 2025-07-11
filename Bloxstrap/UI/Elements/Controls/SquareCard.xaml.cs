using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Bloxstrap.UI.Elements.Controls
{
    public partial class SquareCard : UserControl
    {
        public enum CategoryType
        {
            Network,
            Privacy,
            Cpu,
            Gpu,
            Performance
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(SquareCard));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(SquareCard));

        public static readonly DependencyProperty InnerContentProperty =
            DependencyProperty.Register(nameof(InnerContent), typeof(object), typeof(SquareCard));

        public static readonly DependencyProperty ButtonContentProperty =
            DependencyProperty.Register(nameof(ButtonContent), typeof(object), typeof(SquareCard));

        public static readonly DependencyProperty CategoryIconProperty =
            DependencyProperty.Register(nameof(CategoryIcon), typeof(string), typeof(SquareCard));

        public static readonly DependencyProperty CategoryProperty =
            DependencyProperty.Register(nameof(Category), typeof(CategoryType), typeof(SquareCard), new PropertyMetadata(CategoryType.Performance, OnCategoryChanged));

        public static readonly DependencyProperty SecondaryCategoryIconProperty =
            DependencyProperty.Register(nameof(SecondaryCategoryIcon), typeof(string), typeof(SquareCard));

        public static readonly DependencyProperty PrimaryIconToolTipProperty =
            DependencyProperty.Register(nameof(PrimaryIconToolTip), typeof(string), typeof(SquareCard));

        public static readonly DependencyProperty SecondaryIconToolTipProperty =
            DependencyProperty.Register(nameof(SecondaryIconToolTip), typeof(string), typeof(SquareCard));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public object InnerContent
        {
            get => GetValue(InnerContentProperty);
            set => SetValue(InnerContentProperty, value);
        }

        public object ButtonContent
        {
            get => GetValue(ButtonContentProperty);
            set => SetValue(ButtonContentProperty, value);
        }

        public string CategoryIcon
        {
            get => (string)GetValue(CategoryIconProperty);
            set => SetValue(CategoryIconProperty, value);
        }

        public string SecondaryCategoryIcon
        {
            get => (string)GetValue(SecondaryCategoryIconProperty);
            set => SetValue(SecondaryCategoryIconProperty, value);
        }

        public string PrimaryIconToolTip
        {
            get => (string)GetValue(PrimaryIconToolTipProperty);
            set => SetValue(PrimaryIconToolTipProperty, value);
        }

        public string SecondaryIconToolTip
        {
            get => (string)GetValue(SecondaryIconToolTipProperty);
            set => SetValue(SecondaryIconToolTipProperty, value);
        }

        public CategoryType Category
        {
            get => (CategoryType)GetValue(CategoryProperty);
            set => SetValue(CategoryProperty, value);
        }

        private static void OnCategoryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SquareCard)d;
            var category = (CategoryType)e.NewValue;

            control.CategoryIcon = category switch
            {
                CategoryType.Network => "🌐",
                CategoryType.Privacy => "🔒",
                CategoryType.Cpu => "🧠",
                CategoryType.Gpu => "🖥️",
                CategoryType.Performance => "⚡",
                _ => "⚙️"
            };
        }

        private void SquareCard_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (App.Settings.Prop.DisableAnimations)
            {
                var hoverExit = (Storyboard)FindResource("HoverExitStoryboard");
                hoverExit.Stop();

                HoverScaleTransform.ScaleX = 1.08;
                HoverScaleTransform.ScaleY = 1.08;
            }
            else
            {
                var hoverEnter = (Storyboard)FindResource("HoverEnterStoryboard");
                hoverEnter.Begin();
            }
        }

        private void SquareCard_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (App.Settings.Prop.DisableAnimations)
            {
                var hoverEnter = (Storyboard)FindResource("HoverEnterStoryboard");
                hoverEnter.Stop();

                HoverScaleTransform.ScaleX = 1.0;
                HoverScaleTransform.ScaleY = 1.0;
            }
            else
            {
                // Play the hover-exit animation smoothly
                var hoverExit = (Storyboard)FindResource("HoverExitStoryboard");
                hoverExit.Begin();
            }
        }

        public SquareCard()
        {
            InitializeComponent();
        }
    }
}