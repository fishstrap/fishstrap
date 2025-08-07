using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Bloxstrap.UI.ViewModels.Dialogs;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class ChannelListsDialog
    {
        public ChannelListsDialog()
        {
            InitializeComponent();
            DataContext = new ChannelListsViewModel();
        }

        private void ChannelDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                var dataGrid = (DataGrid)sender;

                var selectedItems = dataGrid.SelectedItems.Cast<DeployInfoDisplay>().ToList();

                if (selectedItems.Count > 0)
                {
                    var textToCopy = string.Join(Environment.NewLine, selectedItems.Select(i => i.ChannelName));

                    Clipboard.SetText(textToCopy);

                    e.Handled = true;
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}