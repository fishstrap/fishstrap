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

        public abstract string CdnPathExtension { get; }
        public abstract bool SupportsCustomDeployments { get; }
        public abstract string RobloxPath { get; }
        public string RobloxLogs => Path.Combine(RobloxPath, "logs");

        public virtual IAppData RobloxPlayerData { get; } = new RobloxPlayerData();
        public virtual IAppData RobloxStudioData { get; } = new RobloxStudioData();
    }
}
