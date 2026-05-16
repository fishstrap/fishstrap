using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Shell;
using System.Windows.Controls;
using System.Windows;

using Bloxstrap.UI.ViewModels.Bootstrapper;
using Bloxstrap.UI.Elements.Bootstrapper.Base;
using System.Windows.Media;

namespace Bloxstrap.UI.Elements.Bootstrapper
{
    public partial class TerminalDialog : IBootstrapperDialog
    {
        private readonly TerminalDialogViewModel _viewModel;

        public Bloxstrap.Bootstrapper? Bootstrapper { get; set; }

        private bool _isClosing;

        #region UI Elements
        public string Message
        {
            get => _viewModel.Message;
            set
            {
                if (string.IsNullOrEmpty(_viewModel.Message))
                {
                    _viewModel.Message = value;
                }
                else
                {
                    string downloadPrefix = Strings.Bootstrapper_Status_Downloading;
                    int lastNewlineIndex = _viewModel.Message.LastIndexOf('\n');
                    string lastLine = lastNewlineIndex == -1 ? _viewModel.Message : _viewModel.Message.Substring(lastNewlineIndex + 1).Trim();

                    string lastLineIdentity = lastLine.LastIndexOf(" - ") != -1 ? lastLine.Substring(0, lastLine.LastIndexOf(" - ")) : lastLine;
                    string valueIdentity = value.LastIndexOf(" - ") != -1 ? value.Substring(0, value.LastIndexOf(" - ")) : value;

                    if (lastLine.StartsWith(downloadPrefix) && value.StartsWith(downloadPrefix) && lastLineIdentity == valueIdentity)
                    {
                        if (lastNewlineIndex == -1)
                        {
                            _viewModel.Message = value;
                        }
                        else
                        {
                            _viewModel.Message = _viewModel.Message.Substring(0, lastNewlineIndex + 1) + value;
                        }
                    }
                    else
                    {
                        _viewModel.Message += "\n" + value;
                    }
                }

                _viewModel.OnPropertyChanged(nameof(_viewModel.Message));
            }
        }

        public ProgressBarStyle ProgressStyle
        {
            get => _viewModel.ProgressIndeterminate ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
            set
            {
                _viewModel.ProgressIndeterminate = (value == ProgressBarStyle.Marquee);
                _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressIndeterminate));
            }
        }

        public int ProgressMaximum
        {
            get => _viewModel.ProgressMaximum;
            set
            {
                _viewModel.ProgressMaximum = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressMaximum));
            }
        }

        public int ProgressValue
        {
            get => _viewModel.ProgressValue;
            set
            {
                _viewModel.ProgressValue = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressValue));
            }
        }

        public TaskbarItemProgressState TaskbarProgressState
        {
            get => _viewModel.TaskbarProgressState;
            set
            {
                _viewModel.TaskbarProgressState = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.TaskbarProgressState));
            }
        }

        public double TaskbarProgressValue
        {
            get => _viewModel.TaskbarProgressValue;
            set
            {
                _viewModel.TaskbarProgressValue = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.TaskbarProgressValue));
            }
        }

        public bool CancelEnabled
        {
            get => _viewModel.CancelEnabled;
            set
            {
                _viewModel.CancelEnabled = value;

                _viewModel.OnPropertyChanged(nameof(_viewModel.CancelButtonVisibility));
                _viewModel.OnPropertyChanged(nameof(_viewModel.CancelEnabled));
            }
        }
        #endregion

        public TerminalDialog()
        {
            InitializeComponent();

            _viewModel = new TerminalDialogViewModel(this);
            DataContext = _viewModel;

            if (!App.Settings.Prop.UseAcrylicBackground)
                this.Resources["ApplicationBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));

            Title = App.Settings.Prop.BootstrapperTitle;
            Icon = App.Settings.Prop.BootstrapperIcon.GetIcon().GetImageSource();
        }

        private void UiWindow_Closing(object sender, RoutedEventArgs e)
        {
            if (!_isClosing)
                Bootstrapper?.Cancel();
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e) 
        {
            if (e.ExtentHeightChange > 0)
                ScrollViewer.ScrollToBottom();
        }

        // this is needed to center the titlebar buttonns
        private void TerminalTitleBar_Loaded(object sender, RoutedEventArgs e)
        {
            var closeButton = TerminalTitleBar.Template.FindName("PART_CloseButton", TerminalTitleBar) as FrameworkElement;
            var buttonGrid = closeButton?.Parent as Grid;

            if (buttonGrid != null)
            {
                buttonGrid.VerticalAlignment = VerticalAlignment.Center;

                foreach (UIElement child in buttonGrid.Children)
                {
                    var button = child as FrameworkElement;

                    if (button != null)
                        button.Height = 40;
                }
            }
        }

        #region IBootstrapperDialog Methods
        public void ShowBootstrapper() => this.ShowDialog();

        public void CloseBootstrapper()
        {
            _isClosing = true;
            Dispatcher.BeginInvoke(this.Close);
        }

        public void ShowSuccess(string message, Action? callback) => BaseFunctions.ShowSuccess(message, callback);
        #endregion
    }
}