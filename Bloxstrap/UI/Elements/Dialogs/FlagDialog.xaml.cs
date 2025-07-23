using System.Windows;
using System.Windows.Controls;
using Bloxstrap.UI.Elements.Settings.Pages;
using Bloxstrap.UI.Elements.Base;
using System.Collections.ObjectModel;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class FlagDialog : WpfUiWindow
    {
        private readonly FastFlagEditorPage _fastFlagEditor;

        private readonly ObservableCollection<dynamic> _invalidFlagsSource = new();
        private readonly ObservableCollection<dynamic> _defaultFlagsSource = new();
        private readonly ObservableCollection<dynamic> _updatedFlagsSource = new();

        private Dictionary<string, object> InvalidFlags { get; }
        private Dictionary<string, string> DefaultFlags { get; }
        private List<(string OldName, string NewName)> UpdatedFlags { get; }
        private readonly Action? OnFlagsRestored;

        public FlagDialog(
            FastFlagEditorPage fastFlagEditor,
            Dictionary<string, object> invalidFlagsDict,
            Dictionary<string, string> defaultValues,
            List<(string OldName, string NewName)> updatedFlags,
            Action? onFlagsRestored = null)
        {
            InitializeComponent();

            _fastFlagEditor = fastFlagEditor ?? throw new ArgumentNullException(nameof(fastFlagEditor));
            InvalidFlags = invalidFlagsDict;
            DefaultFlags = defaultValues;
            UpdatedFlags = updatedFlags;
            OnFlagsRestored = onFlagsRestored;

            if (invalidFlagsDict.Count > 0)
            {
                foreach (var kvp in invalidFlagsDict)
                {
                    string valueStr = kvp.Value?.ToString() ?? "";
                    valueStr = NormalizeBoolean(valueStr);

                    _invalidFlagsSource.Add(new { Key = kvp.Key, Value = valueStr, Status = "Removed" });
                }

                InvalidFlagsGrid.ItemsSource = _invalidFlagsSource;
                InvalidTab.Header = $"Invalid FastFlag{(invalidFlagsDict.Count > 1 ? "s" : "")} ({invalidFlagsDict.Count})";
            }
            else
            {
                TabControl.Items.Remove(InvalidTab);
            }

            if (defaultValues.Count > 0)
            {
                foreach (var kvp in defaultValues)
                {
                    string valueStr = NormalizeBoolean(kvp.Value);
                    _defaultFlagsSource.Add(new { Key = kvp.Key, Value = valueStr, Status = "Removed" });
                }

                DefaultValuesGrid.ItemsSource = _defaultFlagsSource;
                DefaultTab.Header = $"Default Value{(defaultValues.Count > 1 ? "s" : "")} ({defaultValues.Count})";
            }
            else
            {
                TabControl.Items.Remove(DefaultTab);
            }

            if (updatedFlags.Count > 0)
            {
                foreach (var pair in updatedFlags)
                {
                    _updatedFlagsSource.Add(new { OldName = pair.OldName, NewName = pair.NewName, Status = "Updated" });
                }

                UpdatedFlagsGrid.ItemsSource = _updatedFlagsSource;
                UpdatedTab.Header = $"Updated FastFlag{(updatedFlags.Count > 1 ? "s" : "")} ({updatedFlags.Count})";
            }
            else
            {
                TabControl.Items.Remove(UpdatedTab);
            }

            if (TabControl.Items.Count > 0)
                ((TabItem)TabControl.Items[0]).Focus();

            InvalidFlagsGrid.SelectionChanged += (_, _) => UpdateUndoButtonState();
            DefaultValuesGrid.SelectionChanged += (_, _) => UpdateUndoButtonState();
            UpdatedFlagsGrid.SelectionChanged += (_, _) => UpdateUndoButtonState();
            TabControl.SelectionChanged += (_, _) => UpdateUndoButtonVisibility();

            UpdateUndoButtonVisibility();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            int restored = 0;

            foreach (var item in InvalidFlagsGrid.SelectedItems.Cast<dynamic>().ToList())
            {
                string key = item.Key;
                if (InvalidFlags.TryGetValue(key, out object? value))
                {
                    App.FastFlags.SetValue(key, value?.ToString());
                    _invalidFlagsSource.Remove(item);
                    restored++;
                }
            }

            foreach (var item in DefaultValuesGrid.SelectedItems.Cast<dynamic>().ToList())
            {
                string key = item.Key;
                if (DefaultFlags.TryGetValue(key, out string? value))
                {
                    App.FastFlags.SetValue(key, value);
                    _defaultFlagsSource.Remove(item);
                    restored++;
                }
            }

            foreach (var item in UpdatedFlagsGrid.SelectedItems.Cast<dynamic>().ToList())
            {
                string oldName = item.OldName;
                string newName = item.NewName;
                string? value = App.FastFlags.GetValue(newName);

                App.FastFlags.SetValue(newName, null);
                App.FastFlags.SetValue(oldName, value);
                _updatedFlagsSource.Remove(item);
                restored++;
            }

            if (restored > 0)
            {
                _fastFlagEditor.UpdateTotalFlagsCount();
                OnFlagsRestored?.Invoke();
            }
            else
            {
                Frontend.ShowMessageBox("No FastFlags selected to undo.", MessageBoxImage.Warning);
            }

            InvalidFlagsGrid.UnselectAll();
            DefaultValuesGrid.UnselectAll();
            UpdatedFlagsGrid.UnselectAll();

            UpdateUndoButtonState();
        }

        private void UpdateUndoButtonState()
        {
            UndoButton.IsEnabled =
                InvalidFlagsGrid.SelectedItems.Count > 0 ||
                DefaultValuesGrid.SelectedItems.Count > 0 ||
                UpdatedFlagsGrid.SelectedItems.Count > 0;
        }

        private void UpdateUndoButtonVisibility()
        {
            UndoButton.Visibility = Visibility.Visible;
            UpdateUndoButtonState();
        }

        private static string NormalizeBoolean(string value)
        {
            if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
                return "True";
            if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
                return "False";
            return value;
        }
    }
}