using Backoffice.Shared.Models;

namespace Backoffice.Web.Models
{
    public class MarketSettlementModalFilterModel
    {
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
        public string SelectionId { get; set; }
        public List<SelectListItemModel<string>> Selections { get; set; }
        public int ToStatus { get; set; }
        public List<SelectListItemModel<int>> Statuses { get; set; }
        public string DateRangeFilter { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string SettledBy { get; set; }
    }
}
