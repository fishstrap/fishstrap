using Bloxstrap.AppData;

namespace Bloxstrap.Distribution
{
    public class VNGGamesDist : CommonDist, IDistribution
    {
        public override string? RobloxDomain => "robloxapp.vnggames.com";

        public override IAppData RobloxPlayerData { get; } = new RobloxPlayerVNGData();
        public override IAppData RobloxStudioData { get; } = new RobloxStudioData();
    }
}