using System.Collections;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class UnMapped
    {
        private UnmappedModel _draggedProviderItem;
        private List<UnmappedModel> _unMappedSports = new();
        
        private void HandleDragProviderItem(UnmappedModel providerItem)
        {
            _draggedProviderItem = providerItem;
        }
        private void HandleDropProviderItem()
        {
            Console.WriteLine($"Dropped Provider Item: {_draggedProviderItem.Na}");
        }
        protected override async Task OnInitializedAsync()
        {
            var response = await _providerMappingService.GetUnmappedSports();
            _unMappedSports = response.Result.Unmapped;
        }
    }
}