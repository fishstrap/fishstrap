using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Windows;

namespace Bloxstrap.PcTweaks
{
    public static class PowerPlanImporter
    {
        private const string ResourceNamespace = "Bloxstrap.Resources.FroststrapPowerPlans";

        /// <summary>
        /// Lists all embedded power plan filenames (e.g. FroststrapLowLatency.pow).
        /// </summary>
        public static IReadOnlyList<string> GetAvailablePowerPlans()
        {
            var assembly = Assembly.GetExecutingAssembly();

            return assembly
                .GetManifestResourceNames()
                .Where(name => name.StartsWith(ResourceNamespace + ".", StringComparison.OrdinalIgnoreCase)
                            && name.EndsWith(".pow", StringComparison.OrdinalIgnoreCase))
                .Select(name => name.Substring(ResourceNamespace.Length + 1)) // remove namespace prefix
                .ToList();
        }

        public static bool ImportAndActivatePowerPlan(string planFileName, out string message)
        {
            if (!IsRunningAsAdmin())
            {
                PromptAndRestartAsAdmin("This feature requires administrator privileges to import and activate a power plan.");
                message = "Operation cancelled - administrator rights are required.";
                return false;
            }

            try
            {
                string tempFile = ExtractPowerPlanToTempFile(planFileName);

                bool importSuccess = ImportPowerPlanFromFile(tempFile, out string importOut, out string importErr);

                if (File.Exists(tempFile))
                    File.Delete(tempFile);

                if (!importSuccess)
                {
                    message = $"Failed to import power plan: {importErr}";
                    return false;
                }

                // Extract GUID from import output (optional enhancement)
                string? guid = TryExtractGuidFromPowerCfgOutput(importOut);
                if (string.IsNullOrWhiteSpace(guid))
                {
                    message = "Power plan imported, but could not detect the GUID.";
                    return false;
                }

                bool setActiveSuccess = SetActivePowerPlan(guid, out string setActiveOut, out string setActiveErr);

                if (!setActiveSuccess)
                {
                    message = $"Imported but failed to activate power plan: {setActiveErr}";
                    return false;
                }

                message = "Power Plan imported and Activated successfully.";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Exception: {ex.Message}";
                return false;
            }
        }

        // Extracts embedded resource to a temporary .pow file
        private static string ExtractPowerPlanToTempFile(string powerPlanFileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = $"{ResourceNamespace}.{powerPlanFileName}";

            using Stream? resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null)
                throw new FileNotFoundException($"Resource {resourceName} not found.");

            string tempFilePath = Path.Combine(Path.GetTempPath(), powerPlanFileName);

            using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write);
            resourceStream.CopyTo(fileStream);

            return tempFilePath;
        }

        private static bool ImportPowerPlanFromFile(string filePath, out string output, out string error)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powercfg.exe",
                Arguments = $"/import \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas"
            };

            using Process process = Process.Start(startInfo)!;
            output = process.StandardOutput.ReadToEnd();
            error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return process.ExitCode == 0;
        }

        private static bool SetActivePowerPlan(string guid, out string output, out string error)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powercfg.exe",
                Arguments = $"/setactive {guid}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas"
            };

            using Process process = Process.Start(startInfo)!;
            output = process.StandardOutput.ReadToEnd();
            error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return process.ExitCode == 0;
        }

        private static bool IsRunningAsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void PromptAndRestartAsAdmin(string reasonMessage)
        {
            var result = Frontend.ShowMessageBox(
                $"{reasonMessage}\n\nRestart Froststrap as administrator?",
                MessageBoxImage.Warning,
                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                string? exeName = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exeName))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = exeName,
                            UseShellExecute = true,
                            Verb = "runas"
                        });

                        Application.Current.Shutdown();
                    }
                    catch { /* silent */ }
                }
            }
        }

        private static string? TryExtractGuidFromPowerCfgOutput(string output)
        {
            var match = System.Text.RegularExpressions.Regex.Match(output, @"([a-fA-F0-9]{8}-([a-fA-F0-9]{4}-){3}[a-fA-F0-9]{12})");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}