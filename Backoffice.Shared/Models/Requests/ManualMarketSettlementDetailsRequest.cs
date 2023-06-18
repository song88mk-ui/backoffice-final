using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ManualMarketSettlementDetailsRequest
    {
        public string FixtureId { get; set; }
        public string MarketId { get; set; }
        public string SelectionId { get; set; }
    }
}
