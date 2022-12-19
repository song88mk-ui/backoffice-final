using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class MarketFixtureResponse
    {
        public MarketFixtureResponse()
        {
            Fixtures = new List<FixtureModel>();
        }

        public List<FixtureModel> Fixtures { get; set; }
    }
}
