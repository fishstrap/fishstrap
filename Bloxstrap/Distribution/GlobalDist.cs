namespace Bloxstrap.Distribution
{
    public class GlobalDist : CommonDist, IDistribution
    {
        public override string CdnPathExtension { get; } = string.Empty;
        public override bool SupportsCustomDeployments { get; } = true;
        public override string RobloxPath { get; } = Path.Combine(Paths.LocalAppData, "Roblox");
    }
}
