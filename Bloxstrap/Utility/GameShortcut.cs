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

        private static string IconsFolder => Path.Combine(Paths.Base, "GameIcons");

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

            var universe = await Http.GetJson<UniverseIdResponse>(UrlBuilder.BuildApiUrl("apis", $"universes/v1/places/{placeId}/universe"));

            if (universe.UniverseId is not long universeId)
                return null;

            if (UniverseDetails.LoadFromCache(universeId) is null)
                await UniverseDetails.FetchSingle(universeId);

            string name = UniverseDetails.LoadFromCache(universeId)!.Data.Name;
            string lnkPath = Path.Combine(Paths.Desktop, $"{FileName(name)}.lnk");

            if (File.Exists(lnkPath))
                File.Delete(lnkPath);

            Shortcut.Create(Paths.Application, $"roblox://experiences/start?placeId={placeId}", lnkPath, await SaveIcon(universeId));

            if (!File.Exists(lnkPath))
                throw new IOException("The shortcut was not created");

            App.Logger.WriteLine(LOG_IDENT, $"Created a desktop shortcut for place {placeId}");

            return name;
        }

        private static async Task<string?> SaveIcon(long universeId)
        {
            const string LOG_IDENT = "GameShortcut::SaveIcon";

            try
            {
                var response = await Http.GetJson<ApiArrayResponse<ThumbnailResponse>>(
                    UrlBuilder.BuildApiUrl("thumbnails", $"v1/games/icons?universeIds={universeId}&returnPolicy=PlaceHolder&size=256x256&format=Png&isCircular=false"));

                string? url = response.Data.FirstOrDefault()?.ImageUrl;

                if (String.IsNullOrEmpty(url))
                    return null;

                byte[] png = await App.HttpClient.GetByteArrayAsync(url);

                Directory.CreateDirectory(IconsFolder);

                string path = Path.Combine(IconsFolder, $"{universeId}.ico");

                using var writer = new BinaryWriter(File.Create(path));

                // what the fuck
                writer.Write((short)0);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((short)1);
                writer.Write((short)32);
                writer.Write(png.Length);
                writer.Write(22);
                writer.Write(png);

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
