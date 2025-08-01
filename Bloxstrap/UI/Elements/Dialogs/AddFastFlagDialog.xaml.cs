using Bloxstrap.Resources;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class AddFastFlagDialog
    {
        public string? FormattedName { get; private set; }
        public string? FormattedValue { get; private set; }
        public string? ImportGameId { get; private set; }
        public string? ImportGameIdJson { get; private set; }

        public FastFlagFilterType AddIdFilterType =>
            (AddIdFilterTypeComboBox.SelectedIndex == 1) ? FastFlagFilterType.DataCenterFilter : FastFlagFilterType.PlaceFilter;

        public FastFlagFilterType ImportIdFilterType =>
            (ImportIdFilterTypeComboBox.SelectedIndex == 1) ? FastFlagFilterType.DataCenterFilter : FastFlagFilterType.PlaceFilter;

        public MessageBoxResult Result = MessageBoxResult.Cancel;

        public ObservableCollection<CommonValueItem> BooleanValues { get; } = new ObservableCollection<CommonValueItem>()
        {
            new CommonValueItem { Value = "True", Group = "Boolean" },
            new CommonValueItem { Value = "False", Group = "Boolean" },
        };

        public ObservableCollection<CommonValueItem> NumericValues { get; } = new ObservableCollection<CommonValueItem>()
        {
            new CommonValueItem { Value = "64", Group = "Intergers" },
            new CommonValueItem { Value = "100", Group = "Intergers" },
            new CommonValueItem { Value = "128", Group = "Intergers" },
            new CommonValueItem { Value = "256", Group = "Intergers" },
            new CommonValueItem { Value = "512", Group = "Intergers" },
            new CommonValueItem { Value = "1024", Group = "Intergers" },
            new CommonValueItem { Value = "2048", Group = "Intergers" },
            new CommonValueItem { Value = "4096", Group = "Intergers" },
            new CommonValueItem { Value = "8192", Group = "Intergers" },
            new CommonValueItem { Value = "10000", Group = "Intergers" },
            new CommonValueItem { Value = "16384", Group = "Intergers" },
            new CommonValueItem { Value = "2147483647", Group = "Intergers" },
            new CommonValueItem { Value = "-2147483648", Group = "Intergers" },
        };

        public ObservableCollection<CommonValueItem> SpecialValues { get; } = new ObservableCollection<CommonValueItem>()
        {
            new CommonValueItem { Value = "null", Group = "Special" },
        };

        public CollectionViewSource CommonValuesView { get; }

        public AddFastFlagDialog()
        {
            InitializeComponent();

            var vm = AdvancedSettingsDialog.SharedViewModel;

            var allValues = new ObservableCollection<CommonValueItem>();
            foreach (var item in BooleanValues) allValues.Add(item);
            foreach (var item in NumericValues) allValues.Add(item);
            foreach (var item in SpecialValues) allValues.Add(item);

            CommonValuesView = new CollectionViewSource { Source = allValues };
            CommonValuesView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(CommonValueItem.Group)));

            DataContext = this;

            UpdateAddWithIDTabs(vm.ShowAddWithID);

            vm.ShowAddWithIDChanged += (_, _) =>
            {
                Dispatcher.Invoke(() =>
                {
                    UpdateAddWithIDTabs(vm.ShowAddWithID);
                });
            };
        }

        private void UpdateAddWithIDTabs(bool show)
        {
            AddWithIdTab.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            ImportIdJsonTab.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = $"{Strings.FileTypes_JSONFiles}|*.json"
            };

            if (dialog.ShowDialog() != true)
                return;

            JsonTextBox.Text = File.ReadAllText(dialog.FileName);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = Tabs.SelectedIndex;

            if (selectedIndex == 2)
            {
                string name = GameFlagNameTextBox.Text.Trim();
                string value = GameFlagValueComboBox.Text.Trim();
                string gameId = GameFlagIdTextBox.Text.Trim();
                var filterType = AddIdFilterType;
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(gameId))
                {
                    string suffix = filterType == FastFlagFilterType.DataCenterFilter ? "_DataCenterFilter" : "_PlaceFilter";
                    FormattedName = $"{name}{suffix}";
                    FormattedValue = $"{value};{gameId}";
                    ImportGameId = null;
                    ImportGameIdJson = null;
                    Result = MessageBoxResult.OK;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Please fill in all fields.");
                }
                return;
            }
            else if (selectedIndex == 3)
            {
                var filterType = ImportIdFilterType;
                ImportGameId = ImportGameIdTextBox.Text.Trim();
                ImportGameIdJson = GameIdJsonTextBox.Text.Trim();
                FormattedName = null;
                FormattedValue = null;
                Result = MessageBoxResult.OK;
                DialogResult = true;
                Close();
                return;
            }

            if (selectedIndex == 0)
            {
                string name = FlagNameTextBox.Text.Trim();
                string value = FlagValueComboBox.Text.Trim();
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                {
                    FormattedName = name;
                    FormattedValue = value;
                    ImportGameId = null;
                    ImportGameIdJson = null;
                    Result = MessageBoxResult.OK;
                    DialogResult = true;
                    Close();
                    return;
                }
                else
                {
                    MessageBox.Show("Please fill in both Name and Value.");
                    return;
                }
            }

            FormattedName = null;
            FormattedValue = null;
            ImportGameId = null;
            ImportGameIdJson = null;
            Result = MessageBoxResult.OK;
            DialogResult = true;
            Close();
        }

        private void FlagValueComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (FlagValueComboBox.Template.FindName("PART_EditableTextBox", FlagValueComboBox) is TextBox tb)
            {
                tb.GotFocus += (_, _) =>
                {
                    if (tb.Text == "Enter or select a value")
                        tb.Text = "";
                };

                tb.LostFocus += (_, _) =>
                {
                    if (string.IsNullOrWhiteSpace(tb.Text))
                    {
                        tb.Text = "Enter or select a value";
                    }
                };

                tb.Text = "Enter or select a value";
            }
        }

        private void GameFlagValueComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (GameFlagValueComboBox.Template.FindName("PART_EditableTextBox", GameFlagValueComboBox) is TextBox tb)
            {
                tb.GotFocus += (_, _) =>
                {
                    if (tb.Text == "Enter or select a value")
                        tb.Text = "";
                };

                tb.LostFocus += (_, _) =>
                {
                    if (string.IsNullOrWhiteSpace(tb.Text))
                    {
                        tb.Text = "Enter or select a value";
                    }
                };

                tb.Text = "Enter or select a value";
            }
        }
    }

    public class CommonValueItem
    {
        public string Value { get; set; } = "";
        public string Group { get; set; } = "";

        public override string ToString() => Value;
    }
}