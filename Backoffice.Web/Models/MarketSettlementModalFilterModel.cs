using Backoffice.Shared.Models;

namespace Backoffice.Web.Models
{
    public class MarketSettlementModalFilterModel
    {
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public string FixtureId { get; set; }
        public string MarketId { get; set; }
        public string SelectionId { get; set; }
        public int ToStatus { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string SettledBy { get; set; }
    }
}