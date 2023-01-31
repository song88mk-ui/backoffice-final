using Backoffice.Shared.Models;

namespace Backoffice.Web.Models
{
    public class AddPriceBoostFilterModel
    {
        public AddPriceBoostFilterModel()
        {
            Sports = new List<SelectListItemModel<int>>();
            Segments = new List<SelectListItemModel<int>>();
            Leagues = new List<SelectListItemModel<int>>();
            Fixtures = new List<SelectListItemModel<string>>();
            Markets = new List<SelectListItemModel<string>>();
            Selections = new List<SelectListItemModel<string>>();
            PriceBoostConfigList = new List<PriceBoostConfigModel>();
            PriceBoostConfigAuditList = new List<PriceBoostConfigAuditModel>();
        }
        public string ClientCode { get; set; }
        public int SportId { get; set; }

        public string SportName { get; set; }
        public List<SelectListItemModel<int>> Sports { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public List<SelectListItemModel<int>> Segments { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public List<SelectListItemModel<int>> Leagues { get; set; }
        public string ProviderFixtureId { get; set; }
        public string FixtureName { get; set; }
        public List<SelectListItemModel<string>> Fixtures { get; set; }
        public string MarketTemplateId { get; set; }
        public string MarketSpecifier { get; set; }
        public string MarketName { get; set; }
        public List<SelectListItemModel<string>> Markets { get; set; }
        public string SelectionTemplateId { get; set; }
        public string SelectionSpecifier { get; set; }
        public string SelectionName { get; set; }
        public List<SelectListItemModel<string>> Selections { get; set; }
        public List<PriceBoostConfigModel> PriceBoostConfigList { get; set; }
        public List<PriceBoostConfigAuditModel> PriceBoostConfigAuditList { get; set; }
    }
}
