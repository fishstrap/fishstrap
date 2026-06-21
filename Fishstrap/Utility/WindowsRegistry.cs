using Microsoft.Win32;
using System.CodeDom;

namespace Fishstrap.Utility
{
    static class WindowsRegistry
    {
        private const string RobloxPlaceKey = "Roblox.Place";
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string StartupApprovedRunKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
        
        public static readonly List<RegistryKey> Roots = new() { Registry.CurrentUser, Registry.LocalMachine };

        public static void RegisterProtocol(string key, string name, string handler, string handlerParam = "%1")
        {
            string handlerArgs = $"\"{handler}\" {handlerParam}";

            using var uriKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{key}");
            using var uriIconKey = uriKey.CreateSubKey("DefaultIcon");
            using var uriCommandKey = uriKey.CreateSubKey(@"shell\open\command");

            if (uriKey.GetValue("") is null)
            {
                uriKey.SetValueSafe("", $"URL: {name} Protocol");
                uriKey.SetValueSafe("URL Protocol", "");
            }

            if (uriCommandKey.GetValue("") as string != handlerArgs)
            {
                uriIconKey.SetValueSafe("", handler);
                uriCommandKey.SetValueSafe("", handlerArgs);
            }
        }

        /// <summary>
        /// Registers Roblox Player protocols for Fishstrap
        /// </summary>
        public static void RegisterPlayer() => RegisterPlayer(Paths.Application, "-player \"%1\"");

        public static void RegisterPlayer(string handler, string handlerParam)
        {
            RegisterProtocol("roblox", "Roblox", handler, handlerParam);
            RegisterProtocol("roblox-player", "Roblox", handler, handlerParam);
        }

        /// <summary>
        /// Registers all Roblox Studio classes for Fishstrap
        /// </summary>
        public static void RegisterStudio()
        {
            RegisterStudioProtocol(Paths.Application, "-studio \"%1\"");
            RegisterStudioFileClass(Paths.Application, "-studio \"%1\"");
            RegisterStudioFileTypes();
        }

        /// <summary>
        /// Registers roblox-studio and roblox-studio-auth protocols
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="handlerParam"></param>
        public static void RegisterStudioProtocol(string handler, string handlerParam)
        {
            RegisterProtocol("roblox-studio", "Roblox", handler, handlerParam);
            RegisterProtocol("roblox-studio-auth", "Roblox", handler, handlerParam);
        }

        /// <summary>
        /// Registers file associations for Roblox.Place class
        /// </summary>
        public static void RegisterStudioFileTypes()
        {
            RegisterStudioFileType(".rbxl");
            RegisterStudioFileType(".rbxlx");
        }

        /// <summary>
        /// Registers Roblox.Place class
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="handlerParam"></param>
        public static void RegisterStudioFileClass(string handler, string handlerParam)
        {
            const string keyValue = "Roblox Place";
            string handlerArgs = $"\"{handler}\" {handlerParam}";
            string iconValue = $"{handler},0";

            using RegistryKey uriKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + RobloxPlaceKey);
            using RegistryKey uriIconKey = uriKey.CreateSubKey("DefaultIcon");
            using RegistryKey uriOpenKey = uriKey.CreateSubKey(@"shell\Open");
            using RegistryKey uriCommandKey = uriOpenKey.CreateSubKey(@"command");

            if (uriKey.GetValue("") as string != keyValue)
                uriKey.SetValueSafe("", keyValue);

            if (uriCommandKey.GetValue("") as string != handlerArgs)
                uriCommandKey.SetValueSafe("", handlerArgs);

            if (uriOpenKey.GetValue("") as string != "Open")
                uriOpenKey.SetValueSafe("", "Open");

            if (uriIconKey.GetValue("") as string != iconValue)
                uriIconKey.SetValueSafe("", iconValue);
        }

        public static void RegisterStudioFileType(string key)
        {
            using RegistryKey uriKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{key}");
            uriKey.CreateSubKey(RobloxPlaceKey + @"\ShellNew");

            if (uriKey.GetValue("") as string != RobloxPlaceKey)
                uriKey.SetValueSafe("", RobloxPlaceKey);
        }

        public static void RegisterApis()
        {
            static void Register()
            {
                using var apisKey = Registry.CurrentUser.CreateSubKey(App.ApisKey);
                apisKey.SetValueSafe("ApplicationPath", Paths.Application);
                apisKey.SetValueSafe("InstallationPath", Paths.Base);
            };

            var currentApis = Registry.CurrentUser.OpenSubKey(App.ApisKey,false);

            if (currentApis == null)
            {
                Register();
            };
            currentApis?.Dispose();
        }

        private static bool IsRobloxStartupEntry(string valueName, string value)
        {
            string valueLower = value.ToLowerInvariant();
            string nameLower = valueName.ToLowerInvariant();

            return
                nameLower.Contains("roblox game client") ||
                nameLower.Contains("robloxplayerbeta") ||
                valueLower.Contains("robloxplayerbeta.exe") ||
                valueLower.Contains("robloxplayerlauncher.exe") ||
                (nameLower.Contains("roblox") && valueLower.Contains("roblox"));
        }

        public static bool IsRobloxAutoStartDisabled() => QueryRobloxAutoStartDisabled(currentUserOnly: true);

