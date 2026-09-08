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

namespace Backoffice.Web.Components.OddsEngine.Mapping.Sport
{
    public partial class EditMappingSportModal : IDisposable
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [CascadingParameter] private CustomModal Modal { get; set; }
        private MappingSetupItemModel _mappingSetupItem = new MappingSetupItemModel();
        private string _formId = "editMappingFormId";
        private AlertModel _alertModel;
        private bool _isPreLoader = true;
        private bool _isSportMappingFound = false;
        protected override async Task OnInitializedAsync()
        {
            await FetchUnmappedSports();

            var mappedResponse = await _providerMappingService.GetSportMappingBySpId(ModalParameters.TryGet<int>("SpId"));
            var sportMapping = mappedResponse.Result;

            if (sportMapping == null)
            {
                _alertModel = new AlertModel()
                {
                    Status = AlertStatus.Error,
                    Message = "Sorry! Unable to find mapping for this sport.",
                    Opacity = 1.0m,
                };
                _isPreLoader = false;
                return;
            }

            _isSportMappingFound = true;
            _mappingSetupItem = new MappingSetupItemModel
            {
                Id = sportMapping.SpId.ToString(),
                Name = sportMapping.SpNa,
                Providers = GetUnmappedProviderByInfo(sportMapping.Info)
            };
            CurrentMappingSetupProviderItems = _mappingSetupItem.Providers;

            UpdateUnmappedItems();

            SetDropdownFilters();

            FilterProviderItems();

            SetLinkedItems();

            _isPreLoader = false;
        }

        private void UpdateUnmappedItems()
        {
            foreach (var providerItem in CurrentMappingSetupProviderItems)
            {
                if (!UnmappedItems.Any(item => JsonSerializer.Serialize(item) == JsonSerializer.Serialize(providerItem)))
                {
                    UnmappedItems.Add(providerItem);
                }
            }
        }

        private async Task FetchUnmappedSports()
        {
            var response = await _providerMappingService.GetUnmappedItemsByCategory(FetchMappingCategories[MappingCategory.Sport]);

            if (response != null && response?.Result != null && response?.StatusCode == StatusCode.Success)
            {
                UnmappedItems = response.Result.Unmapped.Select(s => new MappingProviderItemModel(s)).ToList();
                TempFilteredUnmappedItems = UnmappedItems;
            }

            RenderComponent = true;
        }

        private List<MappingProviderItemModel> GetUnmappedProviderByInfo(ProviderItemModel<Item<int>> info)
        {
            var providers = new List<MappingProviderItemModel>();

            if (info.SrId.Id > 0)
            {
                providers.Add(new MappingProviderItemModel()
                {
                    SpId = info.SrId.Id.ToString(),
                    SpNa = info.SrId.Name,
                    ChTyId = (int)ChannelType.SR,
                    OtPrLi = info.SrId.OtPrLi,
                });
            }
            if (info.BgId.Id > 0)
            {
                providers.Add(new MappingProviderItemModel()
                {
                    SpId = info.BgId.Id.ToString(),
                    SpNa = info.BgId.Name,
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

            if (Int32.TryParse(item.Providers.FirstOrDefault(i => i.ChTyId == (int)ChannelType.SR)?.SpId, out int SrId))
            {
                info.SrId = SrId;
            }

            if (Int32.TryParse(item.Providers.FirstOrDefault(i => i.ChTyId == (int)ChannelType.BG)?.SpId, out int BgId))
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

            var response = await _providerMappingService.AddOrUpdateProviderMapping(SaveMappingCategories[MappingCategory.Sport], request);

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
                    Message = response?.Message ?? "Something went wrong. Please try again!",
                    Opacity = 1.0m,
                };
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
