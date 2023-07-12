using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Models
{
    public class EditConfigTemplateFilterModel
    {
        public EditConfigTemplateFilterModel()
        {
            ConfigTemplate = new ConfigTemplateModel();
            MarketGroups = new List<SelectListItemWithCheckboxModel<string>>();
            ConfigStatuses = new List<SelectListItemWithCheckboxModel<string>>();
            MarketTemplateGroups = new List<MarketGroupNavigationModel>();
            MarketLookup = new MarketLookupResponse();
            SelectedMarkets = new List<string>();
            SelectedConfigStatus = new List<string>();
        }
        public ConfigTemplateModel ConfigTemplate { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> MarketGroups { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> ConfigStatuses { get; set; }
        public List<MarketGroupNavigationModel> MarketTemplateGroups { get; set; }
        public MarketLookupResponse MarketLookup { get; set; }
        public int SelectedMarketCount { get; set; }
        public List<string> SelectedMarkets { get; set; }

        public int SelectedConfigStatusCount { get; set; }
        public List<string> SelectedConfigStatus { get; set; }
        public string SportName { get; set; }
        public int ScopeId { get; set; }
        public int SportId { get; set; }

    }
}
