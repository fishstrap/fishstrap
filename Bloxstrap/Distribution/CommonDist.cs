using Bloxstrap.AppData;

namespace Bloxstrap.Distribution
{
    public abstract class CommonDist : IDistribution
    {
        public virtual string? RobloxDomain { get; } = null;

        public virtual Dictionary<string, int> CdnUrls { get; } = new()
        {
            { "https://setup.rbxcdn.com", 0 },
            { "https://setup-aws.rbxcdn.com", 2 },
            { "https://setup-ak.rbxcdn.com", 2 },
            { "https://s3.amazonaws.com/setup.roblox.com", 4 }
        };

        public abstract IAppData RobloxPlayerData { get; }
        public abstract IAppData RobloxStudioData { get; }
    }
}
