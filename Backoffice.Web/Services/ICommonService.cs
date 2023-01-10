
using Backoffice.Shared.Enums;

namespace Backoffice.Web.Services
{
    public interface ICommonService
    {
        string ToShortName(int currentPeriod);
        decimal GetFinalOdds(decimal originalValue);
        string GetHomeName(string name, FixtureType fixtureType);
        string GetAwayName(string name, FixtureType fixtureType);
    }
}
