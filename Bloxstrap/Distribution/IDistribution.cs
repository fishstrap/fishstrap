using Bloxstrap.AppData;

namespace Bloxstrap.Distribution
{
    public interface IDistribution
    {
        public string? RobloxDomain { get; }
        public Dictionary<string, int> CdnUrls { get; }
        public IAppData RobloxPlayerData { get; }
        public IAppData RobloxStudioData { get; }
    }
}
