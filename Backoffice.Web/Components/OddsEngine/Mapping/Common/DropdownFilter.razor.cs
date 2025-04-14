using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models.OddsEngine.Mapping;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Backoffice.Web.Components.OddsEngine.Mapping.Common
{
    public partial class DropdownFilter
    {
        [Parameter]
        public EventCallback<DropdownFilterCallbackModel> OnClickCallback { get; set; }
        [Parameter]
        public MappingDropdownFilterGroup FilterGroup { get; set; }
        [Parameter]
        public MappingCategory MappingCategory { get; set; }

        private bool _showDropdown = false;

        private async Task HandleItemClick(ChangeEventArgs e, MappingDropdownFilterItemModel? item)
        {
            await OnClickCallback.InvokeAsync(new DropdownFilterCallbackModel
            {
                IsChecked = (bool)e.Value,
                MappingCategory = MappingCategory,
                Item = item
            });
        }

        private void HandleClickDropdown()
        {
            _showDropdown = !_showDropdown;
        }

        private string GetDropdownLabel()
        {
            List<MappingDropdownFilterItemModel> checkedItems = FilterGroup.Items.Where(x => x.IsChecked).ToList();
            List<string> namesToJoin = new List<string>();

            switch (MappingCategory)
            {
                case MappingCategory.Sport:
                    namesToJoin = checkedItems.Select(x => x.SportName).ToList();
                    break;
                case MappingCategory.Segment:
                    namesToJoin = checkedItems.Select(x => x.SegmentName).ToList();
                    break;
                case MappingCategory.League:
                    namesToJoin = checkedItems.Select(x => x.LeagueName).ToList();
                    break;
                default:
                    break;
            }
            return FilterGroup.SelectAllValue == true ? _translationState.GetValue("All Sports") : String.Join(", ", namesToJoin);
        }

        private string GetCheckboxLabel(MappingDropdownFilterItemModel item)
        {
            string label = "";

            switch (MappingCategory)
            {
                case MappingCategory.Sport:
                    label = item.SportName;
                    break;
                case MappingCategory.Segment:
                    label = item.SegmentName;
                    break;
                case MappingCategory.League:
                    label = item.LeagueName;
                    break;
                default:
                    break;
            }

            return label;
        }
    }
}
