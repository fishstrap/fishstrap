using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class FindFlagDialog : Base.WpfUiWindow
    {
        private ComboBox? _sourceSelectorComboBox;
        private CancellationTokenSource? _searchCancellationTokenSource;
        private const int _debounceDelay = 300;

        private List<string> _cachedRawFlags = new();
        private Dictionary<string, string> _cachedFlagDictionary = new();

        private bool _isCheckAllSelected = false;

        public FindFlagDialog()
        {
            InitializeComponent();
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                _sourceSelectorComboBox = comboBox;

                if (comboBox.Items.Count == 0)
                {
                    PopulateComboBoxItems(comboBox);
                    comboBox.SelectedIndex = 0;
                }
            }
        }

        private void PopulateComboBoxItems(ComboBox comboBox)
        {
            comboBox.Items.Clear();
            comboBox.Items.Add("Check All Sources");
            comboBox.Items.Add("Froststrap's PCDesktopClient");
            comboBox.Items.Add("Froststrap's FVariablesV2");
            comboBox.Items.Add("Live Roblox FastFlags");
            comboBox.Items.Add("MaximumADHD PCDesktopClient");
            comboBox.Items.Add("MaximumADHD PCClientBootstrapper");
            comboBox.Items.Add("MaximumADHD FVariable");
        }

        private async void SourceSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_sourceSelectorComboBox == null)
                return;

            var selectedSource = _sourceSelectorComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedSource))
                return;

            SetFlagOutputText("Loading flags, please wait...");

            try
            {
                if (selectedSource == "Check All Sources")
                {
                    _isCheckAllSelected = true;

                    var mergedFlags = await LoadAndMergeJsonSources();

                    _cachedFlagDictionary = mergedFlags;
                    _cachedRawFlags.Clear();

                    SetFlagOutputText(JsonSerializer.Serialize(_cachedFlagDictionary, new JsonSerializerOptions { WriteIndented = true }));
                }
                else if (selectedSource == "MaximumADHD FVariable")
                {
                    _isCheckAllSelected = false;

                    var rawText = await DownloadAndFormatRawText("https://raw.githubusercontent.com/MaximumADHD/Roblox-Client-Tracker/refs/heads/roblox/FVariables.txt");

                    _cachedRawFlags = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                    _cachedFlagDictionary.Clear();

                    SetFlagOutputText(string.Join(Environment.NewLine, _cachedRawFlags));
                }
                else
                {
                    _isCheckAllSelected = false;

                    var flagsText = await LoadFlagsForSource(selectedSource);

                    var parsedFlags = JsonSerializer.Deserialize<Dictionary<string, string>>(flagsText);

                    if (parsedFlags != null)
                    {
                        _cachedFlagDictionary = parsedFlags;
                        _cachedRawFlags.Clear();
                    }
                    else
                    {
                        _cachedFlagDictionary.Clear();
                        _cachedRawFlags.Clear();
                    }

                    SetFlagOutputText(JsonSerializer.Serialize(_cachedFlagDictionary, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch (Exception ex)
            {
                SetFlagOutputText($"Error loading flags: {ex.Message}");
                _cachedFlagDictionary.Clear();
                _cachedRawFlags.Clear();
            }
        }

        private async Task<Dictionary<string, string>> LoadAndMergeJsonSources()
        {
            // List of URLs to merge (exclude raw text source)
            var urls = new[]
            {
                "https://raw.githubusercontent.com/SCR00M/froststap-shi/refs/heads/main/PCDesktopClient.json",
                "https://raw.githubusercontent.com/SCR00M/froststap-shi/refs/heads/main/FVariablesV2.json",
                "https://clientsettings.roblox.com/v2/settings/application/PCDesktopClient",
                "https://raw.githubusercontent.com/MaximumADHD/Roblox-FFlag-Tracker/refs/heads/main/PCDesktopClient.json",
                "https://raw.githubusercontent.com/MaximumADHD/Roblox-FFlag-Tracker/refs/heads/main/PCClientBootstrapper.json"
            };

            var mergedFlags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var url in urls)
            {
                Dictionary<string, string> flags = url.Contains("clientsettings.roblox.com")
                    ? await DownloadJsonFlags(url, isLiveSettings: true)
                    : await DownloadJsonFlags(url);

                foreach (var kvp in flags)
                {
                    mergedFlags[kvp.Key] = kvp.Value;
                }
            }

            var sorted = mergedFlags.OrderBy(kvp => kvp.Key)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return sorted;
        }

        private async Task<Dictionary<string, string>> DownloadJsonFlags(string url, bool isLiveSettings = false)
        {
            using var client = new HttpClient();
            var json = await client.GetStringAsync(url);

            var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using var doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            if (isLiveSettings)
            {
                if (root.TryGetProperty("applicationSettings", out JsonElement appSettings))
                {
                    foreach (var prop in appSettings.EnumerateObject())
                        flags[prop.Name] = prop.Value.ToString();
                }
            }
            else
            {
                foreach (var prop in root.EnumerateObject())
                    flags[prop.Name] = prop.Value.ToString();
            }

            return flags;
        }

        private async Task<string> LoadFlagsForSource(string sourceName)
        {
            return sourceName switch
            {
                "Froststrap's PCDesktopClient" => await DownloadAndFormatJson("https://raw.githubusercontent.com/SCR00M/froststap-shi/refs/heads/main/PCDesktopClient.json"),
                "Froststrap's FVariablesV2" => await DownloadAndFormatJson("https://raw.githubusercontent.com/SCR00M/froststap-shi/refs/heads/main/FVariablesV2.json"),
                "Live Roblox FastFlags" => await DownloadAndFormatJson("https://clientsettings.roblox.com/v2/settings/application/PCDesktopClient", isLiveSettings: true),
                "MaximumADHD PCDesktopClient" => await DownloadAndFormatJson("https://raw.githubusercontent.com/MaximumADHD/Roblox-FFlag-Tracker/refs/heads/main/PCDesktopClient.json"),
                "MaximumADHD PCClientBootstrapper" => await DownloadAndFormatJson("https://raw.githubusercontent.com/MaximumADHD/Roblox-FFlag-Tracker/refs/heads/main/PCClientBootstrapper.json"),
                "MaximumADHD FVariable" => await DownloadAndFormatRawText("https://raw.githubusercontent.com/MaximumADHD/Roblox-Client-Tracker/refs/heads/roblox/FVariables.txt"),
                _ => "Unknown source"
            };
        }

        private async Task<string> DownloadAndFormatJson(string url, bool isLiveSettings = false)
        {
            var flags = await DownloadJsonFlags(url, isLiveSettings);

            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(flags.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value), options);
        }

        private async Task<string> DownloadAndFormatRawText(string url)
        {
            using var client = new HttpClient();
            var rawText = await client.GetStringAsync(url);

            var lines = rawText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            var cleanLines = lines
                .Select(line =>
                {
                    int idx = line.IndexOf('}');
                    return (line.StartsWith("{") && idx >= 0) ? line[(idx + 1)..].Trim() : line.Trim();
                })
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .OrderBy(line => line);

            return string.Join(Environment.NewLine, cleanLines);
        }

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textbox) return;

            string newSearch = textbox.Text.Trim();

            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource = new CancellationTokenSource();

            try
            {
                await Task.Delay(_debounceDelay, _searchCancellationTokenSource.Token);

                if (_searchCancellationTokenSource.Token.IsCancellationRequested)
                    return;

                if (string.IsNullOrEmpty(newSearch))
                {
                    if (_isCheckAllSelected || _cachedFlagDictionary.Count > 0)
                    {
                        SetFlagOutputText(JsonSerializer.Serialize(_cachedFlagDictionary, new JsonSerializerOptions { WriteIndented = true }));
                    }
                    else if (_cachedRawFlags.Count > 0)
                    {
                        SetFlagOutputText(string.Join(Environment.NewLine, _cachedRawFlags));
                    }
                    else
                    {
                        SetFlagOutputText(string.Empty);
                    }
                }
                else
                {
                    if (_isCheckAllSelected || _cachedFlagDictionary.Count > 0)
                    {
                        var filteredDict = _cachedFlagDictionary
                            .Where(kvp => kvp.Key.IndexOf(newSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                            .OrderBy(kvp => kvp.Key)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                        SetFlagOutputText(filteredDict.Count > 0
                            ? JsonSerializer.Serialize(filteredDict, new JsonSerializerOptions { WriteIndented = true })
                            : "No matching flags found.");
                    }
                    else if (_cachedRawFlags.Count > 0)
                    {
                        var filteredRaw = _cachedRawFlags
                            .Where(f => f.IndexOf(newSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                            .ToList();

                        SetFlagOutputText(filteredRaw.Count > 0
                            ? string.Join(Environment.NewLine, filteredRaw)
                            : "No matching flags found.");
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // ignored
            }
        }

        private void SetFlagOutputText(string text)
        {
            FlagOutputBox.Text = text;
            UpdateFlagCount();
        }

        private void UpdateFlagCount()
        {
            int lineCount = 0;
            if (!string.IsNullOrEmpty(FlagOutputBox.Text))
            {
                lineCount = FlagOutputBox.Text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            }

            FlagCountTextBlock.Text = $"Total Flag Count: {lineCount}";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}