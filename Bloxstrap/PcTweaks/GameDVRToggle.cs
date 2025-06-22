using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using Microsoft.Win32;

namespace Bloxstrap.PcTweaks
{
    internal static class GameDvrToggle
    {
        public static bool ToggleGameDvr(bool enable)
        {
            if (!IsRunningAsAdmin())
            {
                var res = Frontend.ShowMessageBox(
                    "This feature requires administrator privileges.\n\nRestart Froststrap as administrator?",
                    MessageBoxImage.Warning,
                    MessageBoxButton.YesNo);

                if (res == MessageBoxResult.Yes)
                    RestartElevated();

                return false;
            }

            try
            {
                int dvrValue = enable ? 1 : 0;

                using (var userKey = Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore"))
                {
                    userKey?.SetValue("GameDVR_Enabled", dvrValue, RegistryValueKind.DWord);
                }

                using (var machineKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\GameDVR"))
                {
                    machineKey?.SetValue("AllowGameDVR", dvrValue, RegistryValueKind.DWord);
                }

                Frontend.ShowMessageBox(
                    $"Game DVR has been {(enable ? "enabled" : "disabled")}.\n\nRestart your PC or sign out for this to take full effect.",
                    MessageBoxImage.Information,
                    MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(
                    $"Failed to {(enable ? "enable" : "disable")} Game DVR:\n\n{ex.Message}",
                    MessageBoxImage.Error,
                    MessageBoxButton.OK);

                return false;
            }

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
            string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath == null) return;

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