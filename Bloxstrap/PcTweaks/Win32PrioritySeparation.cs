using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using Microsoft.Win32;

namespace Bloxstrap.PcTweaks
{
    internal static class Win32PrioritySeparation
    {
        private const string RegistryKey = @"SYSTEM\CurrentControlSet\Control\PriorityControl";
        private const string RegistryValueName = "Win32PrioritySeparation";

        public static bool ApplyTweak()
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
                string cpuManufacturer = GetCpuManufacturer()?.Trim() ?? "";
                int priorityValue = 36; // default for Intel

                if (cpuManufacturer.IndexOf("amd", StringComparison.OrdinalIgnoreCase) >= 0)
                    priorityValue = 26;

                using var regKey = Registry.LocalMachine.CreateSubKey(RegistryKey);
                regKey?.SetValue(RegistryValueName, priorityValue, RegistryValueKind.DWord);

                Frontend.ShowMessageBox(
                    $"Detected CPU Manufacturer: {cpuManufacturer}\n" +
                    $"Win32PrioritySeparation set to {priorityValue}.\n\nRestart your PC for full effect.",
                    MessageBoxImage.Information,
                    MessageBoxButton.OK);

                return true;
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(
                    $"Failed to apply Win32PrioritySeparation tweak:\n\n{ex.Message}",
                    MessageBoxImage.Error,
                    MessageBoxButton.OK);
                return false;
            }
        }

        private static string? GetCpuManufacturer()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = "-NoProfile -Command \"(Get-CimInstance Win32_Processor).Manufacturer\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                    return null;

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return output.Trim();
            }
            catch
            {
                return null;
            }
        }

        private static bool IsRunningAsAdmin()
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
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

        public static bool IsEnabled()
        {
            try
            {
                using var regKey = Registry.LocalMachine.OpenSubKey(RegistryKey);
                if (regKey == null)
                    return false;

                var value = regKey.GetValue(RegistryValueName);
                if (value == null)
                    return false;

                int intValue = Convert.ToInt32(value);
                string cpuManufacturer = GetCpuManufacturer()?.Trim() ?? "";

                // Intel expected value is 36, AMD expected value is 26
                if (cpuManufacturer.IndexOf("amd", StringComparison.OrdinalIgnoreCase) >= 0)
                    return intValue == 26;
                else
                    return intValue == 36;
            }
            catch
            {
                return false;
            }
        }

    }
}