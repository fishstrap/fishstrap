using System.IO;
using System.Windows;
using System.Xml.Linq;
using Bloxstrap.AppData;
using Microsoft.Win32;

namespace Bloxstrap
{
    internal class Installer
    {
        /// <summary>
        /// Should this version automatically open the release notes page?
        /// Recommended for major updates only.
        /// </summary>
        private const bool OpenReleaseNotes = true;

        private static string DesktopShortcut => Path.Combine(Paths.Desktop, $"{App.ProjectName}.lnk");

        private static string StartMenuShortcut => Path.Combine(Paths.WindowsStartMenu, $"{App.ProjectName}.lnk");

        public string BloxstrapInstallDirectory = Path.Combine(Paths.LocalAppData, "Bloxstrap"); // default directory for bloxstrap
                                                                                                 // TODO dynamically fetch from uninstall/player registry keys
        public string InstallLocation = Path.Combine(Paths.LocalAppData, App.ProjectName);

        public bool ExistingDataPresent => File.Exists(Path.Combine(InstallLocation, "Settings.json"));

        public bool CreateDesktopShortcuts = true;

        public bool CreateStartMenuShortcuts = true;

        public bool ImportSettings = 
            Directory.Exists(Path.Combine(Paths.LocalAppData, "Bloxstrap")) ||
            Directory.Exists(Path.Combine(Paths.LocalAppData, "Voidstrap")) ||
            Directory.Exists(Path.Combine(Paths.LocalAppData, "Fishstrap"));

        public bool IsImplicitInstall = false;

        public string InstallLocationError { get; set; } = "";

        public ImportSettingsFrom ImportSource { get; set; } = ImportSettingsFrom.Bloxstrap;

        // anything we want copied should be put in here
        // root directory only
        public string[] FilesForImporting = {
            "CustomThemes", // from feature/custom-bootstrappers
            "Modifications",
            "Settings.json",
            "Mods" // why tf does voidstrap have diffrent names for stuff bro
        };

        public void DoInstall()
        {
            const string LOG_IDENT = "Installer::DoInstall";

            App.Logger.WriteLine(LOG_IDENT, "Beginning installation");

            // should've been created earlier from the write test anyway
            Directory.CreateDirectory(InstallLocation);

            Paths.Initialize(InstallLocation);

            if (!IsImplicitInstall)
            {
                Filesystem.AssertReadOnly(Paths.Application);

                try
                {
                    File.Copy(Paths.Process, Paths.Application, true);
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Could not overwrite executable");
                    App.Logger.WriteException(LOG_IDENT, ex);

                    Frontend.ShowMessageBox(Strings.Installer_Install_CannotOverwrite, MessageBoxImage.Error);
                    App.Terminate(ErrorCode.ERROR_INSTALL_FAILURE);
                }
            }

            using (var uninstallKey = Registry.CurrentUser.CreateSubKey(App.UninstallKey))
            {
                uninstallKey.SetValueSafe("DisplayIcon", $"{Paths.Application},0");
                uninstallKey.SetValueSafe("DisplayName", App.ProjectName);

                uninstallKey.SetValueSafe("DisplayVersion", App.Version);

                if (uninstallKey.GetValue("InstallDate") is null)
                    uninstallKey.SetValueSafe("InstallDate", DateTime.Now.ToString("yyyyMMdd"));

                uninstallKey.SetValueSafe("InstallLocation", Paths.Base);
                uninstallKey.SetValueSafe("NoRepair", 1);
                uninstallKey.SetValueSafe("Publisher", App.ProjectOwner);
                uninstallKey.SetValueSafe("ModifyPath", $"\"{Paths.Application}\" -settings");
                uninstallKey.SetValueSafe("QuietUninstallString", $"\"{Paths.Application}\" -uninstall -quiet");
                uninstallKey.SetValueSafe("UninstallString", $"\"{Paths.Application}\" -uninstall");
                uninstallKey.SetValueSafe("HelpLink", App.ProjectHelpLink);
                uninstallKey.SetValueSafe("URLInfoAbout", App.ProjectSupportLink);
                uninstallKey.SetValueSafe("URLUpdateInfo", App.ProjectDownloadLink);
            }

            WindowsRegistry.RegisterApis();

            // only register player, for the scenario where the user installs bloxstrap, closes it,
            // and then launches from the website expecting it to work
            // studio can be implicitly registered when it's first launched manually
            WindowsRegistry.RegisterPlayer();

            if (CreateDesktopShortcuts)
                Shortcut.Create(Paths.Application, "", DesktopShortcut);

            if (CreateStartMenuShortcuts)
                Shortcut.Create(Paths.Application, "", StartMenuShortcut);

            if (ImportSettings && ImportSource != ImportSettingsFrom.None)
            {
                try
                {
                    ImportSettingsFromSelectedApp();
                }
                catch (Exception ex)
                {
                    Frontend.ShowMessageBox(
                        String.Format(Strings.Installer_FailedToImportSettings, ex.Message),
                        MessageBoxImage.Error,
                        MessageBoxButton.OK
                    );
                }
            }
            else
            {
                // If no import, but Modifications and State.json exist, use them
                string modificationsPath = Paths.Modifications;
                string statePath = App.State.FileLocation;

                if (Directory.Exists(modificationsPath))
                {
                    // Optionally log or notify that existing modifications are being used
                }

                if (File.Exists(statePath))
                {
                    App.State.Load(false);
                }
            }

        // existing configuration persisting from an earlier install
        // or from importing settings
        App.Settings.Load(false);
            App.State.Load(false);
            App.FastFlags.Load(false);

            if (App.IsStudioVisible)
                WindowsRegistry.RegisterStudio();

            App.Settings.Save();

            App.Logger.WriteLine(LOG_IDENT, "Installation finished");

        }