        private static bool QueryRobloxAutoStartDisabled(bool currentUserOnly)
        {
            const string LOG_IDENT = "WindowsRegistry::QueryRobloxAutoStartDisabled";
            bool foundStartupEntry = false;
            bool foundEnabledStartupEntry = false;

            IEnumerable<RegistryKey> roots = currentUserOnly ? new[] { Registry.CurrentUser } : Roots;

            foreach (RegistryKey root in roots)
            {
                try
                {
                    using RegistryKey? runKey = root.OpenSubKey(RunKey);
                    using RegistryKey? approvedRunKey = root.OpenSubKey(StartupApprovedRunKey);

                    InspectRunStartupState(runKey, approvedRunKey, ref foundStartupEntry, ref foundEnabledStartupEntry);
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Failed to inspect startup entries in {root.Name}");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }
            }

            return foundStartupEntry && !foundEnabledStartupEntry;
        }

        private static void InspectRunStartupState(RegistryKey? runKey, RegistryKey? approvedKey, ref bool foundStartupEntry, ref bool foundEnabledStartupEntry)
        {
            if (runKey is null)
                return;

            foreach (string valueName in runKey.GetValueNames())
            {
                string? value = runKey.GetValue(valueName)?.ToString();

                if (value is null || !IsRobloxStartupEntry(valueName, value))
                    continue;

                foundStartupEntry = true;

                if (approvedKey?.GetValue(valueName) is byte[] startupApprovedValue && startupApprovedValue.Length > 0 && startupApprovedValue[0] == 0x03)
                    continue;

                foundEnabledStartupEntry = true;
            }
        }

        public static void DisableRobloxAutoStart(bool promptForElevation = true)
        {
            SetRobloxAutoStartDisabledAsync(true, promptForElevation).GetAwaiter().GetResult();
        }

        public static void EnableRobloxAutoStart(bool promptForElevation = true)
        {
            SetRobloxAutoStartDisabledAsync(false, promptForElevation).GetAwaiter().GetResult();
        }

        public static Task DisableRobloxAutoStartAsync(bool promptForElevation = true) => SetRobloxAutoStartDisabledAsync(true, promptForElevation);

        public static Task EnableRobloxAutoStartAsync(bool promptForElevation = true) => SetRobloxAutoStartDisabledAsync(false, promptForElevation);

        private static async Task SetRobloxAutoStartDisabledAsync(bool disabled, bool promptForElevation)
        {
            byte[] startupValue = disabled
                ? new byte[] { 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }
                : new byte[] { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            bool[] elevationRequired = await Task.WhenAll(
                Roots.Select(root => Task.Run(() => SetRobloxAutoStartDisabledForRoot(root, startupValue, disabled)))
            ).ConfigureAwait(false);

            if (promptForElevation && elevationRequired.Any(x => x))
                RequestRobloxAutoStartElevation(disabled);
        }

        private static bool SetRobloxAutoStartDisabledForRoot(RegistryKey root, byte[] startupValue, bool disabled)
        {
            const string LOG_IDENT = "WindowsRegistry::SetRobloxAutoStartDisabledForRoot";

            try
            {
                using RegistryKey? runKey = root.OpenSubKey(RunKey);

                SetRunStartupState(root, runKey, StartupApprovedRunKey, startupValue, disabled);
            }
            catch (UnauthorizedAccessException ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Administrator permission is required to update startup entries in {root.Name}");
                App.Logger.WriteException(LOG_IDENT, ex);
                return true;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to inspect startup entries in {root.Name}");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            return false;
        }

        private static void SetRunStartupState(RegistryKey root, RegistryKey? runKey, string approvedKeyName, byte[] startupValue, bool disabled)
        {
            const string LOG_IDENT = "WindowsRegistry::SetRunStartupState";

            if (runKey is null)
                return;

            foreach (string valueName in runKey.GetValueNames())
            {
                string? value = runKey.GetValue(valueName)?.ToString();

                if (value is null || !IsRobloxStartupEntry(valueName, value))
                    continue;

                App.Logger.WriteLine(LOG_IDENT, $"{(disabled ? "Disabling" : "Enabling")} Roblox startup entry '{valueName}' in {root.Name}");
                using RegistryKey? startupApprovedKey = root.CreateSubKey(approvedKeyName);

                if (startupApprovedKey is null)
                    continue;

                startupApprovedKey.SetValue(valueName, startupValue, RegistryValueKind.Binary);
            }
        }

        private static void RequestRobloxAutoStartElevation(bool disabled)
        {
            const string LOG_IDENT = "WindowsRegistry::RequestRobloxAutoStartElevation";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Paths.Process,
                    Arguments = disabled ? "-disablerobloxautostart -quiet" : "-enablerobloxautostart -quiet",
                    Verb = "runas",
                    UseShellExecute = true
                });
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                App.Logger.WriteLine(LOG_IDENT, "User cancelled the elevation prompt");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to launch elevated startup disable task");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        public static void RegisterClientLocation(bool isStudio, string? clientPath)
        {
            string keyName = isStudio ? "StudioPath" : "PlayerPath";
            clientPath ??= "";

            using var apisKey = Registry.CurrentUser.CreateSubKey(App.ApisKey);
            apisKey.SetValueSafe(keyName, clientPath);
        }

        public static void Unregister(string key)
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{key}");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("Protocol::Unregister", $"Failed to unregister {key}: {ex}");
            }
        }
    }
}
