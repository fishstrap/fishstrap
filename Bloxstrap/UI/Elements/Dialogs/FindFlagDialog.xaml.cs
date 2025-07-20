using Bloxstrap.UI.Elements.Settings.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class FindFlagDialog : Base.WpfUiWindow, INotifyPropertyChanged
    {
        private const int _debounceDelay = 300;

        private Dictionary<string, string> _cachedFlagDictionary = new();

        public ObservableCollection<FlagEntry> FilteredFlags { get; } = new();

        private bool _isFlagsLoaded = false;
        public bool IsFlagsLoaded
        {
            get => _isFlagsLoaded;
            set
            {
                if (_isFlagsLoaded != value)
                {
                    _isFlagsLoaded = value;
                    OnPropertyChanged(nameof(IsFlagsLoaded));
                }
            }
        }

        private readonly DispatcherTimer _loadingDotsTimer;
        private int _dotCount = 0;

        public FindFlagDialog()
        {
            InitializeComponent();
            DataContext = this;

            _loadingDotsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _loadingDotsTimer.Tick += (s, e) =>
            {
                _dotCount = (_dotCount + 1) % 4;
                LoadingDotsText.Text = new string('.', _dotCount);
            };

            _ = LoadAndDisplayMergedFlags();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        private void StartLoadingDotsAnimation() => _loadingDotsTimer.Start();
        private void StopLoadingDotsAnimation()
        {
            _loadingDotsTimer?.Stop();
            LoadingDotsText.Text = string.Empty;
        }

        private async Task LoadAndDisplayMergedFlags()
        {
            FlagCountTextBlock.Text = "Loading flags, please wait...";
            IsFlagsLoaded = false;
            StartLoadingDotsAnimation();

            try
            {
                using var client = App.HttpClient ?? new HttpClient();
                var mergedFlags = await FastFlagEditorPage.LoadCombinedFlagsAsync(client);
                _cachedFlagDictionary = mergedFlags;
                IsFlagsLoaded = true;
                ApplyFiltersAndDisplay();
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Error loading flags: {ex.Message}", MessageBoxImage.Error, MessageBoxButton.OK);
                _cachedFlagDictionary.Clear();
                FilteredFlags.Clear();
                FlagCountTextBlock.Text = "Failed to load flags.";
                IsFlagsLoaded = false;
            }
            finally
            {
                StopLoadingDotsAnimation();
            }
        }

        private void ApplyFiltersAndDisplay()
        {
            if (!IsFlagsLoaded)
                return;

            var filtered = FilterFlags();
            UpdateFilteredFlags(filtered);
            FlagCountTextBlock.Text = $"Flags Found: {FilteredFlags.Count}";
        }

        private IEnumerable<KeyValuePair<string, string>> FilterFlags()
        {
            string keywordText = KeywordSearchingTextbox.Text.Trim();
            string valueSearchText = ValueSearchTextBox.Text.Trim();
            bool showTrueOnly = TrueOnlyCheckBox.IsChecked == true;
            bool showFalseOnly = FalseOnlyCheckBox.IsChecked == true;
            string selectedType = (ValueTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All";
            string selectedPrefix = (PrefixFilterComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All";

            IEnumerable<KeyValuePair<string, string>> filtered = _cachedFlagDictionary;

            if (!string.IsNullOrWhiteSpace(keywordText))
            {
                var keywords = keywordText
                    .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(k => k.Trim());

                filtered = filtered.Where(kvp =>
                {
                    var keyToCheck = kvp.Key;
                    return keywords.All(kw => keyToCheck.Contains(kw, StringComparison.OrdinalIgnoreCase));
                });
            }

            if (!string.IsNullOrWhiteSpace(valueSearchText))
            {
                var values = valueSearchText
                    .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim());

                filtered = filtered.Where(kvp =>
                {
                    var valueToCheck = kvp.Value;
                    return values.Any(v => valueToCheck.Contains(v, StringComparison.OrdinalIgnoreCase));
                });
            }

            if (showTrueOnly && !showFalseOnly)
            {
                filtered = filtered.Where(kvp => kvp.Value.Equals("true", StringComparison.OrdinalIgnoreCase));
            }
            else if (!showTrueOnly && showFalseOnly)
            {
                filtered = filtered.Where(kvp => kvp.Value.Equals("false", StringComparison.OrdinalIgnoreCase));
            }

            if (!string.Equals(selectedType, "All", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(kvp =>
                {
                    string type = GetFlagType(kvp.Key);
                    return selectedType.Equals(type, StringComparison.OrdinalIgnoreCase);
                });
            }

            if (!string.Equals(selectedPrefix, "All", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(kvp =>
                    kvp.Key.StartsWith(selectedPrefix, StringComparison.OrdinalIgnoreCase));
            }

            return filtered;
        }

        private void UpdateFilteredFlags(IEnumerable<KeyValuePair<string, string>> filtered)
        {
            FilteredFlags.Clear();
            foreach (var kvp in filtered)
            {
                var type = GetFlagType(kvp.Key);
                var value = NormalizeDisplayValue(kvp.Value, type);

                FilteredFlags.Add(new FlagEntry
                {
                    Name = kvp.Key,
                    Value = value,
                    Type = type
                });
            }
        }

        private string NormalizeDisplayValue(string value, string type)
        {
            if ((type == "Boolean" || type == "Unknown") &&
                (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("false", StringComparison.OrdinalIgnoreCase)))
            {
                return char.ToUpper(value[0]) + value.Substring(1).ToLower();
            }

            return value;
        }

        private string GetFlagType(string flagName)
        {
            if (flagName.StartsWith("DFFlag", StringComparison.OrdinalIgnoreCase) ||
                flagName.StartsWith("FFlag", StringComparison.OrdinalIgnoreCase) ||
                flagName.StartsWith("FLog", StringComparison.OrdinalIgnoreCase) ||
                flagName.StartsWith("DFLog", StringComparison.OrdinalIgnoreCase) ||
                flagName.StartsWith("SFFlag", StringComparison.OrdinalIgnoreCase))
                return "Boolean";

            if (flagName.StartsWith("FInt", StringComparison.OrdinalIgnoreCase) ||
                flagName.StartsWith("DFInt", StringComparison.OrdinalIgnoreCase))
                return "Integer";

            if (flagName.StartsWith("FString", StringComparison.OrdinalIgnoreCase) ||
                flagName.StartsWith("DFString", StringComparison.OrdinalIgnoreCase))
                return "String";

            return "Unknown";
        }

        private void KeywordSearching(object sender, TextChangedEventArgs e)
        {
            if (!IsFlagsLoaded) return;
            DebouncedFilter();
        }

        private void ValueSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsFlagsLoaded) return;
            DebouncedFilter();
        }

        private void FilterCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsFlagsLoaded) return;
            ApplyFiltersAndDisplay();
        }

        private void ValueTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsFlagsLoaded) return;
            ApplyFiltersAndDisplay();
        }

        private CancellationTokenSource? _debounceTokenSource;
        private async void DebouncedFilter()
        {
            _debounceTokenSource?.Cancel();
            _debounceTokenSource = new CancellationTokenSource();

            try
            {
                await Task.Delay(_debounceDelay, _debounceTokenSource.Token);
                if (!_debounceTokenSource.Token.IsCancellationRequested)
                {
                    ApplyFiltersAndDisplay();
                }
            }
            catch (TaskCanceledException) { }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var exportDict = FilteredFlags.ToDictionary(f => f.Name, f => f.Value);
                var json = JsonSerializer.Serialize(exportDict, new JsonSerializerOptions { WriteIndented = true });

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    DefaultExt = ".json",
                    FileName = "flags_export.json"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(saveDialog.FileName, json);
                    MessageBox.Show("Export successful!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrefixFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsFlagsLoaded) return;
            ApplyFiltersAndDisplay();
        }

        private void FlagDataGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.C &&
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                var selectedFlags = FlagDataGrid.SelectedItems.Cast<FlagEntry>().ToList();
                if (selectedFlags.Count > 0)
                {
                    try
                    {
                        var dict = selectedFlags.ToDictionary(f => f.Name, f => f.Value);
                        var json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                        Clipboard.SetText(json);
                        e.Handled = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to copy flags to clipboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class FlagEntry
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public string Type { get; set; } = "";
    }
}