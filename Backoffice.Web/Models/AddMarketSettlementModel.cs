using Backoffice.Shared.Models;

namespace Backoffice.Web.Models
{
    public class AddMarketSettlementModel
    {
        public AddMarketSettlementModel()
        {
            AddMarketSettlementSelections = new List<AddMarketSettlementSelectionModel>();
            Sports = new List<SelectListItemModel<int>>();
            Segments = new List<SelectListItemModel<int>>();
            Leagues = new List<SelectListItemModel<int>>();
            Fixtures = new List<SelectListItemModel<string>>();
            Markets = new List<SelectListItemModel<string>>();
        }

        public int SportId { get; set; }
        public List<SelectListItemModel<int>> Sports { get; set; }
        public int SegmentId { get; set; }
        public List<SelectListItemModel<int>> Segments { get; set; }
        public int LeagueId { get; set; }
        public List<SelectListItemModel<int>> Leagues { get; set; }
        public string FixtureId { get; set; }
        public List<SelectListItemModel<string>> Fixtures { get; set; }
        public string MarketId { get; set; }
        public List<SelectListItemModel<string>> Markets { get; set; }
        public decimal DeadHeat { get; set; }
        public string BetId { get; set; }

        public List<AddMarketSettlementSelectionModel> AddMarketSettlementSelections { get; set; }
    }
}
