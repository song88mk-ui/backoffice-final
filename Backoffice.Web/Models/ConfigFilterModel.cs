using Backoffice.Shared.Models;

namespace Backoffice.Web.Models
{
    public class ConfigFilterModel
    {
        public ConfigFilterModel()
        {
            MarketTemplates = new List<MarketLookupNavigationModel>();
            MarketTemplateGroups = new List<MarketGroupNavigationModel>();
            Clients = new List<ClientInformationModel>();
            Scopes = new List<SelectListItemModel<string>>();
            ConfigStatuses = new List<SelectListItemModel<string>>();
            Sports = new List<SelectListItemModel<int>>();
            Segments = new List<SelectListItemModel<int>>();
            Leagues = new List<SelectListItemModel<int>>();
        }
        public List<MarketLookupNavigationModel> MarketTemplates { get; set; }
        public List<MarketGroupNavigationModel> MarketTemplateGroups { get; set; }
        public int MarketTemplateGroupId { get; set; }
        public string MarketTemplateGroupName { get; set; }
        public string HeaderTitle { get; set; }
        public string LeagueName { get; set; }

        public string ClientCode { get; set; }
        public string ClientId { get; set; }
        public List<ClientInformationModel> Clients { get; set; }
        public int Scope { get; set; }
        public List<SelectListItemModel<string>> Scopes { get; set; }
        public int FilterConfigStatus { get; set; }
        public List<SelectListItemModel<string>> ConfigStatuses { get; set; }
        public int SportId { get; set; }
        public List<SelectListItemModel<int>> Sports { get; set; }
        public int SegmentId { get; set; }
        public List<SelectListItemModel<int>> Segments { get; set; }
        public int LeagueId { get; set; }
        public List<SelectListItemModel<int>> Leagues { get; set; }

        public ConfigModel ClientConfig { get; set; }

        public string SearchText { get; set; }
    }
    public class ConfigActivityLogFilterModel
    {
        public string SelectedSportId { get; set; }
        public string SelectedScopeId { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string SearchText { get; set; } = "";
    }
}
