using System.Reflection;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.Win32;
using Wpf.Ui.Hardware;
using Bloxstrap.Integrations;
using System.Windows.Interop;
using System.Windows.Media;

namespace Bloxstrap
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
#if QA_BUILD
        public const string ProjectName = "Froststrap-QA";
#else
        public const string ProjectName = "Froststrap";
#endif
        public const string ProjectOwner = "RealMeddsam";
        public const string ProjectRepository = "RealMeddsam/Froststrap";
        public const string ProjectDownloadLink = "https://github.com/RealMeddsam/Froststrap/releases";
        public const string ProjectHelpLink = "https://github.com/bloxstraplabs/bloxstrap/wiki";
        public const string ProjectSupportLink = "https://github.com/RealMeddsam/Froststrap/issues/new";

        public const string RobloxPlayerAppName = "RobloxPlayerBeta.exe";
        public const string RobloxStudioAppName = "RobloxStudioBeta.exe";
        // one day ill add studio support
        public const string RobloxAnselAppName = "eurotrucks2.exe";

        // simple shorthand for extremely frequently used and long string - this goes under HKCU
        public const string UninstallKey = $@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{ProjectName}";

        public const string ApisKey = $"Software\\{ProjectName}";
        public static LaunchSettings LaunchSettings { get; private set; } = null!;

        public static BuildMetadataAttribute BuildMetadata = Assembly.GetExecutingAssembly().GetCustomAttribute<BuildMetadataAttribute>()!;

        public static string Version = Assembly.GetExecutingAssembly().GetName().Version!.ToString();

        public static Bootstrapper? Bootstrapper { get; set; } = null!;

        public FroststrapRichPresence? _froststrapRPC;
        public static bool IsActionBuild => !String.IsNullOrEmpty(BuildMetadata.CommitRef);

        public static bool IsProductionBuild => IsActionBuild && BuildMetadata.CommitRef.StartsWith("tag", StringComparison.Ordinal);

        public static bool IsStudioVisible => !String.IsNullOrEmpty(App.RobloxState.Prop.Studio.VersionGuid);

        public static readonly MD5 MD5Provider = MD5.Create();

        public static readonly Logger Logger = new();

        public static readonly Dictionary<string, BaseTask> PendingSettingTasks = new();

        public static readonly JsonManager<Settings> Settings = new();

        public static readonly JsonManager<State> State = new();

        public static readonly JsonManager<RobloxState> RobloxState = new();

        public static readonly FastFlagManager FastFlags = new();

        public static readonly HttpClient HttpClient = new(
            new HttpClientLoggingHandler(
                new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }
            )
        );

