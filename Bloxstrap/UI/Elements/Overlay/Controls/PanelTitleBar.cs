using System.Windows;

using Wpf.Ui.Common;

namespace Bloxstrap.UI.Elements.Overlay.Controls
{
    public class PanelTitleBar : Wpf.Ui.Controls.TitleBar
    {
        public PanelTitleBar()
        {
            SetValue(TemplateButtonCommandProperty, new RelayCommand(parameter =>
            {
                if (parameter is "close")
                    RaiseEvent(new RoutedEventArgs(CloseClickedEvent, this));
            }));
        }

        public override void OnApplyTemplate()
        {
        }
    }
}
