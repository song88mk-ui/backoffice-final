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

        private void HandleSaveMappingClick()
        {
            OnSaveMappingCallback.InvokeAsync();
        }
    }
}
    