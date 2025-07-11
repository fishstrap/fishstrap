using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using Microsoft.Win32;

namespace Bloxstrap.PcTweaks
{
    internal static class DisableMitigations
    {
        private static readonly (string Key, string Name, object Value, RegistryValueKind Kind)[] EnableSettings =
        {
            (@"SYSTEM\CurrentControlSet\Control\Session Manager\kernel", "MitigationOptions", HexStringToByteArray("222222222222222222222222222222222222222222222222"), RegistryValueKind.Binary),
            (@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "EnableCfg", 0, RegistryValueKind.DWord),
            (@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "MoveImages", 0, RegistryValueKind.DWord),
            (@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettings", 1, RegistryValueKind.DWord),
            (@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverride", 3, RegistryValueKind.DWord),
            (@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverrideMask", 3, RegistryValueKind.DWord),
            (@"SYSTEM\CurrentControlSet\Control\Session Manager\kernel", "DisableExceptionChainValidation", 1, RegistryValueKind.DWord),
            (@"SYSTEM\CurrentControlSet\Control\Session Manager\kernel", "KernelSEHOPEnabled", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\FVE", "DisableExternalDMAUnderLock", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows\DeviceGuard", "EnableVirtualizationBasedSecurity", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows\DeviceGuard", "HVCIMATRequired", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Internet Explorer\Main", "DEPOff", 1, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows\Explorer", "NoDataExecutionPrevention", 1, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows\System", "DisableHHDEP", 1, RegistryValueKind.DWord),
            (@"SYSTEM\CurrentControlSet\Control\Session Manager", "ProtectionMode", 0, RegistryValueKind.DWord)
        };

        private static readonly (string Key, string Name, object Value, RegistryValueKind Kind)[] DisableSettings =
        {
            (@"SYSTEM\CurrentControlSet\Control\Session Manager\kernel", "MitigationOptions", HexStringToByteArray("000000000000000000000000000000000000000000000000"), RegistryValueKind.Binary),
            (@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "EnableCfg", 1, RegistryValueKind.DWord),
            (@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "MoveImages", 1, RegistryValueKind.DWord),
            (@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettings", 0, RegistryValueKind.DWord),
            (@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverride", 0, RegistryValueKind.DWord),
            (@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverrideMask", 0, RegistryValueKind.DWord),
            (@"SYSTEM\CurrentControlSet\Control\Session Manager\kernel", "DisableExceptionChainValidation", 0, RegistryValueKind.DWord),
            (@"SYSTEM\CurrentControlSet\Control\Session Manager\kernel", "KernelSEHOPEnabled", 1, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\FVE", "DisableExternalDMAUnderLock", 1, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows\DeviceGuard", "EnableVirtualizationBasedSecurity", 1, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows\DeviceGuard", "HVCIMATRequired", 1, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Internet Explorer\Main", "DEPOff", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows\Explorer", "NoDataExecutionPrevention", 0, RegistryValueKind.DWord),
            (@"SOFTWARE\Policies\Microsoft\Windows\System", "DisableHHDEP", 0, RegistryValueKind.DWord),
            (@"SYSTEM\CurrentControlSet\Control\Session Manager", "ProtectionMode", 1, RegistryValueKind.DWord)
        };


        // List of executables to delete MitigationOptions from
        private static readonly string[] ExeList =
        {
            "Acrobat.exe", "AcrobatInfo.exe", "AcroCEF.exe", "AcroRd32.exe", "AcroServicesUpdater.exe",
            "ExtExport.exe", "ie4uinit.exe", "ieinstal.exe", "ielowutil.exe", "ieUnatt.exe", "iexplore.exe",
            "mscorsvw.exe", "msfeedssync.exe", "mshta.exe", "MsSense.exe", "ngen.exe", "ngentask.exe",
            "PresentationHost.exe", "PrintDialog.exe", "PrintIsolationHost.exe", "runtimebroker.exe",
            "splwow64.exe", "spoolsv.exe", "SystemSettings.exe"
        };

        public static bool TogglePolicy(bool enable)
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
                if (enable)
                {
                    foreach (var exe in ExeList)
                    {
                        using var key = Registry.LocalMachine.OpenSubKey(
                            $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\{exe}", true);

                        key?.DeleteValue("MitigationOptions", false);
                    }
                }
                else
                {
                    Frontend.ShowMessageBox(
                        "Disabling this tweak is not supported.",
                        MessageBoxImage.Information,
                        MessageBoxButton.OK);
                    return false;
                }

                // Apply registry settings
                var settings = enable ? EnableSettings : DisableSettings;

                foreach (var (key, name, value, kind) in settings)
                {
                    using var regKey = Registry.LocalMachine.CreateSubKey(key);
                    regKey?.SetValue(name, value, kind);
                }
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(
                    $"Failed to {(enable ? "apply" : "restore")} mitigation settings:\n\n{ex.Message}",
                    MessageBoxImage.Error,
                    MessageBoxButton.OK);
                return false;
            }

            Frontend.ShowMessageBox(
                "Mitigations have been " + (enable ? "disabled" : "restored") +
                ".\nRestart your PC for full effect.",
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

        private static byte[] HexStringToByteArray(string hex)
        {
            int length = hex.Length;
            byte[] bytes = new byte[length / 2];
            for (int i = 0; i < length; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public static bool AreMitigationsDisabled()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\kernel");
                if (key == null)
                    return false;

                var mitigationValue = key.GetValue("MitigationOptions") as byte[];
                if (mitigationValue == null)
                    return false;

                byte[] disabledBytes = HexStringToByteArray("222222222222222222222222222222222222222222222222");
                return mitigationValue.SequenceEqual(disabledBytes);
            }
            catch
            {
                return false;
            }
        }
    }
}