using Backoffice.Shared.Enums;
using Backoffice.Web.Models.OddsEngine.Mapping;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Mapping.Base
{
    public class BaseUnmapped : ComponentBase
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }

        [Parameter] public EventCallback<UpdateMappingTabCountModel> OnItemsCountUpdateCallback { get; set; }

        public List<MappingProviderItemModel> UnmappedItems { get; set; }
        public List<MappingProviderItemModel> TempFilteredUnmappedItems { get; set; }
        public List<MappingProviderItemModel> CurrentMappingSetupProviderItems { get; set; }
        public List<MappingSetupItemModel> CurrentMappedItems { get; set; }
        public UpdateMappingTabCountModel CountCallbackModel { get; set; }
        public bool IsAllowedMultipleItemsPerProvider { get; set; }
        public bool RenderComponent { get; set; } = false;

        public Dictionary<MappingCategory, string> FetchMappingCategories = new Dictionary<MappingCategory, string>
        {
            { MappingCategory.Sport, "sports" },
            { MappingCategory.Segment, "segments" },
            { MappingCategory.League, "leagues" },
            { MappingCategory.Fixture, "fixtures" },
            { MappingCategory.Market, "markets" },
            { MappingCategory.Selection, "selections" }
        };

        public Dictionary<MappingCategory, string> SaveMappingCategories = new Dictionary<MappingCategory, string>
        {
            { MappingCategory.Sport, "sport" },
            { MappingCategory.Segment, "segment" },
            { MappingCategory.League, "league" },
            { MappingCategory.Fixture, "fixture" },
            { MappingCategory.Market, "market" },
            { MappingCategory.Selection, "selection" }
        };

        public Dictionary<MappingCategory, List<int>> _providerItemFilters = new Dictionary<MappingCategory, List<int>>
        {
            { MappingCategory.Sport, new List<int>() },
            { MappingCategory.Segment, new List<int>() },
            { MappingCategory.League, new List<int>() }
        };

        public BaseUnmapped()
        {
            UnmappedItems = new List<MappingProviderItemModel>();
            TempFilteredUnmappedItems = new List<MappingProviderItemModel>();
            CurrentMappingSetupProviderItems = new List<MappingProviderItemModel>();
            CurrentMappedItems = new List<MappingSetupItemModel> { };
            CountCallbackModel = new UpdateMappingTabCountModel
            {
                Tab = MappingTab.Unmapped,
                Count = 0
            };
        }

        protected override bool ShouldRender() => RenderComponent;

        protected override async Task OnInitializedAsync()
        {

        }

        public async Task HandleProviderItemClick(MappingProviderItemModel providerItem)
        {
            if (providerItem == null) return;

            int sameProviderIndex = CurrentMappingSetupProviderItems.FindIndex(x => x.ChTyId == providerItem.ChTyId);
            bool hasItemOfSameProvider = sameProviderIndex != -1;

            if (hasItemOfSameProvider && !IsAllowedMultipleItemsPerProvider)
            {
                CurrentMappingSetupProviderItems[sameProviderIndex] = providerItem;
            }
            else
            {
                CurrentMappingSetupProviderItems.Add(providerItem);
            }

            FilterProviderItems();

            SetLinkedItems();

            await ScrollItemListsToTop();

            RenderComponent = true;
        }

        public void ResetMappingItems(bool resetMappingSetupProviderItems = true, bool resetMappedItems = true)
        {
            if (resetMappingSetupProviderItems)
            {
                CurrentMappingSetupProviderItems = new List<MappingProviderItemModel>();

                SetLinkedItems();
            }

            if (resetMappedItems)
            {
                CurrentMappedItems = new List<MappingSetupItemModel>();
            }

            RenderComponent = true;
        }

        public void HandleCreateMappingClick(MappingSetupItemModel item)
        {
            CurrentMappedItems.Add(item);

            ResetMappingItems(true, false);

            RenderComponent = true;
        }

        public async Task OnSuccessfulSave()
        {
            UnmappedItems = TempFilteredUnmappedItems;

            CountCallbackModel.Count = UnmappedItems.Count();

            ResetMappingItems();

            await OnItemsCountUpdateCallback.InvokeAsync(CountCallbackModel);

            RenderComponent = true;
        }

        public void HandleRemoveMappingSetupProviderItem(MappingProviderItemModel providerItem)
        {
            if (providerItem == null) return;

            CurrentMappingSetupProviderItems = CurrentMappingSetupProviderItems.Where(item => JsonSerializer.Serialize(item) != JsonSerializer.Serialize(providerItem)).ToList();

            FilterProviderItems();

            SetLinkedItems();

            RenderComponent = true;
        }

        public void HandleRemoveMappedItem(MappingSetupItemModel singleItem = null)
        {
            if (singleItem == null)
            {
                ResetMappingItems(false, true);
            }
            else
            {
                CurrentMappedItems = CurrentMappedItems.Where(x => JsonSerializer.Serialize(x) != JsonSerializer.Serialize(singleItem)).ToList();
            }

            FilterProviderItems();

            RenderComponent = true;
        }

        public List<MappingProviderItemModel> GetAllItemsToBeFiltered()
        {
            List<MappingProviderItemModel> allItemsToBeFiltered = new List<MappingProviderItemModel>();

            allItemsToBeFiltered.AddRange(CurrentMappingSetupProviderItems);

            foreach (var mappedItem in CurrentMappedItems)
            {
                allItemsToBeFiltered.AddRange(mappedItem.Providers);
            }

            return allItemsToBeFiltered;
        }

        public void FilterProviderItems()
        {
            TempFilteredUnmappedItems = UnmappedItems;

            var allItemsToBeFiltered = GetAllItemsToBeFiltered();

            foreach (var providerItem in allItemsToBeFiltered)
            {
                TempFilteredUnmappedItems = TempFilteredUnmappedItems.Where(x => JsonSerializer.Serialize(x) != JsonSerializer.Serialize(providerItem)).ToList();
            }

            RenderComponent = true;
        }

        public void SetLinkedItems()
        {
            int itemIndex = -1;

            List<string> srIds = new List<string>();
            List<string> bgIds = new List<string>();

            foreach (var currentMappingItem in CurrentMappingSetupProviderItems)
            {
                srIds.AddRange(currentMappingItem.OtPrLi.SrId.Select(s => s.ToString()));
                bgIds.AddRange(currentMappingItem.OtPrLi.BgId.Select(s => s.ToString()));
            }

            foreach (var item in TempFilteredUnmappedItems)
            {
                if ((srIds.Contains(item.Id) && item.ChTyId == (int)ChannelType.SR) ||
                    (bgIds.Contains(item.Id) && item.ChTyId == (int)ChannelType.BG))
                {
                    item.IsCurrentLinkedItem = true;
                    continue;
                }

                item.IsCurrentLinkedItem = false;
            }

            RenderComponent = true;
        }

        public async Task ScrollItemListsToTop()
        {
            var _groupedItems = TempFilteredUnmappedItems.GroupBy(m => m.ChTyId).ToDictionary(g => g.Key, g => g.OrderBy(x => x.IsCurrentLinkedItem ? 0 : 1).ToList());

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
