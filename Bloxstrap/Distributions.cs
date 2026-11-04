using Bloxstrap.Distribution;

namespace Bloxstrap
{
    public static class Distributions
    {
        private static IDistribution? _current;

        public static readonly Dictionary<DistributorType, string> ClientDistributions = new()
        {
            { DistributorType.Global, "Global (Default)" },
            { DistributorType.VNGGames, "VNG (Việt Nam)" },
        };

        public static DistributorType GetDistributionFromName(string distribution) => ClientDistributions.FirstOrDefault(x => x.Value == distribution).Key;

        public static List<string> GetDistributions()
        {
            var distributions = new List<string>();
            distributions.AddRange(ClientDistributions.Values);

            return distributions;
        }

        public static IDistribution Get()
        {
            var distributorType = App.Settings.Prop.DistributorType;

            if (_current is null)
                _current = GetDistributionFromType(distributorType);

            return _current;
        }

        public static void Set(DistributorType distributorType)
        {
            _current = GetDistributionFromType(distributorType);

            App.Settings.Prop.DistributorType = distributorType;

            if (_current.RobloxDomain is not null)
                App.Settings.Prop.RobloxDomain = _current.RobloxDomain;
        }

        private static IDistribution GetDistributionFromType(DistributorType distributorType)
        {
            return distributorType switch
            {
                DistributorType.Global => new GlobalDist(),
                DistributorType.VNGGames => new VNGGamesDist(),

                _ => new GlobalDist()
            };
        }
    }
}