private static bool _showingExceptionDialog = false;

        private static string? _webUrl = null;
        public static string WebUrl
        {
            get {
                if (_webUrl != null)
                    return _webUrl;

                string url = ConstructBloxstrapWebUrl();
                if (Settings.Loaded) // only cache if settings are done loading
                    _webUrl = url;
                return url;
            }
        }
        
        public static void Terminate(ErrorCode exitCode = ErrorCode.ERROR_SUCCESS)
        {
            int exitCodeNum = (int)exitCode;

            Logger.WriteLine("App::Terminate", $"Terminating with exit code {exitCodeNum} ({exitCode})");

            Environment.Exit(exitCodeNum);
        }

        public void CreateFroststrapRpcIfNeeded()
        {
            if (Settings.Prop.ShowUsingFroststrapRPC && _froststrapRPC == null)
            {
                _froststrapRPC = new FroststrapRichPresence();
            }
        }

        public static void SoftTerminate(ErrorCode exitCode = ErrorCode.ERROR_SUCCESS)
        {
            int exitCodeNum = (int)exitCode;

            Logger.WriteLine("App::SoftTerminate", $"Terminating with exit code {exitCodeNum} ({exitCode})");

            Current.Dispatcher.Invoke(() => Current.Shutdown(exitCodeNum));
        }

        void GlobalExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            Logger.WriteLine("App::GlobalExceptionHandler", "An exception occurred");

            FinalizeExceptionHandling(e.Exception);
        }

        public static void FinalizeExceptionHandling(AggregateException ex)
        {
            foreach (var innerEx in ex.InnerExceptions)
                Logger.WriteException("App::FinalizeExceptionHandling", innerEx);

            FinalizeExceptionHandling(ex.GetBaseException(), false);
        }

        public static void FinalizeExceptionHandling(Exception ex, bool log = true)
        {
            if (log)
                Logger.WriteException("App::FinalizeExceptionHandling", ex);

            if (_showingExceptionDialog)
                return;

            _showingExceptionDialog = true;

            SendLog();

            if (Bootstrapper?.Dialog != null)
            {
                if (Bootstrapper.Dialog.TaskbarProgressValue == 0)
                    Bootstrapper.Dialog.TaskbarProgressValue = 1; // make sure it's visible

                Bootstrapper.Dialog.TaskbarProgressState = TaskbarItemProgressState.Error;
            }

            Frontend.ShowExceptionDialog(ex);

            Terminate(ErrorCode.ERROR_INSTALL_FAILURE);
        }

        public void ApplyCustomFontToWindow(Window window)
        {
            var fontPath = App.Settings.Prop.CustomFontPath;
            if (string.IsNullOrWhiteSpace(fontPath) || !File.Exists(fontPath))
                return;

            var font = FontManager.LoadFontFromFile(fontPath);
            if (font != null)
            {
                window.FontFamily = font;
            }
        }

        public static string ConstructBloxstrapWebUrl()
        {
            // dont let user switch web environment if debug mode is not on
            if (Settings.Prop.WebEnvironment == WebEnvironment.Production || !Settings.Prop.DeveloperMode)
                return "bloxstraplabs.com";

            string? sub = Settings.Prop.WebEnvironment.GetDescription();
            return $"web-{sub}.bloxstraplabs.com";
        }

        public static bool CanSendLogs()
        {
            // non developer mode always uses production
            if (!Settings.Prop.DeveloperMode || Settings.Prop.WebEnvironment == WebEnvironment.Production)
                return IsProductionBuild;

            return true;
        }

        public static async Task<GithubRelease?> GetLatestRelease()
        {
            const string LOG_IDENT = "App::GetLatestRelease";

            try
            {
                var releaseInfo = await Http.GetJson<GithubRelease>($"https://api.github.com/repos/{ProjectRepository}/releases/latest");

                if (releaseInfo is null || releaseInfo.Assets is null)
                {
                    Logger.WriteLine(LOG_IDENT, "Encountered invalid data");
                    return null;
                }

                return releaseInfo;
            }
            catch (Exception ex)
            {
                Logger.WriteException(LOG_IDENT, ex);
            }

            return null;
        }

        public static async void SendStat(string key, string value)
        {
            if (!Settings.Prop.EnableAnalytics)
                return;

            try
            {
                await HttpClient.GetAsync($"https://{WebUrl}/metrics/post?key={key}&value={value}");
            }
            catch (Exception ex)
            {
                Logger.WriteException("App::SendStat", ex);
            }
        }

        public static async void SendLog()
        {
            if (!Settings.Prop.EnableAnalytics || !CanSendLogs())
                return;

            try
            {
                await HttpClient.PostAsync(
                $"https://{WebUrl}/metrics/post-exception",
                new StringContent(Logger.AsDocument)
                );
            }
            catch (Exception ex)
            {
                Logger.WriteException("App::SendLog", ex);
            }
        }

        public static void AssertWindowsOSVersion()
        {
            const string LOG_IDENT = "App::AssertWindowsOSVersion";

            int major = Environment.OSVersion.Version.Major;
            if (major < 10) // Windows 10 and newer only
            {
                Logger.WriteLine(LOG_IDENT, $"Detected unsupported Windows version ({Environment.OSVersion.Version}).");

                if (!LaunchSettings.QuietFlag.Active)
                    Frontend.ShowMessageBox(Strings.App_OSDeprecation_Win7_81, MessageBoxImage.Error);

                Terminate(ErrorCode.ERROR_INVALID_FUNCTION);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            const string LOG_IDENT = "App::OnStartup";

            Locale.Initialize();

            base.OnStartup(e);

            if (Settings.Prop.DisableAnimations)
            {
                HardwareAcceleration.DisableAllAnimations();
            }


            if (Settings.Prop.WPFSoftwareRender)
            {
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            }

            bool fontApplied = FontManager.ApplySavedCustomFont();

            if (fontApplied)
                Logger.WriteLine(LOG_IDENT, "Custom font applied at startup.");

            foreach (Window window in Application.Current.Windows)
            {
                ApplyCustomFontToWindow(window);
            }

            Logger.WriteLine(LOG_IDENT, $"Starting {ProjectName} v{Version}");

            var userAgent = new StringBuilder($"{ProjectName}/{Version}");

            if (IsActionBuild)
            {
                Logger.WriteLine(LOG_IDENT, $"Compiled {BuildMetadata.Timestamp.ToFriendlyString()} from commit {BuildMetadata.CommitHash} ({BuildMetadata.CommitRef})");

                if (IsProductionBuild)
                    userAgent.Append(" (Production)");
                else
                    userAgent.Append($" (Artifact {BuildMetadata.CommitHash}, {BuildMetadata.CommitRef})");
            }
            else
            {
                Logger.WriteLine(LOG_IDENT, $"Compiled {BuildMetadata.Timestamp.ToFriendlyString()} from {BuildMetadata.Machine}");

#if QA_BUILD
                userAgent.Append(" (QA)");
#else
                userAgent.Append($" (Build {Convert.ToBase64String(Encoding.UTF8.GetBytes(BuildMetadata.Machine))})");
#endif
            }

            Logger.WriteLine(LOG_IDENT, $"OSVersion: {Environment.OSVersion}");
            Logger.WriteLine(LOG_IDENT, $"Loaded from {Paths.Process}");
            Logger.WriteLine(LOG_IDENT, $"Temp path is {Paths.Temp}");
            Logger.WriteLine(LOG_IDENT, $"WindowsStartMenu path is {Paths.WindowsStartMenu}");

            ApplicationConfiguration.Initialize();

            HttpClient.Timeout = TimeSpan.FromSeconds(30);

            if (!HttpClient.DefaultRequestHeaders.UserAgent.Any())
                HttpClient.DefaultRequestHeaders.Add("User-Agent", userAgent.ToString());

            LaunchSettings = new LaunchSettings(e.Args);

            using var uninstallKey = Registry.CurrentUser.OpenSubKey(UninstallKey);
            string? installLocation = null;
            bool fixInstallLocation = false;

            if (uninstallKey?.GetValue("InstallLocation") is string installLocValue)
            {
                if (Directory.Exists(installLocValue))
                {
                    installLocation = installLocValue;
                }
                else
                {
                    var match = Regex.Match(installLocValue, @"^[a-zA-Z]:\\Users\\([^\\]+)", RegexOptions.IgnoreCase);

                    if (match.Success)
                    {
                        string newLocation = installLocValue.Replace(match.Value, Paths.UserProfile, StringComparison.InvariantCultureIgnoreCase);

                        if (Directory.Exists(newLocation))
                        {
                            installLocation = newLocation;
                            fixInstallLocation = true;
                        }
                    }
                }
            }

            if (installLocation == null && Directory.GetParent(Paths.Process)?.FullName is string processDir)
            {
                var files = Directory.GetFiles(processDir).Select(Path.GetFileName).ToArray();

                if (files.Length <= 3 && files.Contains("Settings.json") && files.Contains("State.json"))
                {
                    installLocation = processDir;
                    fixInstallLocation = true;
                }
            }

            if (fixInstallLocation && installLocation != null)
            {
                var installer = new Installer
                {
                    InstallLocation = installLocation,
                    IsImplicitInstall = true
                };

                if (installer.CheckInstallLocation())
                {
                    Logger.WriteLine(LOG_IDENT, $"Changing install location to '{installLocation}'");
                    installer.DoInstall();
                }
                else
                {
                    installLocation = null; // force reinstall
                }
            }

            if (installLocation == null)
            {
                Logger.Initialize(true);
                AssertWindowsOSVersion();
                Logger.WriteLine(LOG_IDENT, "Not installed, launching the installer");
                AssertWindowsOSVersion();
                LaunchHandler.LaunchInstaller();
            }
            else
            {
                Paths.Initialize(installLocation);

                if (Paths.Process != Paths.Application && !File.Exists(Paths.Application))
                    File.Copy(Paths.Process, Paths.Application);

                Logger.Initialize(LaunchSettings.UninstallFlag.Active);

                if (!Logger.Initialized && !Logger.NoWriteMode)
                {
                    Logger.WriteLine(LOG_IDENT, "Possible duplicate launch detected, terminating.");
                    Terminate();
                }

                Settings.Load();
                State.Load();
                RobloxState.Load();
                FastFlags.Load();

                if (!Locale.SupportedLocales.ContainsKey(Settings.Prop.Locale))
                {
                    Settings.Prop.Locale = "nil";
                    Settings.Save();
                }

                Logger.WriteLine(LOG_IDENT, $"Developer mode: {Settings.Prop.DeveloperMode}");
                Logger.WriteLine(LOG_IDENT, $"Web environment: {Settings.Prop.WebEnvironment}");

                Locale.Set(Settings.Prop.Locale);

                if (!LaunchSettings.BypassUpdateCheck)
                    Installer.HandleUpgrade();

                WindowsRegistry.RegisterApis();

                LaunchHandler.ProcessLaunchArgs();
            }

            if (string.IsNullOrWhiteSpace(Settings.Prop.UserId))
            {
                Settings.Prop.UserId = Guid.NewGuid().ToString();
                Settings.Save();
                Logger.WriteLine("Startup", $"Generated UserId: {Settings.Prop.UserId}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _froststrapRPC?.Dispose();
            base.OnExit(e);
        }
    }
}