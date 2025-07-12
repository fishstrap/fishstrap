using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml.Linq;

namespace Bloxstrap
{
    public class JsonManager<T> where T : class, new()
    {
        public T OriginalProp { get; set; } = new();

        public T Prop { get; set; } = new();

        public string? LastFileHash { get; private set; }

        public bool Loaded { get; set; } = false;

        public virtual string ClassName => typeof(T).Name;
        
        public virtual string ProfilesLocation => Path.Combine(Paths.Base, $"Profiles.json");

        public virtual string FileLocation => Path.Combine(Paths.Base, $"{ClassName}.json");

        public virtual string LOG_IDENT_CLASS => $"JsonManager<{ClassName}>";

        public virtual void Load(bool alertFailure = true)
        {
            
            string LOG_IDENT = $"{LOG_IDENT_CLASS}::Load";

            App.Logger.WriteLine(LOG_IDENT, $"Loading from {FileLocation}...");

            try
            {
                string contents = File.ReadAllText(FileLocation);

                T? settings = JsonSerializer.Deserialize<T>(contents);

                if (settings is null)
                    throw new ArgumentNullException("Deserialization returned null");

                Prop = settings;
                Loaded = true;
                LastFileHash = MD5Hash.FromString(contents);

                App.Logger.WriteLine(LOG_IDENT, "Loaded successfully!");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load!");
                App.Logger.WriteException(LOG_IDENT, ex);

                if (alertFailure)
                {
                    string message = "";

                    if (ClassName == nameof(Settings))
                        message = Strings.JsonManager_SettingsLoadFailed;
                    else if (ClassName == nameof(FastFlagManager))
                        message = Strings.JsonManager_FastFlagsLoadFailed;

                    if (!String.IsNullOrEmpty(message))
                        Frontend.ShowMessageBox($"{message}\n\n{ex.Message}", System.Windows.MessageBoxImage.Warning);

                    try
                    {
                        // Create a backup of loaded file
                        File.Copy(FileLocation, FileLocation + ".bak", true);
                    }
                    catch (Exception copyEx)
                    {
                        App.Logger.WriteLine(LOG_IDENT, $"Failed to create backup file: {FileLocation}.bak");
                        App.Logger.WriteException(LOG_IDENT, copyEx);
                    }
                }

                Save();
            }
        }

