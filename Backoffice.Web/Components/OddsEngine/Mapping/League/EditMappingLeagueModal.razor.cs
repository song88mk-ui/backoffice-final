using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Mapping;
using Backoffice.Web.Services;
using Backoffice.Web.Components.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Mapping.League
{
    public partial class EditMappingLeagueModal: IDisposable
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [CascadingParameter] private CustomModal Modal { get; set; }
        private List<MappingProviderItemModel> _unmappedSports;
        private List<MappingProviderItemModel> _tempFilteredUnmappedSports;
        private List<MappingProviderItemModel> _currentMappingSetupProviderItems = new List<MappingProviderItemModel>();
        private List<MappingSetupItemModel> _currentMappedItems = new List<MappingSetupItemModel>();
        private MappingSetupItemModel _mappingSetupItem = new MappingSetupItemModel();
        private string _formId = "editMappingFormId";
        private AlertModel _alertModel;
        private bool _isPreLoader = true;
        private bool _shouldRender;
        private Dictionary<MappingCategory, string> _mappingCategories = new Dictionary<MappingCategory, string>
        {
            { MappingCategory.Sport, "sport" },
            { MappingCategory.Segment, "segment" },
            { MappingCategory.League, "league" },
            { MappingCategory.Fixture, "fixture" },
            { MappingCategory.Market, "market" },
            { MappingCategory.Selection, "selection" }
        };

        protected override bool ShouldRender() => _shouldRender;
        protected override async Task OnInitializedAsync()
        {
            var response = await _providerMappingService.GetUnmappedSports();
            _unmappedSports = response.Result.Unmapped.Select(s => new MappingProviderItemModel(s)).ToList();
            _tempFilteredUnmappedSports = _unmappedSports;

            var mappedResponse = await _providerMappingService.GetSportMappingBySpId(37);
            var sportMapping = mappedResponse.Result;

            _mappingSetupItem = new MappingSetupItemModel
            {
                Id = sportMapping.SpId.ToString(),
                Name = sportMapping.SpNa,
                Providers = GetUnmappedProviderByInfo(sportMapping.Info)
            };
            _currentMappingSetupProviderItems = _mappingSetupItem.Providers;

            FilterProviderItems();

            SetLinkedItems();

            _isPreLoader = false;

            _shouldRender = true;
        }

        private List<MappingProviderItemModel> GetUnmappedProviderByInfo(ProviderItemModel<Item<int>> info)
        {
            var providers = new List<MappingProviderItemModel>();

            if(info.SrId.Id > 0)
            {
                providers.Add(new MappingProviderItemModel()
                {
                    Id = info.SrId.Id.ToString(),
                    Na = info.SrId.Name,
                    ChTyId = (int)ChannelType.SR,
                    OtPrLi = info.SrId.OtPrLi,
                });
            }
            if(info.BgId.Id > 0)
            {
                providers.Add(new MappingProviderItemModel()
                {
                    Id = info.BgId.Id.ToString(),
                    Na = info.BgId.Name,
                    ChTyId = (int)ChannelType.BG,
                    OtPrLi = info.BgId.OtPrLi,
                });
            }

            return providers;
        }
        
        private async Task HandleUpdateMappingClick(MappingSetupItemModel item)
        {
            var info = new ProviderModel<int>()
            {
                SrId = 0,
                BgId = 0,   
            };

            if (Int32.TryParse(item.Providers.FirstOrDefault(i => i.ChTyId == (int)ChannelType.SR)?.Id, out int SrId))
            {
                info.SrId = SrId;
            }

            if (Int32.TryParse(item.Providers.FirstOrDefault(i => i.ChTyId == (int)ChannelType.BG)?.Id, out int BgId))
            {
                info.BgId = BgId;
            }

            var request = new ProviderMappingRequest()
            {
                UserId = _userState.Model.Id,
                Mapping = new ProviderMappingModel
                {
                    Id = item.Id,
                    Na = item.Name,
                    Info = JsonSerializer.Serialize(info)
                }
            };

            var response = await _providerMappingService.AddOrUpdateProviderMapping(_mappingCategories[MappingCategory.Sport], request);
            
            if (response != null && response.StatusCode == StatusCode.Success)
            {
                ModalService.Cancel();

                await Modal.OnSaveClickCallback.InvokeAsync();
            }
            else
            {
                _alertModel = new AlertModel()
                {
                    Status = AlertStatus.Error,
                    Message = "Something went wrong. Please try again!",
                    Opacity = 1.0m,
                };
            }
        }

        private void HandleRemoveMappingSetupProviderItem(MappingProviderItemModel providerItem)
        {
            if (providerItem == null) return;

            _currentMappingSetupProviderItems = _currentMappingSetupProviderItems.Where(item => item.ChTyId != providerItem.ChTyId).ToList();

            if (!_unmappedSports.Any(item => item.Id == providerItem.Id))
            {
                _unmappedSports.Add(providerItem);
            }

            FilterProviderItems();

            SetLinkedItems();

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

        public void OnCloseClickEvent()
        {
            ModalService.Cancel();
        }
        public void Dispose()
        {
        }
    }
}
