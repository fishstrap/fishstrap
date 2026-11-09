namespace Bloxstrap.UI.Elements.Overlay
{
    public enum OverlayToggleAction
    {
        Hide,
        Present,
        Ignore
    }

    public static class OverlayToggle
    {
        public static OverlayToggleAction Decide(bool visible, bool inFront, bool gameMinimised)
        {
            if (visible && (inFront || gameMinimised))
                return OverlayToggleAction.Hide;

            if (gameMinimised)
                return OverlayToggleAction.Ignore;

            return OverlayToggleAction.Present;
        }
    }
}
