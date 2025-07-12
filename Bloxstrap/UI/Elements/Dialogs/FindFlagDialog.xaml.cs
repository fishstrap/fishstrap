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

        private void StartLoadingDotsAnimation()
        {
            _loadingDotsTimer.Start();
        }

        private void StopLoadingDotsAnimation()
        {
            _loadingDotsTimer?.Stop();
            LoadingDotsText.Text = "";
        }

        private async Task LoadAndDisplayMergedFlags()
        {
            FlagCountTextBlock.Text = "Loading flags, please wait...";
            IsFlagsLoaded = false;
            StartLoadingDotsAnimation();

            try
            {
                var mergedFlags = await LoadAndMergeJsonSources();
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

        private async Task<Dictionary<string, string>> LoadAndMergeJsonSources()
        {
            var urls = new[]
            {
                "https://raw.githubusercontent.com/SCR00M/froststap-shi/refs/heads/main/PCDesktopClients.json",
                "https://clientsettings.roblox.com/v2/settings/application/PCDesktopClient",
                "https://raw.githubusercontent.com/MaximumADHD/Roblox-FFlag-Tracker/refs/heads/main/PCDesktopClient.json",
            };

            var mergedFlags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using var client = new HttpClient();

            foreach (var url in urls)
            {
                // Note: no try/catch here — handled inside DownloadJsonFlags
                Dictionary<string, string> flags = url.Contains("clientsettings.roblox.com")
                    ? await DownloadJsonFlags(client, url, isLiveSettings: true)
                    : await DownloadJsonFlags(client, url);

                foreach (var kvp in flags)
                    mergedFlags[kvp.Key] = kvp.Value;
            }

            return mergedFlags.OrderBy(kvp => kvp.Key)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private async Task<Dictionary<string, string>> DownloadJsonFlags(HttpClient client, string url, bool isLiveSettings = false)
        {
            var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    // Handle 404 and other HTTP errors gracefully by returning empty flags
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // Optionally log or ignore silently
                        return flags;
                    }
                    else
                    {
                        // You can also skip other HTTP errors or throw if needed
                        return flags;
                    }
                }

                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                if (isLiveSettings && root.TryGetProperty("applicationSettings", out JsonElement appSettings))
                {
                    foreach (var prop in appSettings.EnumerateObject())
                        flags[prop.Name] = prop.Value.ToString();
                }
                else
                {
                    foreach (var prop in root.EnumerateObject())
                        flags[prop.Name] = prop.Value.ToString();
                }
            }
            catch (HttpRequestException)
            {
                // Network or HTTP failure - skip
            }
            catch (JsonException)
            {
                // JSON parse failure - skip
            }
            catch (Exception)
            {
                // Other exceptions - skip
            }

            return flags;
        }

        private void ApplyFiltersAndDisplay()
        {
            if (!IsFlagsLoaded) return;

            string keywordText = KeywordSearchingTextbox.Text.Trim();
            string valueSearchText = ValueSearchTextBox.Text.Trim();

            bool showTrueOnly = TrueOnlyCheckBox.IsChecked == true;
            bool showFalseOnly = FalseOnlyCheckBox.IsChecked == true;
            string selectedType = (ValueTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All";

            IEnumerable<KeyValuePair<string, string>> filtered = _cachedFlagDictionary;

            if (!string.IsNullOrWhiteSpace(keywordText))
            {
                var keywords = keywordText
                    .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(k => k.Trim().ToLowerInvariant())
                    .ToArray();

                if (keywords.Length > 0)
                {
                    filtered = filtered.Where(kvp =>
                    {
                        string keyLower = kvp.Key.ToLowerInvariant();
                        return keywords.All(kw => keyLower.Contains(kw));
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(valueSearchText))
            {
                var values = valueSearchText
                    .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim().ToLowerInvariant())
                    .ToArray();

                if (values.Length > 0)
                {
                    filtered = filtered.Where(kvp =>
                    {
                        string valLower = kvp.Value.ToLowerInvariant();
                        return values.Any(v => valLower.Contains(v));
                    });
                }
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
                    string flagName = kvp.Key;
                    if (selectedType.Equals("Boolean", StringComparison.OrdinalIgnoreCase))
                    {
                        return flagName.StartsWith("DFFlag", StringComparison.OrdinalIgnoreCase)
                            || flagName.StartsWith("FFlag", StringComparison.OrdinalIgnoreCase)
                            || flagName.StartsWith("FLog", StringComparison.OrdinalIgnoreCase)
                            || flagName.StartsWith("DFLog", StringComparison.OrdinalIgnoreCase)
                            || flagName.StartsWith("SFFlag", StringComparison.OrdinalIgnoreCase);
                    }
                    else if (selectedType.Equals("Integer", StringComparison.OrdinalIgnoreCase))
                    {
                        return flagName.StartsWith("FInt", StringComparison.OrdinalIgnoreCase)
                            || flagName.StartsWith("DFInt", StringComparison.OrdinalIgnoreCase)
                            || flagName.StartsWith("FLog", StringComparison.OrdinalIgnoreCase)
                            || flagName.StartsWith("DFLog", StringComparison.OrdinalIgnoreCase);
                    }
                    else if (selectedType.Equals("String", StringComparison.OrdinalIgnoreCase))
                    {
                        return flagName.StartsWith("FString", StringComparison.OrdinalIgnoreCase)
                            || flagName.StartsWith("DFString", StringComparison.OrdinalIgnoreCase);
                    }
                    return true;
                });
            }

            FilteredFlags.Clear();
            foreach (var kvp in filtered)
            {
                string displayValue = kvp.Value;
                string flagType = GetFlagType(kvp.Key);

                if (flagType.Equals("Boolean", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(displayValue, "true", StringComparison.OrdinalIgnoreCase))
                        displayValue = "True";
                    else if (string.Equals(displayValue, "false", StringComparison.OrdinalIgnoreCase))
                        displayValue = "False";
                }
                else if (flagType.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(displayValue, "true", StringComparison.OrdinalIgnoreCase))
                        displayValue = "True";
                    else if (string.Equals(displayValue, "false", StringComparison.OrdinalIgnoreCase))
                        displayValue = "False";
                }

                FilteredFlags.Add(new FlagEntry
                {
                    Name = kvp.Key,
                    Value = displayValue,
                    Type = flagType
                });
            }

            FlagCountTextBlock.Text = $"Flags Found: {FilteredFlags.Count}";
        }

        private void ValueSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsFlagsLoaded) return;
            DebouncedFilter();
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

        private void FlagDataGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Check for Ctrl+C
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
                        e.Handled = true; // Prevent further processing
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
