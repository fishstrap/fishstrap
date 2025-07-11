using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using Microsoft.Win32;

namespace Bloxstrap.PcTweaks
{
    internal static class DisableDefenderSmartScreen
    {
        private static readonly (string Key, string Name, object Value, RegistryValueKind Kind)[] EnableSettings =
        {
            (@"SOFTWARE\Policies\Microsoft\Windows\System", "EnableSmartScreen", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\MicrosoftEdge\PhishingFilter", "EnabledV9", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableRoutinelyTakingAction", 1, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows Defender", "ServiceKeepAlive", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", "DisableRealtimeMonitoring", 1, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows Defender\Reporting", "DisableEnhancedNotifications", 1, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows Defender\SmartScreen", "ConfigureAppInstallControlEnabled", 0, RegistryValueKind.DWord),

            (@"SOFTWARE\Policies\Microsoft\Windows Defender\Threats", "Threats_ThreatSeverityDefaultAction", 1, RegistryValueKind.DWord),

            // These are REG_SZ strings, so store as strings, even if numeric
            (@"SOFTWARE\Policies\Microsoft\Windows Defender\Threats\ThreatSeverityDefaultAction", "1", "6", RegistryValueKind.String),
            (@"SOFTWARE\Policies\Microsoft\Windows Defender\Threats\ThreatSeverityDefaultAction", "2", "6", RegistryValueKind.String),
            (@"SOFTWARE\Policies\Microsoft\Windows Defender\Threats\ThreatSeverityDefaultAction", "4", "6", RegistryValueKind.String),
            (@"SOFTWARE\Policies\Microsoft\Windows Defender\Threats\ThreatSeverityDefaultAction", "5", "6", RegistryValueKind.String),

            (@"SOFTWARE\Policies\Microsoft\Windows Defender\UX Configuration", "Notification_Suppress", 1, RegistryValueKind.DWord),
            (@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\MsMpEng.exe\PerfOptions", "CpuPriorityClass", 1, RegistryValueKind.DWord),
            (@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\MsMpEngCP.exe\PerfOptions", "CpuPriorityClass", 1, RegistryValueKind.DWord)
        };

        // Defaults - enabling Defender/SmartScreen again
        private static readonly (string Key, string Name, object Value, RegistryValueKind Kind)[] DisableSettings =
        {
            (@"SOFTWARE\Policies\Microsoft\Windows\System", "EnableSmartScreen", 1, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\MicrosoftEdge\PhishingFilter", "EnabledV9", 1, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableRoutinelyTakingAction", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows Defender", "ServiceKeepAlive", 1, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", "DisableRealtimeMonitoring", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows Defender\Reporting", "DisableEnhancedNotifications", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows Defender\SmartScreen", "ConfigureAppInstallControlEnabled", 1, RegistryValueKind.DWord),

            (@"SOFTWARE\Policies\Microsoft\Windows Defender\Threats", "Threats_ThreatSeverityDefaultAction", 0, RegistryValueKind.DWord),

            (@"SOFTWARE\Policies\Microsoft\Windows Defender\Threats\ThreatSeverityDefaultAction", "1", "0", RegistryValueKind.String),
            (@"SOFTWARE\Policies\Microsoft\Windows Defender\Threats\ThreatSeverityDefaultAction", "2", "0", RegistryValueKind.String),
            (@"SOFTWARE\Policies\Microsoft\Windows Defender\Threats\ThreatSeverityDefaultAction", "4", "0", RegistryValueKind.String),
            (@"SOFTWARE\Policies\Microsoft\Windows Defender\Threats\ThreatSeverityDefaultAction", "5", "0", RegistryValueKind.String),

            (@"SOFTWARE\Policies\Microsoft\Windows Defender\UX Configuration", "Notification_Suppress", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\MsMpEng.exe\PerfOptions", "CpuPriorityClass", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\MsMpEngCP.exe\PerfOptions", "CpuPriorityClass", 0, RegistryValueKind.DWord)
        };

        public static bool ToggleDisableDefenderSmartScreen(bool enable)
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
                var settingsToWrite = enable ? EnableSettings : DisableSettings;

                foreach (var (key, name, value, kind) in settingsToWrite)
                {
                    using var regKey = Registry.LocalMachine.CreateSubKey(key);
                    regKey?.SetValue(name, value, kind);
                }

                Frontend.ShowMessageBox(
                    (enable ? "Defender and SmartScreen features have been disabled" : "Defender and SmartScreen features have been restored") +
                    ".\nRestart your PC for the changes to take full effect.",
                    MessageBoxImage.Information,
                    MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(
                    $"Failed to {(enable ? "disable" : "restore")} Defender/SmartScreen settings:\n\n{ex.Message}",
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

        public static bool IsDisabled()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System");
                if (key == null)
                    return false;

                var val = key.GetValue("EnableSmartScreen");
                if (val is int intVal)
                    return intVal == 0;

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}