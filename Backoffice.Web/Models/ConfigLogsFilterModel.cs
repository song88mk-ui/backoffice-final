namespace Backoffice.Web.Models
{
    public class ConfigLogsFilterModel
    {
        public List<SelectListItemModel<int>> FixtureTypeLookUp { get; set; }
        public int FixtureTypeId { get; set; }
        public List<SelectListItemModel<Guid>> Clients { get; set; }
        public Guid? ClientId { get; set; }
        public List<SelectListItemModel<int>> Sports { get; set; }
        public int SportId { get; set; }
        public List<SelectListItemModel<int>> Segments { get; set; }
        public int SegmentId { get; set; }
        public List<SelectListItemModel<int>> Leagues { get; set; }
        public int LeagueId { get; set; }
        public List<SelectListItemModel<int>> MarketTemplateGroups { get; set; }
        public int MarketTemplateGroupId { get; set; }
        public List<string> MarketTemplateIds { get; set; }
        public List<SelectListItemModel<string>> Markets { get; set; }
        public string MarketId { get; set; }
        public List<SelectListItemModel<string>> Scopes { get; set; }
        public string Scope { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Filter { get; set; }
        public List<ConfigLogsDetailsModel> ConfigLogsDetails { get; set; }
    }
}
