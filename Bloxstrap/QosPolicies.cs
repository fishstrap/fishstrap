using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows;

namespace Bloxstrap.Integrations
{
    internal static class QosPolicies
    {
        private const string KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\QoS\RobloxWiFiBoost";

        public static bool TogglePolicy(bool enable)
        {
            if (!IsRunningAsAdmin())
            {
                var res = System.Windows.Forms.MessageBox.Show(
                    "This feature requires administrator privileges.\n\nRestart Froststrap as administrator?",
                    "Administrator Required",
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Warning);

                if (res == System.Windows.Forms.DialogResult.Yes)
                {
                    RestartElevated();
                }

                return false;
            }

            if (enable)
            {
                string? exePath = TryFindRobloxPlayerBeta();

                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                    exePath = PromptUserToSelectExe();

                if (string.IsNullOrEmpty(exePath))
                    return false;

                try
                {
                    using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(KeyPath);
                    key?.SetValue("ApplicationName", "RobloxPlayerBeta.exe", Microsoft.Win32.RegistryValueKind.String);
                    key?.SetValue("PolicyName", "RobloxNetworkBoost", Microsoft.Win32.RegistryValueKind.String);
                    key?.SetValue("Version", 1, Microsoft.Win32.RegistryValueKind.DWord);
                    key?.SetValue("DSCPValue", 46, Microsoft.Win32.RegistryValueKind.DWord);
                    key?.SetValue("ThrottleRate", unchecked((int)0xFFFFFFFF), Microsoft.Win32.RegistryValueKind.DWord); // -1
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                try
                {
                    Microsoft.Win32.Registry.LocalMachine.DeleteSubKeyTree(KeyPath, throwOnMissingSubKey: false);
                }
                catch
                {
                    return false;
                }
            }

            System.Windows.Forms.MessageBox.Show(
                "QoS policy updated. Please restart your PC for this to take full effect.",
                "Restart Recommended",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information
            );

            return true;
        }

        private static bool IsRunningAsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void RestartElevated()
        {
            string? exeName = Process.GetCurrentProcess().MainModule?.FileName;
            if (exeName == null)
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exeName,
                    UseShellExecute = true,
                    Verb = "runas"
                });

                Application.Current.Shutdown();
            }
            catch { }
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
    }
}