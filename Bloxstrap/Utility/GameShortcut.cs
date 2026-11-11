namespace Bloxstrap.Utility
{
    internal static class GameShortcut
    {
        private const int MaxNameLength = 100;

        private static readonly Regex[] LinkPatterns =
        {
            new(@"roblox\.com/(?:[a-z]{2}(?:-[a-z]{2})?/)?games/([0-9]+)", RegexOptions.IgnoreCase),
            new(@"[?&]placeId=([0-9]+)", RegexOptions.IgnoreCase),
            new(@"^([0-9]+)$")
        };

        // includes all of the icon data for game shortcuts
        private static ReadOnlySpan<byte> IcoData => new byte[] { 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x20, 0x00 };

        private const int IcoImageOffset = 0x16;

        public static long? ParsePlaceId(string link)
        {
            string trimmed = link.Trim();

            foreach (Regex pattern in LinkPatterns)
            {
                Match match = pattern.Match(trimmed);

                if (match.Success && long.TryParse(match.Groups[1].Value, out long placeId) && placeId > 0)
                    return placeId;
            }

            return null;
        }

        public static async Task<string?> Create(long placeId)
        {
            const string LOG_IDENT = "GameShortcut::Create";

            var universe = await Http.GetJson<UniverseIdResponse>(UrlBuilder.BuildApisUrl("universes", $"v1/places/{placeId}/universe"));

            if (universe.UniverseId is not long universeId)
                return null;

            // cant we check the cache status in UniverseDetails???
            if (UniverseDetails.LoadFromCache(universeId) is null)
                await UniverseDetails.FetchSingle(universeId, ThumbnailResolution.Res256);

            UniverseDetails universeData = UniverseDetails.LoadFromCache(universeId)!;
            string gameName = FileName(universeData.Data.Name);
            string gameIconUrl = universeData.Thumbnail.ImageUrl ?? "";
            byte[] gameIcon = await App.HttpClient.GetByteArrayAsync(gameIconUrl);

            string lnkPath = Path.Combine(Paths.Desktop, $"{gameName}.lnk");
            string? lnkIcon = SaveIcon(universeId, gameIcon);

            if (File.Exists(lnkPath))
                File.Delete(lnkPath);

            Shortcut.Create(Paths.Application, $"roblox://experiences/start?placeId={placeId}", lnkPath, lnkIcon);

            if (!File.Exists(lnkPath))
                throw new IOException("The shortcut was not created");

            App.Logger.WriteLine(LOG_IDENT, $"Created a desktop shortcut for place {placeId}");

            return gameName;
        }

        private static string? SaveIcon(long universeId, byte[] gameIcon)
        {
            const string LOG_IDENT = "GameShortcut::SaveIcon";

            try
            {
                Directory.CreateDirectory(Paths.GameIcons);

                string path = Path.Combine(Paths.GameIcons, $"{universeId}.ico");

                using var writer = new BinaryWriter(File.Create(path));

                writer.Write(IcoData);
                writer.Write(gameIcon.Length);
                writer.Write(IcoImageOffset);
                writer.Write(gameIcon);

                return path;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to save the icon for universe {universeId}");
                App.Logger.WriteException(LOG_IDENT, ex);
                return null;
            }
        }

        private static string FileName(string name)
        {
            string clean = String.Concat(name.Split(Path.GetInvalidFileNameChars())).Trim().TrimEnd('.');

            if (clean.Length > MaxNameLength)
                clean = clean[..MaxNameLength].Trim();

            return String.IsNullOrEmpty(clean) ? "Roblox" : clean;
        }
    }
}
