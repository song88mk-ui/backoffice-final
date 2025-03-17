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
        private List<UnmappedModel> _draggedProviderItems;

        private List<UnmappedModel> _unmappedSports;
        private List<MappingSetupItemModel> _currentMappedItems = new List<MappingSetupItemModel>();

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

        private void HandleDragProviderItem(List<UnmappedModel> providerItems)
        {
            _draggedProviderItems = providerItems;

            _shouldRender = true;
        }

        private void ResetMappedItemsList()
        {
            _currentMappedItems = new List<MappingSetupItemModel>();

            _shouldRender = true;
        }

        private ProviderMappingBulkRequest BuildRequestPayload(List<MappingSetupItemModel> items)
        {
            var request = new ProviderMappingBulkRequest();
            int SrId = 0;
            int BgId = 0;

            foreach (var item in items)
            {
                // info varies per mapping category
                var info = new ProviderModel<int>()
                {
                    SrId = 0,
                    BgId = 0,   
                };

                if (Int32.TryParse(item.Providers.FirstOrDefault(i => i.ChTyId == (int)ChannelType.SR)?.Id, out SrId))
                {
                    info.SrId = SrId;
                }

                if (Int32.TryParse(item.Providers.FirstOrDefault(i => i.ChTyId == (int)ChannelType.BG)?.Id, out BgId))
                {
                    info.BgId = BgId;
                }

                // todo: map SpId, SegId, LeId for requests that need it, BE will also update request such that ChTyId is nullable
                request.Mapping.Add(
                    new ProviderMappingRequest()
                    {
                        Id = item.Id,
                        Na = item.Name,
                        Info = JsonSerializer.Serialize(info),
                        //SpId = item.SpId,
                        //SegId = item.SegId,
                        //LeId = item.LeId,
                        ChTyId = 0
                    }
                );
            }

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
            var response = await _providerMappingService.AddOrUpdateProviderMappingBulk(_mappingCategories[MappingCategory.Sport], BuildRequestPayload(_currentMappedItems));

            ResetMappedItemsList();

            _shouldRender = true;
        }
    }
}