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
        public bool ClearSearchQuery { get; set; }
        public bool Init { get; set; } = true;
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

        public Dictionary<MappingCategory, MappingDropdownFilterGroup> DropdownFilters = new Dictionary<MappingCategory, MappingDropdownFilterGroup>
        {
            { MappingCategory.Sport, new MappingDropdownFilterGroup() },
            { MappingCategory.Segment, new MappingDropdownFilterGroup() },
            { MappingCategory.League, new MappingDropdownFilterGroup() }
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

            ClearSearchQuery = true;

            RenderComponent = true;
        }

        public async Task OnSuccessfulSave()
        {
            UnmappedItems = TempFilteredUnmappedItems;

            CountCallbackModel.Count = UnmappedItems.Count();

            ResetMappingItems();

            SetDropdownFilters();

            ClearSearchQuery = true;

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

            SetLinkedItems();

            RenderComponent = true;
        }

        public List<MappingProviderItemModel> GetMappingItemsToBeFiltered()
        {
            List<MappingProviderItemModel> itemsToFilter = new List<MappingProviderItemModel>();

            itemsToFilter.AddRange(CurrentMappingSetupProviderItems);

            foreach (var mappedItem in CurrentMappedItems)
            {
                itemsToFilter.AddRange(mappedItem.Providers);
            }

            return itemsToFilter;
        }

        public List<MappingProviderItemModel> GetSelectedDropdownItems()
        {
            List<MappingProviderItemModel> itemsToInclude = new List<MappingProviderItemModel>();

            var sportIdsToInclude = DropdownFilters[MappingCategory.Sport].Items.Where(x => x.IsChecked == true).Select(sp => sp.SportId).ToList();
            var segmentIdsToInclude = DropdownFilters[MappingCategory.Segment].Items.Where(x => x.IsChecked == true).Select(se => se.SegmentId).ToList();
            var leagueIdsToInclude = DropdownFilters[MappingCategory.League].Items.Where(x => x.IsChecked == true).Select(le => le.LeagueId).ToList();

            if (sportIdsToInclude.Count == 0 || segmentIdsToInclude.Count == 0 || leagueIdsToInclude.Count == 0)
            {
                return itemsToInclude;
            }

            itemsToInclude = UnmappedItems.Where(y => 
                sportIdsToInclude.Contains(y.SpId) && 
                segmentIdsToInclude.Contains(y.SegId) && 
                leagueIdsToInclude.Contains(y.LeId))
                .ToList();

            return itemsToInclude;
        }

        public void FilterProviderItems()
        {
            TempFilteredUnmappedItems = GetSelectedDropdownItems();

            var allItemsToExclude = GetMappingItemsToBeFiltered();

            foreach (var providerItem in allItemsToExclude)
            {
                TempFilteredUnmappedItems = TempFilteredUnmappedItems.Where(x => JsonSerializer.Serialize(x) != JsonSerializer.Serialize(providerItem)).ToList();
            }

            RenderComponent = true;
        }

        public void SetLinkedItems()
        {
            List<string> srIds = new List<string>();
            List<string> bgIds = new List<string>();

            foreach (var currentMappingItem in CurrentMappingSetupProviderItems)
            {
                srIds.AddRange(currentMappingItem.OtPrLi.SrId.Select(s => s.ToString()));
                bgIds.AddRange(currentMappingItem.OtPrLi.BgId.Select(s => s.ToString()));
            }

            foreach (var item in TempFilteredUnmappedItems)
            {
                string idToCheck = !string.IsNullOrEmpty(item.FiId) ? item.FiId : !string.IsNullOrEmpty(item.LeId) ? item.LeId : !string.IsNullOrEmpty(item.SegId) ? item.SegId : item.SpId;

                if ((srIds.Contains(idToCheck) && item.ChTyId == (int)ChannelType.SR) ||
                    (bgIds.Contains(idToCheck) && item.ChTyId == (int)ChannelType.BG))
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

        public void SetDropdownFilters()
        {
            if (Init)
            {
                var initDropdownItems = UnmappedItems.Select(s =>
                    new MappingDropdownFilterItemModel
                    {
                        SportId = s.SpId,
                        SportName = s.SpNa,
                        SegmentId = s.SegId,
                        SegmentName = s.SegNa,
                        LeagueId = s.LeId,
                        LeagueName = s.LeNa
                    }
                );

                var distinctSports = initDropdownItems.DistinctBy(x => x.SportId).ToList();
                var distinctSegments = initDropdownItems.DistinctBy(x => x.SegmentId).ToList();
                var distinctLeagues = initDropdownItems.DistinctBy(x => x.LeagueId).ToList();

                DropdownFilters[MappingCategory.Sport].Items = distinctSports;
                DropdownFilters[MappingCategory.Segment].Items = distinctSegments;
                DropdownFilters[MappingCategory.League].Items = distinctLeagues;
            } else
            {
                var selectedSportIds = DropdownFilters[MappingCategory.Sport].Items.Where(x => x.IsChecked == true).Select(y => y.SportId).ToList();
                var selectedSegmentIds = DropdownFilters[MappingCategory.Segment].Items.Where(x => x.IsChecked == true).Select(y => y.SegmentId).ToList();

                foreach (var segment in DropdownFilters[MappingCategory.Segment].Items)
                {
                    if (selectedSportIds.Contains(segment.SportId))
                    {
                        segment.IsVisible = true;
                        continue;
                    }

                    segment.IsVisible = false;
                }

                foreach (var league in DropdownFilters[MappingCategory.League].Items)
                {
                    if (selectedSportIds.Contains(league.SportId) && selectedSegmentIds.Contains(league.SegmentId))
                    {
                        league.IsVisible = true;
                        continue;
                    }

                    league.IsVisible = false;
                }
            }

            Init = false;

            RenderComponent = true;
        }

        public void HandleDropdownItemClick(DropdownFilterCallbackModel model)
        {
            if (model.Item == null)
            {
                DropdownFilters[model.MappingCategory].SelectAllValue = model.IsChecked;

                foreach (var dropdownItem in DropdownFilters[model.MappingCategory].Items)
                {
                    dropdownItem.IsChecked = model.IsChecked;
                }
            }
            else
            {
                model.Item.IsChecked = model.IsChecked;

                if (DropdownFilters[model.MappingCategory].Items.Where(x => x.IsChecked == false).Count() == 0)
                {
                    DropdownFilters[model.MappingCategory].SelectAllValue = true;
                }

                if (DropdownFilters[model.MappingCategory].Items.Where(x => x.IsChecked == false).Count() > 0)
                {
                    DropdownFilters[model.MappingCategory].SelectAllValue = false;
                }
            }

            SetDropdownFilters();

            FilterProviderItems();
        }

        public void SetClearSearchQueryValue(bool value)
        {
            ClearSearchQuery = value;

            RenderComponent = true;
        }
    }
}
