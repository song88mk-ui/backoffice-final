using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models.OddsEngine.Mapping;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Mapping.League
{
    public partial class UnMappingLeague
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }

        private List<MappingProviderItemModel> _unmappedSports;
        private List<MappingProviderItemModel> _tempFilteredUnmappedSports;
        private List<MappingProviderItemModel> _currentMappingSetupProviderItems = new List<MappingProviderItemModel>();
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
            _unmappedSports = response.Result.Unmapped.Select(s => new MappingProviderItemModel(s)).ToList();
            _tempFilteredUnmappedSports = _unmappedSports;

            _shouldRender = true;
        }

        private async Task HandleProviderItemClick(MappingProviderItemModel providerItem)
        {
            if (providerItem == null) return;

            int sameProviderIndex = _currentMappingSetupProviderItems.FindIndex(x => x.ChTyId == providerItem.ChTyId);
            bool hasItemOfSameProvider = sameProviderIndex != -1;

            if (hasItemOfSameProvider)
            {
                _currentMappingSetupProviderItems[sameProviderIndex] = providerItem;
            }
            else
            {
                _currentMappingSetupProviderItems.Add(providerItem);
            }

            FilterProviderItems();

            SetLinkedItems();

            await ScrollItemListsToTop();

            _shouldRender = true;
        }

        private void ResetMappingItems(bool resetMappingSetupProviderItems = true, bool resetMappedItems = true)
        {
            if (resetMappingSetupProviderItems)
            {
                _currentMappingSetupProviderItems = new List<MappingProviderItemModel>();

                SetLinkedItems();
            }

            if (resetMappedItems)
            {
                _currentMappedItems = new List<MappingSetupItemModel>();
            }

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

            ResetMappingItems(true, false);

            _shouldRender = true;
        }

        private async Task HandleSaveMappingClick()
        {
            // todo: create if/switch case for different categories based on selected mapping category
            var response = await _providerMappingService.AddOrUpdateProviderMappingBulk(_mappingCategories[MappingCategory.Sport], BuildRequestPayload(_currentMappedItems));

            _unmappedSports = _tempFilteredUnmappedSports;

            ResetMappingItems();

            _shouldRender = true;
        }

        private void HandleRemoveMappingSetupProviderItem(MappingProviderItemModel providerItem)
        {
            if (providerItem == null) return;

            _currentMappingSetupProviderItems = _currentMappingSetupProviderItems.Where(item => item.ChTyId != providerItem.ChTyId).ToList();

            FilterProviderItems();

            SetLinkedItems();

            _shouldRender = true;
        }

        private void HandleRemoveMappedItem(MappingSetupItemModel singleItem = null)
        {
            if (singleItem == null)
            {
                ResetMappingItems(false, true);
            }
            else
            {
                _currentMappedItems = _currentMappedItems.Where(x => x.Id != singleItem.Id && x.Name != singleItem.Name).ToList();
            }

            FilterProviderItems();

            _shouldRender = true;
        }

        private List<MappingProviderItemModel> GetAllItemsToBeFiltered()
        {
            List<MappingProviderItemModel> allItemsToBeFiltered = new List<MappingProviderItemModel>();

            allItemsToBeFiltered.AddRange(_currentMappingSetupProviderItems);

            foreach(var mappedItem in _currentMappedItems)
            {
                allItemsToBeFiltered.AddRange(mappedItem.Providers);
            }

            return allItemsToBeFiltered;
        }

        private void FilterProviderItems()
        {
            _tempFilteredUnmappedSports = _unmappedSports;

            var allItemsToBeFiltered = GetAllItemsToBeFiltered();

            foreach (var providerItem in allItemsToBeFiltered)
            {
                _tempFilteredUnmappedSports = _tempFilteredUnmappedSports.Where(x => JsonSerializer.Serialize(x) != JsonSerializer.Serialize(providerItem)).ToList();
            }

            _shouldRender = true;
        }

        private void SetLinkedItems()
        {
            int itemIndex = -1;

            List<string> srIds = new List<string>();
            List<string> bgIds = new List<string>();

            foreach (var currentMappingItem in _currentMappingSetupProviderItems)
            {
                srIds.AddRange(currentMappingItem.OtPrLi.SrId.Select(s => s.ToString()));
                bgIds.AddRange(currentMappingItem.OtPrLi.BgId.Select(s => s.ToString()));
            }

            foreach (var item in _tempFilteredUnmappedSports)
            {
                if ((srIds.Contains(item.Id) && item.ChTyId == (int)ChannelType.SR) || 
                    (bgIds.Contains(item.Id) && item.ChTyId == (int)ChannelType.BG))
                {
                    item.IsCurrentLinkedItem = true;
                    continue;
                }

                item.IsCurrentLinkedItem = false;
            }

            _shouldRender = true;
        }

        private async Task ScrollItemListsToTop()
        {
            var _groupedItems = _tempFilteredUnmappedSports.GroupBy(m => m.ChTyId).ToDictionary(g => g.Key, g => g.OrderBy(x => x.IsCurrentLinkedItem ? 0 : 1).ToList());

            foreach (var key in _groupedItems.Keys)
            {
                if (_groupedItems[key].First().IsCurrentLinkedItem == true)
                {
                    await JSRuntime.InvokeVoidAsync("scrollToTopOfElement", $".provider-{key}-items-container", "fast");
                }
            }
        }
    }
}
