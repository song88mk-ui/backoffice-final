namespace Backoffice.Shared.Enums
{
    public enum FixtureType
    {
        Match = 0,
        Outright = 1
    }
    public enum FixtureChangeType
    {
        New = 1,
        NewStartTime = 2,
        Cancelled = 3,
        Format = 4,
        LostCoverage = 5,
        Pitcher = 6,
        Other = 101,
        NA = 102
    }
}
