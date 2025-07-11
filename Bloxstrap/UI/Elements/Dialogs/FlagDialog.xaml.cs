using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bloxstrap.UI.Elements.Base;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class FlagDialog : WpfUiWindow
    {
        public FlagDialog(
            Dictionary<string, object> invalidFlagsDict,
            Dictionary<string, string> defaultValues,
            List<(string OldName, string NewName)> updatedFlags)
        {
            InitializeComponent();

            Title = "FastFlag Changes";

            if (invalidFlagsDict.Count > 0)
            {
                InvalidFlagsGrid.ItemsSource = invalidFlagsDict
                    .Select(kvp =>
                    {
                        string valueStr = kvp.Value?.ToString() ?? string.Empty;

                        // Capitalize boolean strings
                        if (string.Equals(valueStr, "true", StringComparison.OrdinalIgnoreCase))
                            valueStr = "True";
                        else if (string.Equals(valueStr, "false", StringComparison.OrdinalIgnoreCase))
                            valueStr = "False";

                        return new
                        {
                            Key = kvp.Key,
                            Value = valueStr,
                            Status = "Removed"
                        };
                    })
                    .ToList();

                string pluralSuffix = invalidFlagsDict.Count > 1 ? "s" : "";
                InvalidTab.Header = $"Invalid FastFlag{pluralSuffix} ({invalidFlagsDict.Count})";
            }
            else
            {
                TabControl.Items.Remove(InvalidTab);
            }

            if (defaultValues.Count > 0)
            {
                DefaultValuesGrid.ItemsSource = defaultValues
                    .Select(kvp =>
                    {
                        string valueStr = kvp.Value;

                        if (string.Equals(valueStr, "true", StringComparison.OrdinalIgnoreCase))
                            valueStr = "True";
                        else if (string.Equals(valueStr, "false", StringComparison.OrdinalIgnoreCase))
                            valueStr = "False";

                        return new
                        {
                            Key = kvp.Key,
                            Value = valueStr,
                            Status = "Removed"
                        };
                    })
                    .ToList();

                string pluralSuffix = defaultValues.Count > 1 ? "s" : "";
                DefaultTab.Header = $"Default Value{pluralSuffix} ({defaultValues.Count})";
            }
            else
            {
                TabControl.Items.Remove(DefaultTab);
            }

            if (updatedFlags.Count > 0)
            {
                UpdatedFlagsGrid.ItemsSource = updatedFlags
                    .Select(pair => new
                    {
                        OldName = pair.OldName,
                        NewName = pair.NewName,
                        Status = "Updated"
                    })
                    .ToList();

                string pluralSuffix = updatedFlags.Count > 1 ? "s" : "";
                UpdatedTab.Header = $"Updated FastFlag{pluralSuffix} ({updatedFlags.Count})";
            }
            else
            {
                TabControl.Items.Remove(UpdatedTab);
            }

            if (TabControl.Items.Count > 0)
                ((TabItem)TabControl.Items[0]).Focus();
        }
    }
}