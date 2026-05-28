using Bloxstrap.Distribution;

namespace Bloxstrap.Utility
{
    public static class Distributions
    {
        private static IDistribution? _current;

#if DEBUG
#warning Forcing VNGGames distributor
        public static IDistribution GetCurrent()
        {
            if (_current is null)
                _current = new VNGGamesDist();

            return _current;
        }
#else
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
#endif
    }
}
