using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class FixtureOddsHistoryResponse
    {
        public FixtureOddsHistoryResponse()
        {
            Fixtures = new List<MarketFixtureResponse.FixtureModel>();
        }
        public List<MarketFixtureResponse.FixtureModel> Fixtures { get; set; }
    }
}
