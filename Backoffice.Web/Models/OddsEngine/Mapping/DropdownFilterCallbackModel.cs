using Backoffice.Shared.Enums;

namespace Backoffice.Web.Models.OddsEngine.Mapping
{
    public class DropdownFilterCallbackModel
    {
        public bool IsChecked { get; set; }
        public MappingCategory MappingCategory { get; set; }
        public MappingDropdownFilterItemModel? Item { get; set; }
    }
}
