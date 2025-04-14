using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models.OddsEngine.Mapping;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Mapping.Sport
{
    public partial class UnmappedSport
    {
        protected override async Task OnInitializedAsync()
        {
            IsAllowedMultipleItemsPerProvider = false;

            await FetchUnmappedSports();
        }

        private async Task FetchUnmappedSports()
        {
            var response = await _providerMappingService.GetUnmappedItemsByCategory(FetchMappingCategories[MappingCategory.Sport]);

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

            int SrId = 0;
            int BgId = 0;

            foreach (var item in CurrentMappedItems)
            {
                var info = new ProviderModel<int>()
                {
                    SrId = 0,
                    BgId = 0,   
                };

                if (Int32.TryParse(item.Providers.FirstOrDefault(i => i.ChTyId == (int)ChannelType.SR)?.SpId, out SrId))
                {
                    info.SrId = SrId;
                }

                if (Int32.TryParse(item.Providers.FirstOrDefault(i => i.ChTyId == (int)ChannelType.BG)?.SpId, out BgId))
                {
                    info.BgId = BgId;
                }

                request.Mapping.Add(
                    new ProviderMappingModel()
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
            var response = await _providerMappingService.AddOrUpdateProviderMappingBulk(SaveMappingCategories[MappingCategory.Sport], BuildRequestPayload());

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