using Backoffice.Shared.Models;

namespace Backoffice.Web.Models
{
    public class EditConfigTemplateFilterModel
    {
        public EditConfigTemplateFilterModel()
        {
            //MarketTemplates = new List<MarketLookupNavigationModel>();
            //MarketTemplateGroups = new List<MarketGroupNavigationModel>();
            //Clients = new List<ClientInformationModel>();
            //Scopes = new List<SelectListItemModel<string>>();
            //ConfigStatuses = new List<SelectListItemModel<string>>();
            //Sports = new List<SelectListItemModel<int>>();
            //Segments = new List<SelectListItemModel<int>>();
            //Leagues = new List<SelectListItemModel<int>>();
        }

        public List<SelectListItemWithCheckboxModel<string>> Markets { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> ConfigStatuses { get; set; }

        public int SelectedClientCount { get; set; }
        public string SelectedClientName { get; set; } = "";
        public int SelectedLeagueCount { get; set; }
        public string SelectedLeagueName { get; set; } = "";
        public int SelectedMarketCount { get; set; }
        public string SelectedMarketName { get; set; } = "";

        public int SelectedConfigStatusCount { get; set; }
        public string SelectedConfigStatusName { get; set; } = "";

        //public List<MarketLookupNavigationModel> MarketTemplates { get; set; }
        //public List<MarketGroupNavigationModel> MarketTemplateGroups { get; set; }
        //public int MarketTemplateGroupId { get; set; }
        //public string MarketTemplateGroupName { get; set; }
        //public string HeaderTitle { get; set; }
        //public string LeagueName { get; set; }

        //public string ClientCode { get; set; }
        //public string ClientId { get; set; }
        //public List<ClientInformationModel> Clients { get; set; }
        //public int Scope { get; set; }
        //public List<SelectListItemModel<string>> Scopes { get; set; }
        //public int FilterConfigStatus { get; set; }
        //public List<SelectListItemModel<string>> ConfigStatuses { get; set; }
        //public int SportId { get; set; }
        //public List<SelectListItemModel<int>> Sports { get; set; }
        //public int SegmentId { get; set; }
        //public List<SelectListItemModel<int>> Segments { get; set; }
        //public int LeagueId { get; set; }
        //public List<SelectListItemModel<int>> Leagues { get; set; }

        //public ConfigModel ClientConfig { get; set; }

        //public string SearchText { get; set; }
    }
}
