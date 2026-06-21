using System.Reflection;
using DiscordRPC;

namespace Fishstrap.Extensions
{
    public static class DiscordRPCStatusDisplayEx
    {
        public static IReadOnlyList<DiscordRPCStatusDisplay> Selections => new DiscordRPCStatusDisplay[]
        {
            DiscordRPCStatusDisplay.Name,
            DiscordRPCStatusDisplay.Details,
        };

        public static StatusDisplayType ToStatusDisplayType(this DiscordRPCStatusDisplay statusDisplay)
        {
            return (StatusDisplayType)statusDisplay;
        }
    }
}
