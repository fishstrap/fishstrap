using Bloxstrap.UI.Elements.Base;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class FlagDialog : WpfUiWindow
    {
        public FlagDialog(string updatedText, string invalidText, string defaultText, string headerText = "FastFlag Changes")
        {
            InitializeComponent();

            Title = headerText;
            UpdatedFastFlagsTextBox.Text = updatedText;
            InvalidFlagsTextBox.Text = invalidText;
            DefaultValuesTextBox.Text = defaultText;

            // Focus the first non-empty tab
            if (!string.IsNullOrWhiteSpace(updatedText))
            {
                UpdatedFastFlagsTextBox.Focus();
                UpdatedFastFlagsTextBox.Select(0, 0);
            }
            else if (!string.IsNullOrWhiteSpace(invalidText))
            {
                InvalidFlagsTextBox.Focus();
                InvalidFlagsTextBox.Select(0, 0);
            }
            else
            {
                DefaultValuesTextBox.Focus();
                DefaultValuesTextBox.Select(0, 0);
            }
        }
    }
}
