namespace Backoffice.Web.Models.OddsEngine.Config
{
    public class ConfigContentFilterModel
    {
        public string Region { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }
        public List<SelectListItemModel<string>> Sports { get; set; }
        public List<SelectListItemModel<string>> Scopes { get; set; }
    }
}
