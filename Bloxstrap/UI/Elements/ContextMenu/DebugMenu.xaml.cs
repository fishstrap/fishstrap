using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using Bloxstrap.UI.Elements.Base;

namespace Bloxstrap.UI.Elements.ContextMenu
{
    public partial class DebugMenu : WpfUiWindow
    {
        private readonly string _logFilePath;
        private FileSystemWatcher? _logWatcher;
        private string[] _allLogLines = Array.Empty<string>();

        public DebugMenu(string logFilePath)
        {
            InitializeComponent();
            _logFilePath = logFilePath;
            LoadLogFile();
            SetupLogWatcher();
        }

        private void LoadLogFile()
        {
            if (!File.Exists(_logFilePath))
            {
                LogListBox.Items.Clear();
                LogListBox.Items.Add("Log file not found.");
                return;
            }

            try
            {
                LogListBox.Items.Clear();

                using var stream = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                _allLogLines = reader.ReadToEnd().Split(Environment.NewLine);

                foreach (var line in _allLogLines)
                    LogListBox.Items.Add(line);
            }
            catch (IOException ex)
            {
                LogListBox.Items.Clear();
                LogListBox.Items.Add($"Error reading log file: {ex.Message}");
            }
        }

        private void SetupLogWatcher()
        {
            if (!File.Exists(_logFilePath))
                return;

            _logWatcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(_logFilePath)!,
                Filter = Path.GetFileName(_logFilePath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            _logWatcher.Changed += OnLogFileChanged;
            _logWatcher.Renamed += OnLogFileChanged;
        }

        private void OnLogFileChanged(object? sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    using var stream = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(stream);
                    var lines = reader.ReadToEnd().Split(Environment.NewLine);

                    if (lines.Length <= _allLogLines.Length)
                        return;

                    var newLines = lines.Skip(_allLogLines.Length).ToArray();
                    _allLogLines = lines;

                    foreach (var line in newLines)
                        LogListBox.Items.Add(line);

                    LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
                }
                catch
                {
                    // Ignore read errors
                }
            });
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_allLogLines.Length == 0)
                return;

            string filter = SearchBox.Text.Trim();
            LogListBox.Items.Clear();

            foreach (var line in _allLogLines)
            {
                if (line.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    LogListBox.Items.Add(line);
            }
        }

        private void ClearLogs_Click(object sender, RoutedEventArgs e)
        {
            LogListBox.Items.Clear();
        }

        private void CopyLogs_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(string.Join(Environment.NewLine, LogListBox.Items.Cast<string>()));
        }

        private void OpenLogsFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logsFolderPath = Paths.Logs;

                if (!Directory.Exists(logsFolderPath))
                {
                    Frontend.ShowMessageBox("Logs folder does not exist.", MessageBoxImage.Information);
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = logsFolderPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Failed to open logs folder:\n{ex.Message}", MessageBoxImage.Error);
            }
        }

        private void RefreshLogs_Click(object sender, RoutedEventArgs e)
        {
            LoadLogFile();
        }

        private void SaveLogsAs_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Save Logs As",
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                FileName = "FroststrapLogs.txt"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllLines(dlg.FileName, LogListBox.Items.Cast<string>());
                    Frontend.ShowMessageBox("Logs saved successfully.", MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Frontend.ShowMessageBox($"Failed to save logs:\n{ex.Message}", MessageBoxImage.Error);
                }
            }
        }

        private void CopySelected_Click(object sender, RoutedEventArgs e)
        {
            if (LogListBox.SelectedItems.Count > 0)
            {
                var selectedText = string.Join(Environment.NewLine, LogListBox.SelectedItems.Cast<string>());
                Clipboard.SetText(selectedText);
            }
            else
            {
                Frontend.ShowMessageBox("No lines selected to copy.", MessageBoxImage.Information);
            }
        }


        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _logWatcher?.Dispose();
            _logWatcher = null;
        }
    }
}
