using Bloxstrap.RobloxInterfaces;

namespace Bloxstrap.UI.ViewModels.Overlay.Controls
{
    public class GameStatusViewModel : VisibilityViewModel
    {
        public override string Title => Strings.Menu_Overlay_GamePrivacy_Title;

        public override string Hint => Strings.Menu_Overlay_GamePrivacy_Hint;

        protected override string SettingName => "game visibility";

        protected override IReadOnlyList<string> Levels => PrivacySettings.JoinLevels;

        protected override IReadOnlyList<string> Available => State?.JoinOptions ?? Array.Empty<string>();

        protected override string? Current => State?.Join;

        protected override bool IsAllowed(string value) => PrivacySettings.GameVisibilityAllowed(value, State?.Online);

        protected override async Task<string> ApplyAsync(string value)
        {
            const string LOG_IDENT = "GameStatusViewModel::ApplyAsync";

            await PrivacySettings.SetGameVisibilityAsync(value, State?.Online);

            App.Logger.WriteLine(LOG_IDENT, $"Game visibility is now {value}");

            return Strings.Menu_Overlay_Privacy_Saved;
        }
    }
}
