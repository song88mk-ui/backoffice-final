using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models.OddsEngine.Mapping;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class MappingSports
    {
        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;
            PageHeaderTitle = _translationState.GetValue("Sports Mapping");
            BreadcrumbsMappingLabel = _translationState.GetValue("Mapping");
            BreadcrumbsCurrentPageLabel = _translationState.GetValue("Sports Mapping");

            SetBreadcrumbsAndHeader();

            await FetchMappedSports();

            await FetchUnmappedSports();

            await PageState.AuthenticateAccessEvent.InvokeAsync();

            RenderComponent = true;
        }

        private async Task FetchMappedSports()
        {
            var request = new MappingRequest
            {
                Page = 1,
                PageSize = 1000,
            };

            var response = await _providerMappingService.GetSportMapping(request);
            
            if (response.Result != null)
            {
                MappedCount = response.Result.TotalCount;
            }

            RenderComponent = true;
        }

        private async Task FetchUnmappedSports()
        {
            var response = await _providerMappingService.GetUnmappedItemsByCategory(FetchMappingCategories[MappingCategory.Sport]);

            if (response.Result != null)
            {
                UnmappedCount = response.Result.Unmapped.Count();
            }

            RenderComponent = true;
        }

        private async Task SetMappingTabCount(UpdateMappingTabCountModel model)
        {
            switch(model.Tab)
            {
                case MappingTab.Unmapped:
                    UnmappedCount = model.Count;
                    await FetchMappedSports();
                    break;
                case MappingTab.Mapped:
                    MappedCount = model.Count;
                    await FetchUnmappedSports();
                    break;
                default:
                    break;
            }

            RenderComponent = true;
        }
    }
}