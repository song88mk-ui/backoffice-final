using Backoffice.Shared.Models;

namespace Backoffice.Web.Models
{
    public class EditConfigTemplateFilterModel
    {
        public EditConfigTemplateFilterModel()
        {
            Markets = new List<SelectListItemWithCheckboxModel<string>>();
            ConfigStatuses = new List<SelectListItemWithCheckboxModel<string>>();
            SelectedMarkets = new List<string>();
            SelectedConfigStatus = new List<string>();
        }

        public List<SelectListItemWithCheckboxModel<string>> Markets { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> ConfigStatuses { get; set; }

        public int SelectedMarketCount { get; set; }
        public List<string> SelectedMarkets { get; set; }

        public int SelectedConfigStatusCount { get; set; }
        public List<string> SelectedConfigStatus { get; set; }

    }
}
