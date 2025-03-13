using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models.OddsEngine.Mapping;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Mapping
{
    public partial class Unmapped
    {
        private UnmappedModel _draggedProviderItem;
        //private ProviderMappingItem _currentMappingItem;

        private List<UnmappedModel> _unmappedSports;
        private List<MappingSetupItemModel> _currentMappedItems;

        private Dictionary<MappingCategory, string> _mappingCategories = new Dictionary<MappingCategory, string>
        {
            { MappingCategory.Sport, "sport" },
            { MappingCategory.Segment, "segment" },
            { MappingCategory.League, "league" },
            { MappingCategory.Fixture, "fixture" },
            { MappingCategory.Market, "market" },
            { MappingCategory.Selection, "selection" }
        };

        private bool _shouldRender;

        protected override bool ShouldRender() => _shouldRender;

        protected override async Task OnInitializedAsync()
        {
            var response = await _providerMappingService.GetUnmappedSports();
            _unmappedSports = response.Result.Unmapped;

            _shouldRender = true;
        }

        private void HandleDragProviderItem(UnmappedModel providerItem)
        {
            _draggedProviderItem = providerItem;

            _shouldRender = true;
        }

        private void ResetMappedItemsList()
        {
            //if (resetSingle)
            //{
            //    _currentMappingItem = new ProviderMappingItem
            //    {
            //        Id = "",
            //        Na = "",
            //        Info = new ProviderModel<string>
            //        {
            //            SrId = "0",
            //            BgId = "0"
            //        },
            //        SpId = null,
            //        SegId = null,
            //        LeId = null
            //    };
            //}

            _currentMappedItems = new List<MappingSetupItemModel>();

            _shouldRender = true;
        }

        private ProviderMappingBulkRequest BuildRequestPayload(List<MappingSetupItemModel> items)
        {
            var request = new ProviderMappingBulkRequest();

            //foreach (var item in items)
            //{
            //    request.Mapping.Add(
            //        new ProviderMappingRequest()
            //        {
            //            Id = item.Id,
            //            Na = item.Na,
            //            Info = JsonSerializer.Serialize(item.Info),
            //            SpId = item.SpId,
            //            SegId = item.SegId,
            //            LeId = item.LeId,
            //            ChTyId = 0 // Will be changed to null since initial mappings must have no channel type id
            //        }
            //    );
            //}

            return request;
        }

        private void HandleCreateMappingClick(MappingSetupItemModel item)
        {
            _currentMappedItems.Add(item);

            _shouldRender = true;
        }

        private async Task HandleSaveMappingClick()
        {
            // todo: create if/switch case for different categories based on selected mapping category
            await _providerMappingService.AddOrUpdateProviderMappingBulk(_mappingCategories[MappingCategory.Sport], BuildRequestPayload(_currentMappedItems));

            ResetMappedItemsList();

            _shouldRender = true;
        }
    }
}