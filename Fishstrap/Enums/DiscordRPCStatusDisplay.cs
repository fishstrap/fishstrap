using DiscordRPC;

namespace Fishstrap.Enums
{
    public enum DiscordRPCStatusDisplay
    {
        [EnumName(FromTranslation = "Enums.DiscordRPCStatusDisplay.Name")]
        Name = StatusDisplayType.Name,

        [EnumName(FromTranslation = "Enums.DiscordRPCStatusDisplay.Details")]
        Details = StatusDisplayType.Details,
    }
}