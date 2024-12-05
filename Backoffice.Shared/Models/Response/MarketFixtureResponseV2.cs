using static Backoffice.Shared.Models.Response.MarketFixtureResponse;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class MarketFixtureResponseV2
    {
        public List<FixtureModelV2> Fixtures { get; set; }
    }
}