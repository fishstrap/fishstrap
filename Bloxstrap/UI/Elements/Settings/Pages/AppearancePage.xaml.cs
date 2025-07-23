using Bloxstrap.UI.ViewModels.Settings;
using Microsoft.Win32;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    public partial class AppearancePage
    {
        public AppearancePage()
        {
            DataContext = new AppearanceViewModel(this);
            InitializeComponent();
        }

        public void CustomThemeSelection(object sender, SelectionChangedEventArgs e)
        {
            AppearanceViewModel viewModel = (AppearanceViewModel)DataContext;

            viewModel.SelectedCustomTheme = (string)((ListBox)sender).SelectedItem;
            viewModel.SelectedCustomThemeName = viewModel.SelectedCustomTheme;

            viewModel.OnPropertyChanged(nameof(viewModel.SelectedCustomTheme));
            viewModel.OnPropertyChanged(nameof(viewModel.SelectedCustomThemeName));
        }

        private void OnAddGradientStop_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AppearanceViewModel vm)
            {
                var newStop = new GradientStopData { Offset = 0.5, Color = "#" };
                vm.GradientStops.Add(newStop);
                App.Settings.Prop.CustomGradientStops = vm.GradientStops.ToList();
                ((MainWindow)Window.GetWindow(this)!).ApplyTheme();
            }
        }

        private void OnRemoveGradientStop_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is GradientStopData stop)
            {
                if (DataContext is AppearanceViewModel vm)
                {
                    vm.GradientStops.Remove(stop);
                    App.Settings.Prop.CustomGradientStops = vm.GradientStops.ToList();
                    ((MainWindow)Window.GetWindow(this)!).ApplyTheme();
                }
            }
        }

        private void OnChangeGradientColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is GradientStopData stop &&
                DataContext is AppearanceViewModel vm)
            {
                var dialog = new System.Windows.Forms.ColorDialog();
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var color = dialog.Color;
                    stop.Color = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
                    App.Settings.Prop.CustomGradientStops = vm.GradientStops.ToList();
                    ((MainWindow)Window.GetWindow(this)!).ApplyTheme();
                }
            }
        }

        private void OnGradientOffsetChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DataContext is AppearanceViewModel vm)
            {
                App.Settings.Prop.CustomGradientStops = vm.GradientStops.ToList();
                ((MainWindow)Window.GetWindow(this)!).ApplyTheme();
            }
        }

        private void OnGradientColorHexChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.DataContext is GradientStopData stop &&
                DataContext is AppearanceViewModel vm)
            {
                if (!string.IsNullOrWhiteSpace(stop.Color) && stop.Color.StartsWith("#") && stop.Color.Length >= 7)
                {
                    App.Settings.Prop.CustomGradientStops = vm.GradientStops.ToList();
                    ((MainWindow)Window.GetWindow(this)!).ApplyTheme();
                }
            }
        }

        private void OnResetGradient_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AppearanceViewModel vm)
            {
                vm.ResetGradientStops();
                App.Settings.Prop.CustomGradientStops = vm.GradientStops.ToList();
                ((MainWindow)Window.GetWindow(this)!).ApplyTheme();
            }
        }

        private void OnCopyGradientJson_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AppearanceViewModel vm)
            {
                var json = JsonSerializer.Serialize(vm.GradientStops, new JsonSerializerOptions { WriteIndented = true });
                Clipboard.SetText(json);
            }
        }

        private void OnExportGradient_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AppearanceViewModel vm)
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    FileName = "GradientStops.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(vm.GradientStops, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
        }

        private void OnImportGradient_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AppearanceViewModel vm)
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    var json = File.ReadAllText(dialog.FileName);
                    try
                    {
                        var stops = JsonSerializer.Deserialize<List<GradientStopData>>(json);
                        if (stops != null)
                        {
                            vm.GradientStops.Clear();
                            foreach (var stop in stops)
                                vm.GradientStops.Add(stop);

                            App.Settings.Prop.CustomGradientStops = vm.GradientStops.ToList();
                            ((MainWindow)Window.GetWindow(this)!).ApplyTheme();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Invalid JSON: " + ex.Message);
                    }
                }
            }
        }

        private void OnImportFromJsonText_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AppearanceViewModel vm)
            {
                try
                {
                    var json = ImportJsonTextBox.Text;
                    var stops = JsonSerializer.Deserialize<List<GradientStopData>>(json);
                    if (stops != null)
                    {
                        vm.GradientStops.Clear();
                        foreach (var stop in stops)
                            vm.GradientStops.Add(stop);

                        App.Settings.Prop.CustomGradientStops = vm.GradientStops.ToList();

                        ((MainWindow)Window.GetWindow(this)!).ApplyTheme();
                    }
                    ImportJsonTextBox.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Invalid JSON: " + ex.Message);
                }
            }
        }
    }
}