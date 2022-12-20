using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ManualMessageSettlementRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public int InitialBetSelectionStatusId { get; set; }
        public int FixtureId { get; set; }
        public int MarketId { get; set; }
        public string SettledBy { get; set; }
        public long Id { get; set; }
        public string CursorDirection { get; set; }
        public MetaData MetaData { get; set; }
    }
}
