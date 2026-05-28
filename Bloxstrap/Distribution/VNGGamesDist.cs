namespace Bloxstrap.Distribution
{
    public class VNGGamesDist : CommonDist, IDistribution
    {
        public override string CdnPathExtension { get; } = "/vng";
        public override bool SupportsCustomDeployments { get; } = false;
        public override string RobloxPath { get; } = Path.Combine(Paths.LocalAppData, "RobloxPCVNG");
    }
}