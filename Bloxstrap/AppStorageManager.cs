using System.Text.Encodings.Web;
using System.Text.Json.Nodes;

namespace Bloxstrap
{
    public static class AppStorageManager
    {
        public static string FileLocation => Path.Combine(Paths.Roblox, "LocalStorage", "appStorage.json");

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            // default encoder inflates file by like 30% so we're not gonna use it lol
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static void Apply()
        {
            bool background = App.Settings.Prop.EnableRobloxBackgroundApp;

            Mutate(storage =>
            {
                ApplyBackgroundApp(storage, background);
                ApplyTheme(storage);
            });

            // clear registry
            if (!background)
                WindowsRegistry.RemoveRobloxStartupEntry();
        }

        private static void ApplyBackgroundApp(JsonObject storage, bool enabled)
        {
            string state = enabled ? "true" : "false";

            // controls whether roblox launches on startup
            storage["LaunchAtStartup"] = state;

            // keeps the client active in the tray
            storage["MinimizeToTray"] = state;

            storage["SystemTrayModalShown"] = "true";
        }

        private static void ApplyTheme(JsonObject storage)
        {
            const string LOG_IDENT = "AppStorageManager::ApplyTheme";

            var theme = App.Settings.Prop.RobloxTheme;

            if (theme == RobloxTheme.Default)
                return;

            string value = theme == RobloxTheme.Dark ? "dark" : "light";

            storage["AuthenticatedTheme"] = value;

            // theme is stored per user, so we need to read the user id and update the map accordingly
            try
            {
                if (storage["UserId"] is not JsonValue idNode
                    || !idNode.TryGetValue(out string? userId)
                    || String.IsNullOrEmpty(userId))
                    return;

                JsonObject map = new();

                if (storage["DeviceLevelTheme"] is JsonValue mapNode
                    && mapNode.TryGetValue(out string? raw)
                    && !String.IsNullOrWhiteSpace(raw)
                    && JsonNode.Parse(raw) is JsonObject parsed)
                    map = parsed;

                map[userId] = value;

                // it's stored as a json string inside the json, not as a nested object
                storage["DeviceLevelTheme"] = map.ToJsonString(SerializerOptions);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to update the per-account theme cache");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private static void Mutate(Action<JsonObject> mutate)
        {
            const string LOG_IDENT = "AppStorageManager::Mutate";

            string path = FileLocation;

            if (!File.Exists(path))
            {
                Create(path, mutate);
                return;
            }

            string? tempPath = null;

            try
            {
                path = new FileInfo(path).ResolveLinkTarget(true)?.FullName ?? path;

                string contents = File.ReadAllText(path);

                if (String.IsNullOrWhiteSpace(contents))
                {
                    App.Logger.WriteLine(LOG_IDENT, "File is empty, leaving it alone");
                    return;
                }

                if (JsonNode.Parse(contents) is not JsonObject storage)
                {
                    App.Logger.WriteLine(LOG_IDENT, "File is not a JSON object, leaving it alone");
                    return;
                }

                string original = storage.ToJsonString(SerializerOptions);

                mutate(storage);

                string output = storage.ToJsonString(SerializerOptions);

                if (output == original)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Already up to date");
                    return;
                }

                tempPath = path + ".fishstrap-tmp";
                File.WriteAllText(tempPath, output, new UTF8Encoding(false));

                Filesystem.AssertReadOnly(path);
                File.Replace(tempPath, path, null);
                tempPath = null;

                App.Logger.WriteLine(LOG_IDENT, "Saved successfully");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to patch {path}");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
            finally
            {
                if (tempPath is not null)
                {
                    try { File.Delete(tempPath); }
                    catch (Exception ex) { App.Logger.WriteException(LOG_IDENT, ex); }
                }
            }
        }

        private static void Create(string path, Action<JsonObject> mutate)
        {
            const string LOG_IDENT = "AppStorageManager::Create";

            try
            {
                var storage = new JsonObject();

                mutate(storage);

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, storage.ToJsonString(SerializerOptions), new UTF8Encoding(false));

                App.Logger.WriteLine(LOG_IDENT, $"Seeded {path}");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to seed {path}");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }
    }
}
