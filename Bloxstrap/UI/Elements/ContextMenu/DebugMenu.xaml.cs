using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Bloxstrap.UI.Elements.Base;

namespace Bloxstrap.UI.Elements.ContextMenu
{
    public partial class DebugMenu : WpfUiWindow
    {
        public DebugMenu()
        {
            InitializeComponent();
            LoadLogFilesList();
            UpdateButtonStates();
        }

        private void LoadLogFilesList()
        {
            LogFilesList.Items.Clear();
            if (!Directory.Exists(Paths.Logs)) return;
            var files = Directory.GetFiles(Paths.Logs, "*.log").OrderByDescending(File.GetLastWriteTime);
            foreach (var file in files) LogFilesList.Items.Add(Path.GetFileName(file));
            if (LogFilesList.Items.Count > 0) LogFilesList.SelectedIndex = 0;
            UpdateButtonStates();
        }

        private void LogFilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LogFilesList.SelectedItem is not string fileName)
            {
                LogListBox.Items.Clear();
                UpdateButtonStates();
                return;
            }

            string filePath = Path.Combine(Paths.Logs, fileName);
            if (!File.Exists(filePath))
            {
                LogListBox.Items.Clear();
                LogListBox.Items.Add("Selected log file does not exist.");
                UpdateButtonStates();
                return;
            }

            try
            {
                var contents = File.ReadAllLines(filePath);
                LogListBox.Items.Clear();
                foreach (var line in contents) LogListBox.Items.Add(line);
            }
            catch (System.Exception ex)
            {
                LogListBox.Items.Clear();
                LogListBox.Items.Add($"Failed to read log file:\n{ex.Message}");
            }
            UpdateButtonStates();
        }

        private void LogListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonStates();
        }

        private void CopyLogs_Click(object sender, RoutedEventArgs e)
        {
            if (LogListBox.Items.Count == 0)
            {
                Frontend.ShowMessageBox("No log lines to copy.", MessageBoxImage.Information);
                return;
            }
            Clipboard.SetText(string.Join("\n", LogListBox.Items.Cast<string>()));
        }

        private void CopySelected_Click(object sender, RoutedEventArgs e)
        {
            if (LogListBox.SelectedItems.Count == 0)
            {
                Frontend.ShowMessageBox("No lines selected to copy.", MessageBoxImage.Information);
                return;
            }
            Clipboard.SetText(string.Join("\n", LogListBox.SelectedItems.Cast<string>()));
        }

        private void RefreshLogs_Click(object sender, RoutedEventArgs e)
        {
            if (LogFilesList.SelectedItem is string)
                LogFilesList_SelectionChanged(this, null!);
            else
                Frontend.ShowMessageBox("No log file selected.", MessageBoxImage.Warning);
        }

        private void OpenLogsFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(Paths.Logs))
                {
                    Frontend.ShowMessageBox("Logs folder does not exist.", MessageBoxImage.Information);
                    return;
                }
                Process.Start(new ProcessStartInfo { FileName = Paths.Logs, UseShellExecute = true });
            }
            catch (System.Exception ex)
            {
                Frontend.ShowMessageBox($"Failed to open logs folder:\n{ex.Message}", MessageBoxImage.Error);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (LogFilesList.SelectedItem is not string fileName) return;
            string filePath = Path.Combine(Paths.Logs, fileName);
            if (!File.Exists(filePath)) return;

            string filter = SearchBox.Text.Trim();
            try
            {
                var allLines = File.ReadAllLines(filePath);
                LogListBox.Items.Clear();
                foreach (var line in allLines)
                    if (line.Contains(filter, System.StringComparison.OrdinalIgnoreCase))
                        LogListBox.Items.Add(line);
            }
            catch { }
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool logFileSelected = LogFilesList.SelectedItem is string;
            bool hasLogLines = LogListBox.Items.Count > 0;
            bool hasSelectedLines = LogListBox.SelectedItems.Count > 0;

            RefreshButton.IsEnabled = logFileSelected;
            CopyAllButton.IsEnabled = hasLogLines;
            CopySelectedButton.IsEnabled = hasSelectedLines;
            OpenFolderButton.IsEnabled = true;
        }
    }
}