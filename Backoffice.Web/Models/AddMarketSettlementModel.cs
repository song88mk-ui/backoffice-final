using Backoffice.Shared.Models;

namespace Backoffice.Web.Models
{
    public class AddMarketSettlementModel
    {
        public AddMarketSettlementModel()
        {
            AddMarketSettlementSelections = new List<AddMarketSettlementSelectionModel>();
        }

        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public string FixtureId { get; set; }
        public string MarketId { get; set; }
        public decimal DeadHeat { get; set; }
        public string BetId { get; set; }

        public List<AddMarketSettlementSelectionModel> AddMarketSettlementSelections { get; set; }
    }
}
