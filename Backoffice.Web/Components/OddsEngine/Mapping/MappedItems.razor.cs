using Backoffice.Web.Models.OddsEngine.Mapping;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class MappedItems
    {
        [Parameter]
        public List<MappingSetupItemModel> CurrentMappedItems { get; set; }
        [Parameter]
        public EventCallback OnSaveMappingCallback { get; set; }
        [Parameter]
        public EventCallback<MappingSetupItemModel?> OnRemoveCallback { get; set; }

        private void HandleSaveMappingClick()
        {
            OnSaveMappingCallback.InvokeAsync();
        }

        private void HandleRemoveAllClick()
        {
            OnRemoveCallback.InvokeAsync();
        }

        private void HandleRemoveItemClick(MappingSetupItemModel item)
        {
            OnRemoveCallback.InvokeAsync(item);
        }

        private bool IsDisabledSaveBtn()
        {
            return !CurrentMappedItems.Any();
        }

        private string GetDisabledSaveButtonCss()
        {
            return IsDisabledSaveBtn() ? "disabled" : "";
        }
    }
}