        private bool ValidateLocation()
        {
            // prevent from installing to the root of a drive
            if (InstallLocation.Length <= 3)
                return false;

            // unc path, just to be safe
            if (InstallLocation.StartsWith("\\\\"))
                return false;

            if (InstallLocation.StartsWith(Path.GetTempPath(), StringComparison.InvariantCultureIgnoreCase)
                || InstallLocation.Contains("\\Temp\\", StringComparison.InvariantCultureIgnoreCase))
                return false;

            // prevent from installing to a onedrive folder
            if (InstallLocation.Contains("OneDrive", StringComparison.InvariantCultureIgnoreCase))
                return false;

            // prevent from installing to an essential user profile folder (e.g. Documents, Downloads, Contacts idk)
            if (String.Compare(Directory.GetParent(InstallLocation)?.FullName, Paths.UserProfile, StringComparison.InvariantCultureIgnoreCase) == 0)
                return false;

            // prevent from installing into the program files folder
            if (InstallLocation.Contains("Program Files"))
                return false;

            // prevent issues with settings importing
            if (InstallLocation.Contains("Local\\Bloxstrap"))
                return false;

            return true;
        }

        public bool CheckInstallLocation()
        {
            if (string.IsNullOrEmpty(InstallLocation))
            {
                InstallLocationError = Strings.Menu_InstallLocation_NotSet;
            }
            else if (!ValidateLocation())
            {
                InstallLocationError = Strings.Menu_InstallLocation_CantInstall;
            }
            else
            {
                if (!IsImplicitInstall 
                    && !InstallLocation.EndsWith(App.ProjectName, StringComparison.InvariantCultureIgnoreCase)
                    && Directory.Exists(InstallLocation)
                    && Directory.EnumerateFileSystemEntries(InstallLocation).Any())
                {
                    string suggestedChange = Path.Combine(InstallLocation, App.ProjectName);

                    MessageBoxResult result = Frontend.ShowMessageBox(
                        String.Format(Strings.Menu_InstallLocation_NotEmpty, suggestedChange),
                        MessageBoxImage.Warning,
                        MessageBoxButton.YesNoCancel,
                        MessageBoxResult.Yes
                    );

                    if (result == MessageBoxResult.Yes)
                        InstallLocation = suggestedChange;
                    else if (result == MessageBoxResult.Cancel || result == MessageBoxResult.None)
                        return false;
                }

                try
                {
                    // check if we can write to the directory (a bit hacky but eh)
                    string testFile = Path.Combine(InstallLocation, $"{App.ProjectName}WriteTest.txt");

                    Directory.CreateDirectory(InstallLocation);
                    File.WriteAllText(testFile, "");
                    File.Delete(testFile);
                }
                catch (UnauthorizedAccessException)
                {
                    InstallLocationError = Strings.Menu_InstallLocation_NoWritePerms;
                }
                catch (Exception ex)
                {
                    InstallLocationError = ex.Message;
                }
            }

            return String.IsNullOrEmpty(InstallLocationError);
        }

