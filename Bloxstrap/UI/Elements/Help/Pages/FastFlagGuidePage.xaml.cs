namespace Bloxstrap.UI.Elements.Help.Pages
{
    /// <summary>
    /// Interaction logic for FastFlagGuidePage.xaml
    /// </summary>
    public partial class FastFlagGuidePage
    {
        public FastFlagGuidePage()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
