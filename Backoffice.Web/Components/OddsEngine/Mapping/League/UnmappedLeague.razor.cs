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
            var response = await _providerMappingService.GetUnmappedLeagues();

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
            var request = new ProviderMappingBulkRequest();
            int SrId = 0;
            int BgId = 0;

            foreach (var item in CurrentMappedItems)
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
                    new ProviderMappingModel
                    {
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

        private async Task HandleSaveMappingClick()
        {
            var response = await _providerMappingService.AddOrUpdateProviderMappingBulk("league", BuildRequestPayload());

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
