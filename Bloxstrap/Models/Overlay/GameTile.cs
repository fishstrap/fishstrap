namespace Bloxstrap.Models.Overlay
{
    public class GameTile
    {
        public long UniverseId { get; set; }

        public long PlaceId { get; set; }

        public string Name { get; set; } = String.Empty;

        public long? Playing { get; set; }

        public string? IconUrl { get; set; }

        public string PlayingText => Playing is null
            ? String.Empty
            : String.Format(Strings.Menu_Overlay_Games_Playing, Compact(Playing.Value));

        private static string Compact(long value) => value switch
        {
            >= 1_000_000 => (value / 1_000_000d).ToString("0.#", Locale.CurrentCulture) + "M",
            >= 1_000 => (value / 1_000d).ToString("0.#", Locale.CurrentCulture) + "K",
            _ => value.ToString(Locale.CurrentCulture)
        };
    }
}
