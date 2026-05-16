namespace Bloxstrap.Extensions
{
    static class CleanerOptionsEx
    {
        public static IReadOnlyCollection<CleanerOptions> Selections => new CleanerOptions[]
        {
            CleanerOptions.Disabled,
            CleanerOptions.Never,
            CleanerOptions.OneDay,
            CleanerOptions.OneWeek,
            CleanerOptions.OneMonth,
            CleanerOptions.TwoMonths
        };
    }
}
