using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models.OddsEngine.Mapping;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class UnMapped
    {
        private UnmappedModel _draggedProviderItem;
        private List<UnmappedModel> _unMappedSports;
        private ProviderMappingItem _currentMappingItem;
        private List<ProviderMappingItem> _currentMappedItems;

        private bool _shouldRender;
        protected override async Task OnInitializedAsync()
        {
            var response = await _providerMappingService.GetUnmappedSports();
            _unMappedSports = response.Result.Unmapped;
        }

        protected override bool ShouldRender() => _shouldRender;

        private void HandleDragProviderItem(UnmappedModel providerItem)
        {
            _draggedProviderItem = providerItem;
        }
        private void HandleSubmitMappingSetup(MappingSetupModel mappingSetup)
        {
            Console.WriteLine($"Mapping setup submitted. {mappingSetup.NewFixtureName}");
        }

        private void ResetMappingFields(bool resetSingle, bool resetList)
        {
            if (resetSingle)
            {
                _currentMappingItem = new ProviderMappingItem
                {
                    Id = "",
                    Na = "",
                    Info = new ProviderModel<string>
                    {
                        SrId = "0",
                        BgId = "0"
                    },
                    SpId = null,
                    SegId = null,
                    LeId = null
                };
            }

            if (resetList)
            {
                _currentMappedItems = new List<ProviderMappingItem>();
            }

            _shouldRender = true;
        }

        private ProviderMappingBulkRequest BuildRequestPayload(List<ProviderMappingItem> items)
        {
            var request = new ProviderMappingBulkRequest();

            foreach (var item in items)
            {
                request.Mapping.Add(
                    new ProviderMappingRequest()
                    {
                        Id = item.Id,
                        Na = item.Na,
                        Info = JsonSerializer.Serialize(item.Info),
                        SpId = item.SpId,
                        SegId = item.SegId,
                        LeId = item.LeId,
                        ChTyId = 0 // Will be changed to null since initial mappings must have no channel type id
                    }
                );
            }

            return request;
        }

        private void HandleCreateMappingClick()
        {
            _currentMappedItems.Add(_currentMappingItem);

            ResetMappingFields(true, false);

            _shouldRender = true;
        }

        private async Task HandleSaveMappingClick()
        {
            await _providerMappingService.AddOrUpdateProviderMappingBulk("sport", BuildRequestPayload(_currentMappedItems));

            ResetMappingFields(true, true);

            _shouldRender = true;
        }
    }
}