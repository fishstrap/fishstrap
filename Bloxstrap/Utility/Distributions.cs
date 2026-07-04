using Bloxstrap.Distribution;

namespace Bloxstrap.Utility
{
    public static class Distributions
    {
        private static IDistribution? _current;

        public static IDistribution GetCurrent()
        {
            var distributorType = App.Settings.Prop.DistributorType;

            if (_current is null)
                _current = distributorType switch
                {
                    DistributorType.Global => new GlobalDist(),
                    DistributorType.VNGGames => new VNGGamesDist(),

                    _ => new GlobalDist()
                };

            return _current;
        }
    }
}
