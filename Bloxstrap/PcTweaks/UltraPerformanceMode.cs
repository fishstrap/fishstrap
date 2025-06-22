using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using Microsoft.Win32;

namespace Bloxstrap.PcTweaks
{
    internal static class UltraPerformanceMode
    {
        private static readonly (string Key, string Name, object Value, RegistryValueKind Kind)[] PerformanceSettings =
        {
            // Ultra Performance Mode values (enable)
            (@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "GPU Priority", 8, RegistryValueKind.DWord),
            (@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Priority", 6, RegistryValueKind.DWord),
            (@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Scheduling Category", "High", RegistryValueKind.String),
            (@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "SFIO Priority", "High", RegistryValueKind.String),
            (@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Background Only", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Clock Rate", 10000, RegistryValueKind.DWord),
            (@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Win32PrioritySeparation", 38, RegistryValueKind.DWord),
            (@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 1, RegistryValueKind.DWord)
        };

        private static readonly (string Key, string Name, object Value, RegistryValueKind Kind)[] DefaultSettings =
        {
            // Default Windows values (disable)
            (@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "GPU Priority", 2, RegistryValueKind.DWord),
            (@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Priority", 2, RegistryValueKind.DWord),
            (@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Scheduling Category", "Normal", RegistryValueKind.String),
            (@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "SFIO Priority", "Normal", RegistryValueKind.String),
            (@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Background Only", 1, RegistryValueKind.DWord),
            (@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Clock Rate", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Win32PrioritySeparation", 26, RegistryValueKind.DWord),
            (@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 20, RegistryValueKind.DWord),
            (@"SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 0, RegistryValueKind.DWord)
        };

        public static bool TogglePerformanceMode(bool enable)
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
                var settingsToWrite = enable ? PerformanceSettings : DefaultSettings;

                foreach (var (key, name, value, kind) in settingsToWrite)
                {
                    using var regKey = Registry.LocalMachine.CreateSubKey(key);
                    regKey?.SetValue(name, value, kind);
                }

                Frontend.ShowMessageBox(
                    "Ultra Performance Mode has been " + (enable ? "enabled" : "disabled") +
                    ".\n\nRestart your PC for this to take full effect.",
                    MessageBoxImage.Information,
                    MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(
                    $"Failed to {(enable ? "apply" : "restore default")} performance settings:\n\n{ex.Message}",
                    MessageBoxImage.Error,
                    MessageBoxButton.OK);

                return false;
            }

            return true;
        }

        public static bool IsUltraPerformanceModeEnabled()
        {
            try
            {
                foreach (var (key, name, value, kind) in PerformanceSettings)
                {
                    using var regKey = Registry.LocalMachine.OpenSubKey(key);
                    if (regKey == null)
                        return false;

                    var regValue = regKey.GetValue(name);
                    if (regValue == null)
                        return false;

                    if (kind == RegistryValueKind.DWord)
                    {
                        if (Convert.ToInt32(regValue) != Convert.ToInt32(value))
                            return false;
                    }
                    else if (!regValue.Equals(value))
                    {
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
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