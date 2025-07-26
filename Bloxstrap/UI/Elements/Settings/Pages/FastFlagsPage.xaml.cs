using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Bloxstrap.UI.ViewModels.Settings;
using Wpf.Ui.Controls;
using Wpf.Ui.Mvvm.Contracts;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    public partial class FastFlagsPage : UiPage
    {
        private CancellationTokenSource? _searchDebounceCts;
        private bool _initialLoad = false;
        private FastFlagsViewModel _viewModel;
        private List<FrameworkElement> _optionControls = new();
        private List<CardExpander> _cardExpanders;

        private List<FrameworkElement> _recommendedOptions = new();
        private List<FrameworkElement> _experimentalOptions = new();
        private List<FrameworkElement> _debugOptions = new();

        // too lazy to add to enums so im doing it here
        public enum SearchMode
        {
            All,
            Title,
            Description,
        }

        public SearchMode SelectedSearchMode { get; set; } = SearchMode.All;
        public IEnumerable<SearchMode> SearchModes => Enum.GetValues(typeof(SearchMode)).Cast<SearchMode>();


        public FastFlagsPage()
        {
            InitializeComponent();
            _viewModel = new FastFlagsViewModel();
            _cardExpanders = new List<CardExpander>();
            SetupViewModel();
            Loaded += FastFlagsPage_Loaded;
        }

        #region FFlag Options

        private void FastFlagsPage_Loaded(object sender, RoutedEventArgs e)
        {
            _cardExpanders = new List<CardExpander>();
            foreach (var expander in FindVisualChildren<CardExpander>(this))
                _cardExpanders.Add(expander);

            _optionControls = new List<FrameworkElement>();
            foreach (var option in FindVisualChildren<FrameworkElement>(this))
            {
                if (option.GetType().Name == "OptionControl")
                    _optionControls.Add(option);
            }

            _cardExpanders = new List<CardExpander>
            {
                SystemExpander,
                RenderingExpander,
                BasicExpander,
                UserInterfaceExpander,
                RobloxMenuExpander,
                PrivacyExpander,
                SystemExperimentalExpander,
                RenderingAdvancedExpander,
                NetworkExpander,
                MiscExpander,
                SkyExpander,
            };

            _recommendedOptions = new List<FrameworkElement>
            {
                PreferredGPU,
                ForceLogicalProcessors,
                ForceAsyncThreads,
                RefreshRate,
                DisableShadows,
                DisablePostFX,
                DisableTerrainTextures,
                RemoveGrass,
                GraySky,
                DisableClouds,
                GrayAvatars,
                MSAA,
                MinimalRendering,
                FPSLimit,
                LightingTechnology,
                FixDisplayScaling,
                RenderingMode,
                TextureQuality,
                PixelResolution,
                VRToggle,
                FeedBackButton,
                LanguageSelector,
                FrameRate,
                ChatTranslation,
                FullscreenTitlebar,
                NoDisconnectMessage,
                DisableAds,
                HidePurchaseUI,
                DragableCapture,
                HideGUI,
                FontPaddingOption,
                RobloxTelemetry,
                WebView2Telemetry,
                VoiceChatTelemetry,
                BlockTencent,
                BlockVNG,
            };

            _experimentalOptions = new List<FrameworkElement>
            {
                BypassVulkan,
                MemoryProbing,
                LightCulling,
                NewFrameRateSystem,
                PreRender,
                RainbowSky,
                BlackSky,
                WhiteSky,
                LowPolyMeshes,
                FrameCreationBufferPercentage,
                StartingGraphicsLevel,
                OverrideGraphicQualityLevel,
                TextureSkipping,
                NetworkOutputStabilization,
                NoPayloadLimit,
                RCore,
                NetworkStream,
                LargeReplicator,
                IncreaseCacheSize,
                FasterLoadding,
                BufferArray,
                PhysicSenderRate,
                MTU,
                UnthemedInstances,
                RedFont,
                Pseudolocalization,
                DisableLayeredClothing,
                MoreCharacters,
            };

            _debugOptions = new List<FrameworkElement>
            {
                Chunks,
                PingBreakdownOption,
                FlagStateOption
            };
        }
        #endregion

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchDebounceCts?.Cancel();
            _searchDebounceCts = new CancellationTokenSource();
            var token = _searchDebounceCts.Token;

            Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    await Task.Delay(300, token);
                    if (!token.IsCancellationRequested)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            PerformSearchBoxFiltering();
                            ShowSearchSuggestion(SearchTextBox.Text.Trim());
                        });
                    }
                }
                catch (TaskCanceledException)
                {
                    // Ignore cancellation
                }
            });
        }

        private void PerformSearchBoxFiltering()
        {
            string search = SearchTextBox.Text.Trim().ToLowerInvariant();
            bool isSearching = !string.IsNullOrWhiteSpace(search);

            bool anyVisible = false;

            foreach (var option in _optionControls)
            {
                string? header = option.GetType().GetProperty("Header")?.GetValue(option)?.ToString();
                string? description = option.GetType().GetProperty("Description")?.GetValue(option)?.ToString();
                if (string.IsNullOrEmpty(header))
                    header = option.Name;

                bool matches = !isSearching;

                if (isSearching)
                {
                    switch (SelectedSearchMode)
                    {
                        case SearchMode.Title:
                            matches = header?.ToLowerInvariant().Contains(search) ?? false;
                            break;
                        case SearchMode.Description:
                            matches = description?.ToLowerInvariant().Contains(search) ?? false;
                            break;
                        case SearchMode.All:
                            matches = (header?.ToLowerInvariant().Contains(search) ?? false)
                                || (description?.ToLowerInvariant().Contains(search) ?? false);
                            break;
                    }
                }

                option.Visibility = matches ? Visibility.Visible : Visibility.Collapsed;
                if (matches)
                    anyVisible = true;
            }

            if (isSearching)
            {
                if (ManagerEnabled != null)
                    ManagerEnabled.Visibility = Visibility.Collapsed;
                if (Reset != null)
                    Reset.Visibility = Visibility.Collapsed;
            }

            // Show/hide category headers based on visible options
            RecommendedTextBlock.Visibility = _recommendedOptions.Exists(opt => opt.Visibility == Visibility.Visible)
                ? Visibility.Visible : Visibility.Collapsed;
            ExperimentalTextBlock.Visibility = _experimentalOptions.Exists(opt => opt.Visibility == Visibility.Visible)
                ? Visibility.Visible : Visibility.Collapsed;
            DebugTextBlock.Visibility = _debugOptions.Exists(opt => opt.Visibility == Visibility.Visible)
                ? Visibility.Visible : Visibility.Collapsed;

            // Show/hide and expand/collapse card expanders based on visible options
            foreach (var expander in _cardExpanders)
            {
                var expanderOptions = new List<FrameworkElement>();
                foreach (var child in FindVisualChildren<FrameworkElement>(expander))
                {
                    if (_optionControls.Contains(child))
                        expanderOptions.Add(child);
                }

                bool expanderAnyVisible = expanderOptions.Exists(opt => opt.Visibility == Visibility.Visible);

                expander.Visibility = expanderAnyVisible ? Visibility.Visible : Visibility.Collapsed;
                expander.IsExpanded = isSearching && expanderAnyVisible;
            }

            // Show or hide the "No results" message
            NoResultsTextBlock.Visibility = (isSearching && !anyVisible) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowSearchSuggestion(string searchFilter)
        {
            if (string.IsNullOrWhiteSpace(searchFilter))
            {
                AnimateSuggestionVisibility(0);
                return;
            }

            var bestMatch = _optionControls
                .Select(opt => opt.GetType().GetProperty("Header")?.GetValue(opt)?.ToString())
                .Where(flag => !string.IsNullOrEmpty(flag) && flag.Contains(searchFilter, StringComparison.OrdinalIgnoreCase))
                .OrderBy(flag => flag != null && !flag.StartsWith(searchFilter, StringComparison.OrdinalIgnoreCase))
                .ThenBy(flag => flag != null ? flag.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) : int.MaxValue)
                .ThenBy(flag => flag != null ? flag.Length : int.MaxValue)
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(bestMatch))
            {
                if (SuggestionKeywordRun != null)
                    SuggestionKeywordRun.Text = bestMatch;
                AnimateSuggestionVisibility(1);
            }
            else
            {
                AnimateSuggestionVisibility(0);
            }
        }

        private void SuggestionTextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var suggestion = SuggestionKeywordRun?.Text;
            if (!string.IsNullOrEmpty(suggestion) && SearchTextBox != null)
            {
                SearchTextBox.Text = suggestion;
                SearchTextBox.CaretIndex = suggestion.Length;
            }
        }

        private void AnimateSuggestionVisibility(double targetOpacity)
        {
            var opacityAnimation = new DoubleAnimation
            {
                To = targetOpacity,
                Duration = TimeSpan.FromMilliseconds(120),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            var translateAnimation = new DoubleAnimation
            {
                To = targetOpacity > 0 ? 0 : 10,
                Duration = TimeSpan.FromMilliseconds(120),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            opacityAnimation.Completed += (s, e) =>
            {
                if (targetOpacity == 0)
                {
                    SuggestionTextBlock.Visibility = Visibility.Collapsed;
                }
            };

            if (targetOpacity > 0)
                SuggestionTextBlock.Visibility = Visibility.Visible;

            SuggestionTextBlock.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
            SuggestionTranslateTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, translateAnimation);
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                if (child is T t)
                    yield return t;
                foreach (T childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }

        private void SetupViewModel()
        {
            _viewModel = new FastFlagsViewModel();

            _viewModel.OpenFlagEditorEvent += OpenFlagEditor;
            _viewModel.RequestPageReloadEvent += (_, _) => SetupViewModel();

            DataContext = _viewModel;
        }

        private void OpenFlagEditor(object? sender, EventArgs e)
        {
            if (Window.GetWindow(this) is INavigationWindow window)
            {
                window.Navigate(typeof(FastFlagEditorPage));
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_initialLoad)
            {
                _initialLoad = true;
                return;
            }

            SetupViewModel();
        }

        private void ValidateInt32(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == "-")
                e.Handled = ((sender as System.Windows.Controls.TextBox)?.CaretIndex != 0);
            else
                e.Handled = !int.TryParse(e.Text, out _);
        }

        private void ValidateUInt32(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !uint.TryParse(e.Text, out _);
        }

        private void OnConvertToFlagStateFormat_Click(object sender, RoutedEventArgs e)
        {
            var json = JsonInputTextBox.Text;

            try
            {
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    Frontend.ShowMessageBox("Please enter a valid JSON object.", MessageBoxImage.Error);
                    return;
                }

                var keys = new List<string>();
                foreach (var property in doc.RootElement.EnumerateObject())
                    keys.Add(property.Name);

                JsonInputTextBox.Text = string.Join(", ", keys);
            }
            catch (JsonException ex)
            {
                Frontend.ShowMessageBox($"Invalid JSON format: {ex.Message}", MessageBoxImage.Error);
            }
        }
    }
}
