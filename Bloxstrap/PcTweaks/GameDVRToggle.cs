using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using Microsoft.Win32;

namespace Bloxstrap.PcTweaks
{
    internal static class GameDvrToggle
    {
        private static readonly (RegistryKey Hive, string Path, string Name, object Value, RegistryValueKind Kind)[] DisabledSettings =
        {
        (Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 0, RegistryValueKind.DWord),
        (Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0, RegistryValueKind.DWord)
    };

        private static readonly (RegistryKey Hive, string Path, string Name, object Value, RegistryValueKind Kind)[] EnabledSettings =
        {
        (Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 1, RegistryValueKind.DWord),
        (Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 1, RegistryValueKind.DWord)
    };

        public static bool ToggleGameDvr(bool disable)
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
                var settingsToWrite = disable ? DisabledSettings : EnabledSettings;

                foreach (var (hive, path, name, value, kind) in settingsToWrite)
                {
                    using var key = hive.CreateSubKey(path, writable: true);
                    if (key == null)
                        throw new Exception($"Failed to open or create registry key: {path}");

                    key.SetValue(name, value, kind);
                }

                Frontend.ShowMessageBox(
                    $"Game DVR setting has been {(disable ? "turned off" : "restored")}.\n\nPlease restart your PC or sign out to apply changes.",
                    MessageBoxImage.Information,
                    MessageBoxButton.OK);

                return true;
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(
                    $"Failed to update Game DVR setting:\n\n{ex.Message}",
                    MessageBoxImage.Error,
                    MessageBoxButton.OK);
                return false;
            }
        }

        public static bool IsGameDvrDisabled()
        {
            try
            {
                foreach (var (hive, path, name, expectedValue, kind) in DisabledSettings)
                {
                    using var key = hive.OpenSubKey(path);
                    if (key == null)
                        return false;

                    var actualValue = key.GetValue(name);
                    if (actualValue == null)
                        return false;

                    if (kind == RegistryValueKind.DWord)
                    {
                        if (Convert.ToInt32(actualValue) != Convert.ToInt32(expectedValue))
                            return false;
                    }
                    else if (!actualValue.Equals(expectedValue))
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
            try
            {
                string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (exePath == null) return;

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