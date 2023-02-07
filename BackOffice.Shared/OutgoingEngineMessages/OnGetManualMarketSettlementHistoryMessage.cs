using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.OutgoingEngineMessages
{
    public class OnGetMarketSettlementHistoryMessage
    {
        public string UserId { get; set; }
        public string ConnectionId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string SportId { get; set; }
        public string LeagueId { get; set; }
        public string SegmentId { get; set; }
        public string FixtureId { get; set; }
        public string MarketId { get; set; }
        public string SelectionId { get; set; }
        public string SelectionStatus { get; set; }
        public string ManualSettledBy { get; set; }
    }
}
