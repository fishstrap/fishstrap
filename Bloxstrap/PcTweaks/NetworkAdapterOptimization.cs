using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using Microsoft.Win32;

namespace Bloxstrap.PcTweaks
{
    internal static class NetworkAdapterOptimization
    {
        public static bool ToggleNetworkOptimization(bool enable)
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
                    using (var interfacesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", writable: true))
                    {
                        if (interfacesKey != null)
                        {
                            foreach (var subKeyName in interfacesKey.GetSubKeyNames())
                            {
                                using var subKey = interfacesKey.OpenSubKey(subKeyName, writable: true);
                                subKey?.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                                subKey?.SetValue("TCPNoDelay", 1, RegistryValueKind.DWord);
                                subKey?.SetValue("TcpDelAckTicks", 0, RegistryValueKind.DWord);
                            }
                        }
                    }

                    using (var tcpipParams = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", writable: true))
                    {
                        tcpipParams?.SetValue("Tcp1323Opts", 1, RegistryValueKind.DWord);
                        tcpipParams?.SetValue("DefaultTTL", 64, RegistryValueKind.DWord);
                        tcpipParams?.SetValue("EnableTCPChimney", 0, RegistryValueKind.DWord);
                        tcpipParams?.SetValue("EnableRSS", 1, RegistryValueKind.DWord);
                        tcpipParams?.SetValue("EnableTCPA", 0, RegistryValueKind.DWord);
                        tcpipParams?.SetValue("DisableTaskOffload", 1, RegistryValueKind.DWord);
                    }

                    RunPowerCfg("-h off");
                    SetMtuForAllAdapters(1500);
                }
                else
                {
                    using (var interfacesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", writable: true))
                    {
                        if (interfacesKey != null)
                        {
                            foreach (var subKeyName in interfacesKey.GetSubKeyNames())
                            {
                                using var subKey = interfacesKey.OpenSubKey(subKeyName, writable: true);
                                if (subKey == null) continue;

                                subKey.SetValue("TcpAckFrequency", 2, RegistryValueKind.DWord);
                                subKey.SetValue("TCPNoDelay", 0, RegistryValueKind.DWord);
                                subKey.SetValue("TcpDelAckTicks", 2, RegistryValueKind.DWord);
                            }
                        }
                    }

                    using (var tcpipParams = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", writable: true))
                    {
                        if (tcpipParams == null) return false;

                        tcpipParams.SetValue("Tcp1323Opts", 0, RegistryValueKind.DWord);
                        tcpipParams.SetValue("DefaultTTL", 128, RegistryValueKind.DWord);
                        tcpipParams.SetValue("EnableTCPChimney", 1, RegistryValueKind.DWord);
                        tcpipParams.SetValue("EnableRSS", 0, RegistryValueKind.DWord);
                        tcpipParams.SetValue("EnableTCPA", 1, RegistryValueKind.DWord);
                        tcpipParams.SetValue("DisableTaskOffload", 0, RegistryValueKind.DWord);
                    }

                    RunPowerCfg("-h on");
                }

                Frontend.ShowMessageBox(
                    $"Network adapter optimization has been {(enable ? "enabled" : "disabled")}.\n\nRestart your PC for changes to fully take effect.",
                    MessageBoxImage.Information,
                    MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(
                    $"Failed to {(enable ? "enable" : "disable")} network optimization:\n\n{ex.Message}",
                    MessageBoxImage.Error,
                    MessageBoxButton.OK);

                return false;
            }

            return true;
        }

        private static void RunPowerCfg(string args)
        {
            try
            {
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        Verb = "runas"
                    }
                };

                proc.Start();
                proc.WaitForExit();
            }
            catch
            {
                // silently ignore errors
            }
        }

        private static void SetMtuForAllAdapters(int mtu)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "wmic",
                        Arguments = @"nic get Name /value",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.StartsWith("Name=", StringComparison.OrdinalIgnoreCase))
                    {
                        string adapterName = line.Substring(5);
                        SetMtu(adapterName, mtu);
                    }
                }
            }
            catch
            {
                // ignore errors
            }
        }

        private static void SetMtu(string adapterName, int mtu)
        {
            try
            {
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"interface ipv4 set subinterface \"{adapterName}\" mtu={mtu} store=persistent",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                proc.Start();
                proc.WaitForExit();
            }
            catch
            {
                // ignore errors
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
                    Arguments = "-menu",
                    UseShellExecute = true,
                    Verb = "runas"
                });

                Application.Current.Shutdown();
            }
            catch { }
        }

        public static bool IsNetworkOptimizationEnabled()
        {
            try
            {
                using var interfacesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", writable: false);
                if (interfacesKey == null)
                    return false;

                foreach (var subKeyName in interfacesKey.GetSubKeyNames())
                {
                    using var subKey = interfacesKey.OpenSubKey(subKeyName, writable: false);
                    if (subKey == null)
                        continue;

                    var tcpAckFrequency = subKey.GetValue("TcpAckFrequency");
                    var tcpNoDelay = subKey.GetValue("TCPNoDelay");
                    var tcpDelAckTicks = subKey.GetValue("TcpDelAckTicks");

                    // Check if values match the "enabled" state
                    if (Convert.ToInt32(tcpAckFrequency) != 1 ||
                        Convert.ToInt32(tcpNoDelay) != 1 ||
                        Convert.ToInt32(tcpDelAckTicks) != 0)
                    {
                        return false;
                    }
                }

                using var tcpipParams = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", writable: false);
                if (tcpipParams == null)
                    return false;

                if (Convert.ToInt32(tcpipParams.GetValue("Tcp1323Opts", -1)) != 1) return false;
                if (Convert.ToInt32(tcpipParams.GetValue("DefaultTTL", -1)) != 64) return false;
                if (Convert.ToInt32(tcpipParams.GetValue("EnableTCPChimney", -1)) != 0) return false;
                if (Convert.ToInt32(tcpipParams.GetValue("EnableRSS", -1)) != 1) return false;
                if (Convert.ToInt32(tcpipParams.GetValue("EnableTCPA", -1)) != 0) return false;
                if (Convert.ToInt32(tcpipParams.GetValue("DisableTaskOffload", -1)) != 1) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}