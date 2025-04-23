using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models;
using Backoffice.Web.Models.OddsEngine.Mapping;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Mapping.Fixture
{
    public partial class UnmappedFixture
    {
        protected override async Task OnInitializedAsync()
        {
            IsAllowedMultipleItemsPerProvider = true;

            await FetchUnmappedFixtures();

            DropdownFilters[MappingCategory.Fixture].SelectAllLabel = _translationState.GetValue("All Fixtures");
        }

        private async Task FetchUnmappedFixtures()
        {
            var response = await _providerMappingService.GetUnmappedItemsByCategory(FetchMappingCategories[MappingCategory.Fixture]);

            if (response != null && response?.Result != null && response?.StatusCode == StatusCode.Success)
            {
                UnmappedItems = response.Result.Unmapped.Select(s => new MappingProviderItemModel(s)).ToList();
                TempFilteredUnmappedItems = UnmappedItems;

                CountCallbackModel.Count = UnmappedItems.Count();

                await OnItemsCountUpdateCallback.InvokeAsync(CountCallbackModel);

                SetDropdownFilters();
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

                foreach (var provider in item.Providers)
                {
                    if (Int32.TryParse(provider.FiId, out intId))
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
            var response = await _providerMappingService.AddOrUpdateProviderMappingBulk(SaveMappingCategories[MappingCategory.Fixture], BuildRequestPayload());

            if (response != null && response.StatusCode == StatusCode.Success)
            {
                await OnSuccessfulSave();
            }

            RenderComponent = true;
        }
    }
}
