using Microsoft.AspNetCore.Components.Web;

namespace Backoffice.Web.Models.OddsEngine.Mapping
{
    public class MappingDropdownFilterItemModel
    {
        public MappingDropdownFilterItemModel()
        {
            IsChecked = true;
            IsVisible = true;
        }
        public string SportId { get; set; }
        public string SportName { get; set; }
        public string SegmentId { get; set; }
        public string SegmentName { get; set; }
        public string LeagueId { get; set; }
        public string LeagueName { get; set; }
        public bool IsChecked { get; set; }
        public bool IsVisible { get; set; }
    }

    public class MappingDropdownFilterGroup
    {
        public MappingDropdownFilterGroup()
        {
            SelectAllValue = true;
            Items = new List<MappingDropdownFilterItemModel>();
        }
        public string SelectAllLabel { get; set; }
        public bool SelectAllValue { get; set; }
        public List<MappingDropdownFilterItemModel> Items { get; set; }
    }
}