        public virtual void Save()
        {
            string LOG_IDENT = $"{LOG_IDENT_CLASS}::Save";
            
            App.Logger.WriteLine(LOG_IDENT, $"Saving to {FileLocation}...");

            Directory.CreateDirectory(Path.GetDirectoryName(FileLocation)!);

            try
            {
                string contents = JsonSerializer.Serialize(Prop, new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(FileLocation, contents);

                LastFileHash = MD5Hash.FromString(contents);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to save");
                App.Logger.WriteException(LOG_IDENT, ex);

                string errorMessage = string.Format(Resources.Strings.Bootstrapper_JsonManagerSaveFailed, ClassName, ex.Message);
                Frontend.ShowMessageBox(errorMessage, System.Windows.MessageBoxImage.Warning);

                return;
            }

            App.Logger.WriteLine(LOG_IDENT, "Save complete!");
        }

        public void SaveProfile(string name)
        {
            string LOGGER_STRING = "SaveProfile::Profiles";

            string BaseDir = Paths.SavedFlagProfiles;
            try
            {
                string FileDirectory = Path.Combine(BaseDir, name);

                if (string.IsNullOrEmpty(name))
                    return;

                if (!Directory.Exists(BaseDir))
                    Directory.CreateDirectory(BaseDir);

                App.Logger.WriteLine(LOGGER_STRING, $"Writing flag profile {name}");

                if (!File.Exists(FileDirectory))
                    File.Create(FileDirectory).Dispose();

                string FastFlagsJson = JsonSerializer.Serialize(Prop, new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(FileDirectory, FastFlagsJson);
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(ex.Message, MessageBoxImage.Error);
            }
        }

        public void LoadProfile(string? name, bool? clearFlags)
        {
            string LOGGER_STRING = "LoadProfile::Profiles";

            string BaseDir = Paths.SavedFlagProfiles;

            if (string.IsNullOrEmpty(name))
                return;

            try
            {
                if (!Directory.Exists(BaseDir))
                    Directory.CreateDirectory(BaseDir);

                string[] Files = Directory.GetFiles(BaseDir);

                string FoundFile = string.Empty;

                foreach (var file in Files)
                {
                    if (Path.GetFileName(file) == name)
                    {
                        FoundFile = file;
                        break;
                    }
                }

                string SavedClientSettings = File.ReadAllText(FoundFile);

                App.Logger.WriteLine(LOGGER_STRING, $"Loading {SavedClientSettings}");

                T? settings = JsonSerializer.Deserialize<T>(SavedClientSettings);

                if (settings is null)
                    throw new ArgumentNullException("Deserialization returned null");

                App.FastFlags.suspendUndoSnapshot = true;
                App.FastFlags.SaveUndoSnapshot();

                if (clearFlags == true)
                {
                    Prop = settings;
                }
                else
                {
                    if (settings is IDictionary<string, object> settingsDict && Prop is IDictionary<string, object> propDict)
                    {
                        foreach (var kvp in settingsDict)
                        {
                            if (kvp.Value != null)
                                propDict[kvp.Key] = kvp.Value;
                        }
                    }
                }

                App.FastFlags.suspendUndoSnapshot = false;

                App.FastFlags.Save();
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(ex.Message, MessageBoxImage.Error);
            }
        }

        public void LoadPresetProfile(string? name, bool? clearFlags)
        {
            string LOGGER_STRING = "LoadProfile::Profiles";

            if (string.IsNullOrEmpty(name))
                return;

            try
            {
                string profileJson = null!;
                var assembly = Assembly.GetExecutingAssembly();
                string resourcePrefix = "Bloxstrap.Resources.PresetFlags.";

                // Check if name matches an embedded resource profile
                string resourceFullName = resourcePrefix + name;
                string? foundResource = assembly.GetManifestResourceNames()
                                               .FirstOrDefault(r => r.Equals(resourceFullName, StringComparison.OrdinalIgnoreCase));

                if (foundResource != null)
                {
                    // Load from embedded resource
                    using Stream stream = assembly.GetManifestResourceStream(foundResource)!;
                    using StreamReader reader = new StreamReader(stream);
                    profileJson = reader.ReadToEnd();

                    App.Logger.WriteLine(LOGGER_STRING, $"Loading embedded preset profile {name}");
                }
                else
                {
                    // Load from disk (user profiles)
                    string BaseDir = Paths.SavedFlagProfiles;

                    if (!Directory.Exists(BaseDir))
                        Directory.CreateDirectory(BaseDir);

                    string[] Files = Directory.GetFiles(BaseDir);
                    string FoundFile = Files.FirstOrDefault(f => Path.GetFileName(f) == name) ?? string.Empty;

                    if (string.IsNullOrEmpty(FoundFile))
                        throw new FileNotFoundException($"Profile file '{name}' not found.");

                    profileJson = File.ReadAllText(FoundFile);

                    App.Logger.WriteLine(LOGGER_STRING, $"Loading user profile from file {name}");
                }

                // Deserialize the profile JSON
                T? settings = JsonSerializer.Deserialize<T>(profileJson);

                if (settings is null)
                    throw new ArgumentNullException("Deserialization returned null");

                App.FastFlags.suspendUndoSnapshot = true;
                App.FastFlags.SaveUndoSnapshot();

                if (clearFlags == true)
                {
                    Prop = settings;
                }
                else
                {
                    if (settings is IDictionary<string, object> settingsDict && Prop is IDictionary<string, object> propDict)
                    {
                        foreach (var kvp in settingsDict)
                        {
                            if (kvp.Value != null)
                                propDict[kvp.Key] = kvp.Value;
                        }
                    }
                }

                App.FastFlags.suspendUndoSnapshot = false;

                App.FastFlags.Save();
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(ex.Message, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Is the file on disk different to the one deserialised during this session?
        /// </summary>
        public bool HasFileOnDiskChanged()
        {
            return LastFileHash != MD5Hash.FromFile(FileLocation);
        }
    }
}
