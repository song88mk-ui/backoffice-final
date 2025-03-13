using System.Collections;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models.OddsEngine.Mapping;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class UnMapped
    {
        private UnmappedModel _draggedProviderItem;
        private List<UnmappedModel> _unMappedSports = new();
        protected override async Task OnInitializedAsync()
        {
            var response = await _providerMappingService.GetUnmappedSports();
            _unMappedSports = response.Result.Unmapped;
        }
        private void HandleDragProviderItem(UnmappedModel providerItem)
        {
            _draggedProviderItem = providerItem;
        }

        private void HandleDropMappingProvider()
        {
            if (_draggedProviderItem != null)
            {
                _unMappedSports.Remove(_draggedProviderItem);
            }
        }

        private void HandleRemoveProviderItem(UnmappedModel providerItem)
        {
            _unMappedSports.Add(providerItem);
        }
        
        private void HandleSubmitMappingSetup(MappingSetupModel mappingSetup)
        {
            Console.WriteLine($"Mapping setup submitted. {mappingSetup.NewFixtureName}");
        }
    }
}