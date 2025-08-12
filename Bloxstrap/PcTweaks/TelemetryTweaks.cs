using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using Microsoft.Win32;

namespace Bloxstrap.PcTweaks
{
    internal static class TelemetryTweaks
    {
        private static readonly (string Key, string Name, object Value, RegistryValueKind Kind)[] TelemetrySettings =
        {
            (@"SOFTWARE\Policies\Microsoft\AppV\CEIP", "CEIPEnable", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Internet Explorer\SQM", "DisableCustomerImprovementProgram", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Messenger\Client", "CEIP", 2, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\MSDeploy\3", "EnableTelemetry", 1, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\SQMClient\Windows", "CEIPEnable", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows\AppCompat", "AITEnable", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows\AppCompat", "DisableInventory", 1, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "Disabled", 1, RegistryValueKind.DWord),
        };

        public static bool ToggleTelemetrySettings(bool disable)
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
                foreach (var (key, name, value, kind) in TelemetrySettings)
                {
                    using var regKey = Registry.LocalMachine.CreateSubKey(key);
                    if (regKey != null)
                    {
                        // If disabling, set as per array, else revert to default values here (example defaults used)
                        object valToSet = disable ? value : GetDefaultValue(key, name);
                        regKey.SetValue(name, valToSet, kind);
                    }
                }

                Frontend.ShowMessageBox(
                    $"Telemetry and related settings have been {(disable ? "disabled" : "restored")}.\n\nPlease restart your PC for full effect.",
                    MessageBoxImage.Information,
                    MessageBoxButton.OK);

                return true;
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(
                    $"Failed to {(disable ? "disable" : "restore")} telemetry settings:\n\n{ex.Message}",
                    MessageBoxImage.Error,
                    MessageBoxButton.OK);

                return false;
            }
        }

        // Provide some example defaults for enabling telemetry; adjust as needed
        private static object GetDefaultValue(string key, string name)
        {
            return (key, name) switch
            {
                (@"SOFTWARE\Policies\Microsoft\AppV\CEIP", "CEIPEnable") => 1,
                (@"SOFTWARE\Policies\Microsoft\Internet Explorer\SQM", "DisableCustomerImprovementProgram") => 1,
                (@"SOFTWARE\Policies\Microsoft\Messenger\Client", "CEIP") => 0,
                (@"SOFTWARE\Policies\Microsoft\MSDeploy\3", "EnableTelemetry") => 0,
                (@"SOFTWARE\Policies\Microsoft\SQMClient\Windows", "CEIPEnable") => 1,
                (@"SOFTWARE\Policies\Microsoft\Windows\AppCompat", "AITEnable") => 1,
                (@"SOFTWARE\Policies\Microsoft\Windows\AppCompat", "DisableInventory") => 0,
                (@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry") => 1,
                (@"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "Disabled") => 0,
                _ => 0,
            };
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
                    Arguments = "-menu",
                    UseShellExecute = true,
                    Verb = "runas"
                });

                Application.Current.Shutdown();
            }
            catch { }
        }

        public static bool IsTelemetryDisabled()
        {
            try
            {
                foreach (var (key, name, expectedValue, kind) in TelemetrySettings)
                {
                    using var regKey = Registry.LocalMachine.OpenSubKey(key);
                    if (regKey == null)
                        return false;

                    var actualValue = regKey.GetValue(name);
                    if (actualValue == null)
                        return false;

                    if (kind == RegistryValueKind.DWord)
                    {
                        if (Convert.ToInt32(actualValue) != Convert.ToInt32(expectedValue))
                            return false;
                    }
                    else
                    {
                        if (!actualValue.Equals(expectedValue))
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
    }
}
