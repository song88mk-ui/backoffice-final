using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models.OddsEngine.Mapping;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class MappingLeagues
    {
        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;
            PageHeaderTitle = _translationState.GetValue("Leagues Mapping");
            BreadcrumbsMappingLabel = _translationState.GetValue("Mapping");
            BreadcrumbsCurrentPageLabel = _translationState.GetValue("Leagues Mapping");

            SetBreadcrumbsAndHeader();

            await FetchMappedLeagues();

            await FetchUnmappedLeagues();

            await PageState.AuthenticateAccessEvent.InvokeAsync();

            RenderComponent = true;
        }

        private async Task FetchMappedLeagues()
        {
            var request = new MappingRequest
            {
                Page = 1,
                PageSize = 1000,
            };

            var response = await _providerMappingService.GetLeaguesMapping(request);

            if (response.Result != null)
            {
                MappedCount = response.Result.TotalCount;
            }

            RenderComponent = true;
        }

        private async Task FetchUnmappedLeagues()
        {
            var response = await _providerMappingService.GetUnmappedItemsByCategory(FetchMappingCategories[MappingCategory.League]);

            if (response.Result != null)
            {
                UnmappedCount = response.Result.Unmapped.Count();
            }

            RenderComponent = true;
        }

        private async Task SetMappingTabCount(UpdateMappingTabCountModel model)
        {
            switch (model.Tab)
            {
                case MappingTab.Unmapped:
                    UnmappedCount = model.Count;
                    await FetchMappedLeagues();
                    break;
                case MappingTab.Mapped:
                    MappedCount = model.Count;
                    await FetchUnmappedLeagues();
                    break;
                default:
                    break;
            }

            RenderComponent = true;
        }

    }
}