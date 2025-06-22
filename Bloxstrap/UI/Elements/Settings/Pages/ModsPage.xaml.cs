using Bloxstrap.UI.ViewModels.Settings;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for ModsPage.xaml
    /// </summary>
    public partial class ModsPage
    {
        public ModsPage()
        {
            DataContext = new ModsViewModel();
            InitializeComponent();
        }


        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchBox.Text.Trim().ToLower();

            foreach (var child in ResultsPanel.Children)
            {
                if (child is GroupBox groupBox)
                {
                    string modName = "";

                    // Find the mod name in the TextBlock inside the GroupBox
                    if (groupBox.Content is Panel panel)
                    {
                        var grid = panel.Children.OfType<Grid>().FirstOrDefault();
                        if (grid != null)
                        {
                            var textBlock = grid.Children.OfType<TextBlock>().FirstOrDefault();
                            if (textBlock != null)
                            {
                                var run = textBlock.Inlines.OfType<Run>().FirstOrDefault(r => r.FontSize == 20);
                                if (run != null)
                                    modName = run.Text.ToLower();
                            }
                        }
                    }

                    bool match = modName.Contains(query);

                    groupBox.Visibility = match || string.IsNullOrWhiteSpace(query)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
            }
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            var url = "https://drive.google.com/file/d/1cEzvRjuKjAtmSKluEt9FivSZEgHQQ08y/view";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
            }
        }

        private void InstallButton_Click2(object sender, RoutedEventArgs e)
        {
            var url = "https://drive.usercontent.google.com/u/0/uc?id=1kQTfIKBPyqF6o1G85v9fdXE2PHXUA5oM&export=download";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
            }
        }



        private void InstallButton_Click3(object sender, RoutedEventArgs e)
        {
            var url = "https://drive.google.com/file/d/1AdYUMYGjKOODfi1AIHNQlIZIvF9Caj7I/view";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
            }
        }

        private void InstallButton_Click4(object sender, RoutedEventArgs e)
        {
            var url = "https://drive.google.com/file/d/1_k3yi2BnpoPI8CWuM69xD9myVFvPmSiA/view";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
            }
        }

        private void InstallButton_Click5(object sender, RoutedEventArgs e)
        {
            var url = "https://drive.google.com/file/d/1JDHzzISMnWbOkq7qvsjL-7PnxO6I7T64/view";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
            }
        }

        private void InstallButton_Click6(object sender, RoutedEventArgs e)
        {
            var url = "https://gamebanana.com/mods/updates/543392";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
            }
        }

        private void InstallButton_Click7(object sender, RoutedEventArgs e)
        {
            var url = "https://drive.google.com/file/d/1v7JNfmNh0tVy0eJE74lMMf_51-rXXf3v/view";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
            }
        }

        private void InstallButton_Click8(object sender, RoutedEventArgs e)
        {
            var url = "https://drive.google.com/file/d/1WPHwjqLMA9wWImXlNDsGQZr5WhjLmdXX/view";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
            }
        }
    }
}
