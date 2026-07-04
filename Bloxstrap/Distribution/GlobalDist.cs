using Bloxstrap.AppData;

namespace Bloxstrap.Distribution
{
    public class GlobalDist : CommonDist, IDistribution
    {
        public override IAppData RobloxPlayerData { get; } = new RobloxPlayerData();
        public override IAppData RobloxStudioData { get; } = new RobloxStudioData();
    }
}
