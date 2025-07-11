using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows;

namespace Bloxstrap.PcTweaks
{
    internal static class QosPolicies
    {
        private const string KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\QoS\RobloxWiFiBoost";

        public static bool TogglePolicy(bool enable)
        {
            if (!IsRunningAsAdmin())
            {
                var res = Frontend.ShowMessageBox(
                    "This feature requires administrator privileges.\n\nRestart Froststrap as administrator?",
                    MessageBoxImage.Warning,
                    MessageBoxButton.YesNo);

                if (res == MessageBoxResult.Yes)
                {
                    RestartElevated();
                }

                return false;
            }

            if (enable)
            {
                string? exePath = TryFindRobloxPlayerBeta();

                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    Frontend.ShowMessageBox(
                        "Roblox is not installed. Please launch Roblox through Froststrap to use this feature.",
                        MessageBoxImage.Error,
                        MessageBoxButton.OK
                    );
                    return false;
                }

                try
                {
                    using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(KeyPath);
                    key?.SetValue("ApplicationName", "RobloxPlayerBeta.exe", Microsoft.Win32.RegistryValueKind.String);
                    key?.SetValue("PolicyName", "RobloxNetworkBoost", Microsoft.Win32.RegistryValueKind.String);
                    key?.SetValue("Version", 1, Microsoft.Win32.RegistryValueKind.DWord);
                    key?.SetValue("DSCPValue", 46, Microsoft.Win32.RegistryValueKind.DWord);
                    key?.SetValue("ThrottleRate", unchecked((int)0xFFFFFFFF), Microsoft.Win32.RegistryValueKind.DWord);
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

            Frontend.ShowMessageBox(
                "QoS policy updated. Please restart your PC for this to take full effect.",
                MessageBoxImage.Information,
                MessageBoxButton.OK);

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

        public static bool IsPolicyEnabled()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(KeyPath);
                if (key == null)
                    return false;

                var appName = key.GetValue("ApplicationName") as string;
                var policyName = key.GetValue("PolicyName") as string;
                var version = key.GetValue("Version");
                var dscp = key.GetValue("DSCPValue");

                return appName == "RobloxPlayerBeta.exe" &&
                       policyName == "RobloxNetworkBoost" &&
                       Convert.ToInt32(version) == 1 &&
                       Convert.ToInt32(dscp) == 46;
            }
            catch
            {
                return false;
            }
        }
    }
}