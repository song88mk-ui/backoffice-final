using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models.OddsEngine.Mapping;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Mapping.League
{
    public partial class UnmappedLeague
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }

        protected override async Task OnInitializedAsync()
        {
            IsAllowedMultipleItemsPerProvider = true;

            await FetchUnmappedLeagues();
        }

        private async Task FetchUnmappedLeagues()
        {
            var response = await _providerMappingService.GetUnmappedItemsByCategory(_fetchMappingCategories[MappingCategory.League]);

            if (response != null && response?.Result != null && response?.StatusCode == StatusCode.Success)
            {
                UnmappedItems = response.Result.Unmapped.Select(s => new MappingProviderItemModel(s)).ToList();
                TempFilteredUnmappedItems = UnmappedItems;

                CountCallbackModel.Count = UnmappedItems.Count();

                await OnItemsCountUpdateCallback.InvokeAsync(CountCallbackModel);
            }

            RenderComponent = true;
        }

        private ProviderMappingBulkRequest BuildRequestPayload()
        {
            var request = new ProviderMappingBulkRequest
            {
                UserId = _userState.Model.Id
            };

            int intId = 0;

            foreach (var item in CurrentMappedItems)
            {
                var info = new ProviderModel<List<int>>()
                {
                    SrId = new List<int>(),
                    BgId = new List<int>()
                };

                foreach (var provider in item.Providers) {
                    if (Int32.TryParse(provider.Id, out intId))
                    {
                        switch (provider.ChTyId)
                        {
                            case (int)ChannelType.SR:
                                info.SrId.Add(intId); 
                                break;
                            case (int)ChannelType.BG:
                                info.BgId.Add(intId);
                                break;
                            default:
                                break;
                        }
                    }
                }

                request.Mapping.Add(
                    new ProviderMappingModel
                    {
                        Na = item.Name,
                        Info = JsonSerializer.Serialize(info),
                    }
                );
            }

            return request;
        }

        private async Task HandleSaveMappingClick()
        {
            var response = await _providerMappingService.AddOrUpdateProviderMappingBulk(_saveMappingCategories[MappingCategory.League], BuildRequestPayload());

            if (response != null && response.StatusCode == StatusCode.Success)
            {
                UnmappedItems = TempFilteredUnmappedItems;

                CountCallbackModel.Count = UnmappedItems.Count();

                ResetMappingItems();

                await OnItemsCountUpdateCallback.InvokeAsync(CountCallbackModel);
            }

            RenderComponent = true;
        }
    }
}
