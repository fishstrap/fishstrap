using Bloxstrap.RobloxInterfaces;

namespace Bloxstrap.UI.ViewModels.Overlay.Controls
{
    public class OnlineStatusViewModel : VisibilityViewModel
    {
        public override string Title => Strings.Menu_Overlay_Privacy_Title;

        public override string Hint => Strings.Menu_Overlay_Privacy_Hint;

        protected override string SettingName => "online visibility";

        protected override IReadOnlyList<string> Levels => PrivacySettings.OnlineLevels;

        protected override IReadOnlyList<string> Available => State?.OnlineOptions ?? Array.Empty<string>();

        protected override string? Current => State?.Online;

        protected override async Task<string> ApplyAsync(string value)
        {
            const string LOG_IDENT = "OnlineStatusViewModel::ApplyAsync";

            bool narrowed = await PrivacySettings.SetOnlineVisibilityAsync(value, State?.Join);

            App.Logger.WriteLine(LOG_IDENT, $"Online visibility is now {value}{(narrowed ? ", with joining narrowed to match" : "")}");

            return narrowed ? Strings.Menu_Overlay_Privacy_JoinNarrowed : Strings.Menu_Overlay_Privacy_Saved;
        }
    }
}
