using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows;

namespace Bloxstrap.PcTweaks
{
    internal static class FirewallRules
    {
        private const string RuleName = "Froststrap - Roblox Firewall Access";

        public static bool ToggleFirewallRule(bool enable)
        {
            if (!IsRunningAsAdmin())
            {
                var result = Frontend.ShowMessageBox(
                    "This feature requires administrator privileges.\n\nRestart Froststrap as administrator?",
                    MessageBoxImage.Warning,
                    MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                    RestartElevated();

                return false;
            }

            try
            {
                if (enable)
                {
                    string? exePath = TryFindRobloxPlayerBeta();
                    if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                        exePath = PromptUserToSelectExe();

                    if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                    {
                        Frontend.ShowMessageBox(
                            "RobloxPlayerBeta.exe was not found or selected. Firewall rule was not added.",
                            MessageBoxImage.Error);
                        return false;
                    }

                    AddFirewallRule("in", exePath);
                    AddFirewallRule("out", exePath);
                }
                else
                {
                    RemoveFirewallRule("in");
                    RemoveFirewallRule("out");
                }

                Frontend.ShowMessageBox(
                    $"Roblox has been {(enable ? "allowed through" : "removed from")} the firewall.\n\nRestart Roblox to apply changes.",
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(
                    $"Failed to {(enable ? "enable" : "remove")} firewall rule:\n\n{ex.Message}",
                    MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private static void AddFirewallRule(string direction, string exePath)
        {
            var directionFlag = direction == "in" ? "in" : "out";
            var ruleName = $"{RuleName} ({directionFlag.ToUpper()})";

            Process.Start(new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"advfirewall firewall add rule name=\"{ruleName}\" dir={directionFlag} action=allow program=\"{exePath}\" enable=yes profile=any",
                UseShellExecute = false,
                CreateNoWindow = true
            })?.WaitForExit();
        }

        private static void RemoveFirewallRule(string direction)
        {
            var directionFlag = direction == "in" ? "in" : "out";
            var ruleName = $"{RuleName} ({directionFlag.ToUpper()})";

            Process.Start(new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"advfirewall firewall delete rule name=\"{ruleName}\" dir={directionFlag}",
                UseShellExecute = false,
                CreateNoWindow = true
            })?.WaitForExit();
        }

        private static string? PromptUserToSelectExe()
        {
            string? exePath = null;

            using (var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "RobloxPlayerBeta (*.exe)|RobloxPlayerBeta.exe",
                Title = "Select RobloxPlayerBeta.exe"
            })
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    exePath = dialog.FileName;
            }

            return exePath;
        }

        private static string? TryFindRobloxPlayerBeta()
        {
            try
            {
                string versionsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Froststrap", "Versions");

                if (!Directory.Exists(versionsPath))
                    return null;

                var versionFolders = Directory.GetDirectories(versionsPath)
                    .Where(d => Path.GetFileName(d).StartsWith("version-", StringComparison.OrdinalIgnoreCase));

                return versionFolders
                    .Select(dir => Path.Combine(dir, "RobloxPlayerBeta.exe"))
                    .Where(File.Exists)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private static bool IsRunningAsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void RestartElevated()
        {
            string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath == null)
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    Verb = "runas"
                });

                Application.Current.Shutdown();
            }
            catch { }
        }
    }
}