        public static void DoUninstall(bool keepData)
        {
            const string LOG_IDENT = "Installer::DoUninstall";

            var processes = new List<Process>();

            if (!String.IsNullOrEmpty(App.RobloxState.Prop.Player.VersionGuid))
                processes.AddRange(Process.GetProcessesByName(App.RobloxPlayerAppName));

            if (App.IsStudioVisible)
                processes.AddRange(Process.GetProcessesByName(App.RobloxStudioAppName));

            // prompt to shutdown roblox if its currently running
            if (processes.Any())
            {
                var result = Frontend.ShowMessageBox(
                    Strings.Bootstrapper_Uninstall_RobloxRunning,
                    MessageBoxImage.Information,
                    MessageBoxButton.OKCancel,
                    MessageBoxResult.OK
                );

                if (result != MessageBoxResult.OK)
                {
                    App.Terminate(ErrorCode.ERROR_CANCELLED);
                    return;
                }

                try
                {
                    foreach (var process in processes)
                    {
                        process.Kill();
                        process.Close();
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Failed to close process! {ex}");
                }
            }

            string robloxFolder = Path.Combine(Paths.LocalAppData, "Roblox");
            bool playerStillInstalled = true;
            bool studioStillInstalled = true;

            // check if stock bootstrapper is still installed
            using var playerKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\roblox-player");
            var playerFolder = playerKey?.GetValue("InstallLocation");

            if (playerKey is null || playerFolder is not string)
            {
                playerStillInstalled = false;

                WindowsRegistry.Unregister("roblox");
                WindowsRegistry.Unregister("roblox-player");
            }
            else
            {
                bool AnselApp = File.Exists(Path.Combine((string)playerFolder, App.RobloxAnselAppName));
                string playerPath = Path.Combine((string)playerFolder, AnselApp ? App.RobloxAnselAppName : "RobloxPlayerBeta.exe");

                WindowsRegistry.RegisterPlayer(playerPath, "%1");
            }

            using var studioKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\roblox-studio");
            var studioFolder = studioKey?.GetValue("InstallLocation");

            if (studioKey is null || studioFolder is not string)
            {
                studioStillInstalled = false;

                WindowsRegistry.Unregister("roblox-studio");
                WindowsRegistry.Unregister("roblox-studio-auth");

                WindowsRegistry.Unregister("Roblox.Place");
                WindowsRegistry.Unregister(".rbxl");
                WindowsRegistry.Unregister(".rbxlx");
            }
            else
            {
                string studioPath = Path.Combine((string)studioFolder, "RobloxStudioBeta.exe");
                string studioLauncherPath = Path.Combine((string)studioFolder, "RobloxStudioLauncherBeta.exe");

                WindowsRegistry.RegisterStudioProtocol(studioPath, "%1");
                WindowsRegistry.RegisterStudioFileClass(studioPath, "-ide \"%1\"");
            }

            Registry.CurrentUser.DeleteSubKey(App.ApisKey);

            var cleanupSequence = new List<Action>
            {
                () =>
                {
                    foreach (var file in Directory.GetFiles(Paths.Desktop).Where(x => x.EndsWith("lnk")))
                    {
                        var shortcut = ShellLink.Shortcut.ReadFromFile(file);

                        if (shortcut.ExtraData.EnvironmentVariableDataBlock?.TargetUnicode == Paths.Application)
                            File.Delete(file);
                    }
                },

                () => File.Delete(StartMenuShortcut),

                () => Directory.Delete(Paths.Versions, true),

                () => Directory.Delete(Paths.Downloads, true),

                () => File.Delete(App.State.FileLocation),

                () =>
                {
                if (Paths.Roblox == Path.Combine(Paths.Base, "Roblox")) // checking if roblox is installed in base directory
                    Directory.Delete(Paths.Roblox, true);               // made that to prevent accidental removals of different builds
                }
            };

            if (!keepData)
            {
                cleanupSequence.AddRange(new List<Action>
                {
                    () => Directory.Delete(Paths.Modifications, true),
                    () => Directory.Delete(Paths.CustomCursors, true),
                    () => Directory.Delete(Paths.Logs, true),
                    () => File.Delete(App.Settings.FileLocation),
                    () => File.Delete(App.State.FileLocation)
                });

                // Only delete the Froststrap folder if keepData is false
                bool deleteFolder = Directory.GetFiles(Paths.Base).Length <= 3;
                if (deleteFolder)
                    cleanupSequence.Add(() => Directory.Delete(Paths.Base, true));
            }

            if (!playerStillInstalled && !studioStillInstalled && Directory.Exists(robloxFolder))
                cleanupSequence.Add(() => Directory.Delete(robloxFolder, true));

            cleanupSequence.Add(() => Registry.CurrentUser.DeleteSubKey(App.UninstallKey));

            foreach (var process in cleanupSequence)
            {
                try
                {
                    process();
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Encountered exception when running cleanup sequence (#{cleanupSequence.IndexOf(process)})");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }
            }

            if (Directory.Exists(Paths.Base))
            {
                // this is definitely one of the workaround hacks of all time

                string deleteCommand;

                if (!keepData && Directory.GetFiles(Paths.Base).Length <= 3)
                    deleteCommand = $"del /Q \"{Paths.Base}\\*\" && rmdir \"{Paths.Base}\"";
                else
                    deleteCommand = $"del /Q \"{Paths.Application}\"";

                Process.Start(new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c timeout 5 && {deleteCommand}",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
        }

        public static void HandleUpgrade()
        {
            const string LOG_IDENT = "Installer::HandleUpgrade";

            if (!File.Exists(Paths.Application) || Paths.Process == Paths.Application)
                return;

            // 2.0.0 downloads updates to <BaseFolder>/Updates so lol
            bool isAutoUpgrade = App.LaunchSettings.UpgradeFlag.Active
                || Paths.Process.StartsWith(Path.Combine(Paths.Base, "Updates"))
                || Paths.Process.StartsWith(Path.Combine(Paths.LocalAppData, "Temp"))
                || Paths.Process.StartsWith(Paths.TempUpdates);

            var existingVer = FileVersionInfo.GetVersionInfo(Paths.Application).ProductVersion;
            var currentVer = FileVersionInfo.GetVersionInfo(Paths.Process).ProductVersion;

            if (MD5Hash.FromFile(Paths.Process) == MD5Hash.FromFile(Paths.Application))
                return;

            if (currentVer is not null && existingVer is not null && Utilities.CompareVersions(currentVer, existingVer) == VersionComparison.LessThan)
            {
                var result = Frontend.ShowMessageBox(
                    Strings.InstallChecker_VersionLessThanInstalled,
                    MessageBoxImage.Question,
                    MessageBoxButton.YesNo
                );

                if (result != MessageBoxResult.Yes)
                    return;
            }

            // silently upgrade version if the command line flag is set or if we're launching from an auto update
            if (!isAutoUpgrade)
            {
                var result = Frontend.ShowMessageBox(
                    Strings.InstallChecker_VersionDifferentThanInstalled,
                    MessageBoxImage.Question,
                    MessageBoxButton.YesNo
                );

                if (result != MessageBoxResult.Yes)
                    return;
            }

            App.Logger.WriteLine(LOG_IDENT, "Doing upgrade");

            Filesystem.AssertReadOnly(Paths.Application);

            using (var ipl = new InterProcessLock("AutoUpdater", TimeSpan.FromSeconds(5)))
            {
                if (!ipl.IsAcquired)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Failed to update! (Could not obtain singleton mutex)");
                    return;
                }
            }

            // prior to 2.8.0, auto-updating was handled with this... bruteforce method
            // now it's handled with the system mutex you see above, but we need to keep this logic for <2.8.0 versions
            for (int i = 1; i <= 10; i++)
            {
                try
                {
                    File.Copy(Paths.Process, Paths.Application, true);
                    break;
                }
                catch (Exception ex)
                {
                    if (i == 1)
                    {
                        App.Logger.WriteLine(LOG_IDENT, "Waiting for write permissions to update version");
                    }
                    else if (i == 10)
                    {
                        App.Logger.WriteLine(LOG_IDENT, "Failed to update! (Could not get write permissions after 10 tries/5 seconds)");
                        App.Logger.WriteException(LOG_IDENT, ex);
                        return;
                    }

                    Thread.Sleep(500);
                }
            }

            using (var uninstallKey = Registry.CurrentUser.CreateSubKey(App.UninstallKey))
            {
                uninstallKey.SetValueSafe("DisplayVersion", App.Version);

                uninstallKey.SetValueSafe("Publisher", App.ProjectOwner);
                uninstallKey.SetValueSafe("HelpLink", App.ProjectHelpLink);
                uninstallKey.SetValueSafe("URLInfoAbout", App.ProjectSupportLink);
                uninstallKey.SetValueSafe("URLUpdateInfo", App.ProjectDownloadLink);
            }

            if (existingVer is not null)
            {
                if (Utilities.CompareVersions(existingVer, "1.1.4.0") == VersionComparison.LessThan)
                {
                    // move from App.State to App.RobloxState
                    if (App.State.Prop.GetDeprecatedPlayer() != null)
                        App.RobloxState.Prop.Player = App.State.Prop.GetDeprecatedPlayer()!;

                    if (App.State.Prop.GetDeprecatedStudio() != null)
                        App.RobloxState.Prop.Studio = App.State.Prop.GetDeprecatedStudio()!;

                    if (App.State.Prop.GetDeprecatedModManifest() != null)
                        App.RobloxState.Prop.ModManifest = App.State.Prop.GetDeprecatedModManifest()!;
                }

                App.Settings.Save();
                App.FastFlags.Save();
                App.State.Save();
                App.RobloxState.Save();
            }

            if (currentVer is null)
                return;

            if (isAutoUpgrade)
            {
#pragma warning disable CS0162 // Unreachable code detected
                if (OpenReleaseNotes)
                    Utilities.ShellExecute($"https://github.com/{App.ProjectRepository}/wiki/Release-notes-for-Bloxstrap-v{currentVer}");
#pragma warning restore CS0162 // Unreachable code detected
            }
            else
            {
                Frontend.ShowMessageBox(
                    string.Format(Strings.InstallChecker_Updated, currentVer),
                    MessageBoxImage.Information,
                    MessageBoxButton.OK
                );
            }
        }

        public void ImportSettingsFromSelectedApp()
        {
            string sourceDir = ImportSource switch
            {
                ImportSettingsFrom.Bloxstrap => Path.Combine(Paths.LocalAppData, "Bloxstrap"),
                ImportSettingsFrom.Voidstrap => Path.Combine(Paths.LocalAppData, "Voidstrap"),
                ImportSettingsFrom.Fishstrap => Path.Combine(Paths.LocalAppData, "Fishstrap"),
                ImportSettingsFrom.Lunastrap => Path.Combine(Paths.LocalAppData, "Lunastrap"),
                _ => throw new ArgumentOutOfRangeException(nameof(ImportSource), "Invalid import source")
            };

            if (!Directory.Exists(sourceDir))
            {
                Frontend.ShowMessageBox(Strings.Installer_InstallationNotFound, MessageBoxImage.Exclamation);
                return;
            }

            foreach (string fileName in FilesForImporting)
            {
                string actualSourceFile = fileName;
                string actualDestFile = fileName;

                if (ImportSource == ImportSettingsFrom.Voidstrap)
                {
                    if (fileName == "Settings.json")
                        actualSourceFile = "AppSettings.json";
                    if (fileName == "Modifications")
                        actualDestFile = "Mods";
                }

                string sourcePath = Path.Combine(sourceDir, actualSourceFile);
                string destinationPath = Path.Combine(InstallLocation, actualDestFile);

                try
                {
                    if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
                    {
                        App.Logger.WriteLine("Installer::ImportSettings", $"Source path does not exist: {sourcePath}");
                        continue;
                    }

                    FileAttributes attr = File.GetAttributes(sourcePath);
                    bool isDirectory = attr.HasFlag(FileAttributes.Directory);

                    if (isDirectory)
                    {
                        Directory.CreateDirectory(destinationPath);
                        CopyDirectoryContents(sourcePath, destinationPath);
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                        File.Copy(sourcePath, destinationPath, overwrite: true);
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine("Installer::ImportSettings", $"Failed to import '{fileName}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Recursively copies all contents from source directory to destination directory.
        /// Existing files are overwritten, directories are merged.
        /// </summary>
        private void CopyDirectoryContents(string sourceDir, string destDir)
        {
            foreach (var directoryPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                string targetDir = directoryPath.Replace(sourceDir, destDir);
                Directory.CreateDirectory(targetDir);
            }

            foreach (var filePath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                string targetFile = filePath.Replace(sourceDir, destDir);
                File.Copy(filePath, targetFile, overwrite: true);
            }
        }
    }
}