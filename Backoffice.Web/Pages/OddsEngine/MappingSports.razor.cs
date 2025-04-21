using Backoffice.Web.Models;
using System.Drawing;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Enums;
using Backoffice.Web.Models.OddsEngine.Mapping;
using System.Text.Json;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class MappingSports
    {
        private List<BreadcrumbModel> _breadcrumbs;
        private PageHeaderModel _pageHeaderModel;
        private int _unmappedCount;
        private int _mappedCount;
        private bool _shouldRender;
        private MappingTab _activeTab;
        protected override bool ShouldRender() => _shouldRender;

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            _pageHeaderModel = new()
            {
                Title = "Sports Mapping",
                ShowLanguageDropDown = false
            };

            _breadcrumbs = new List<BreadcrumbModel>
            {
                new BreadcrumbModel
                {
                    Name = "Mapping",
                    Url = string.Empty,
                    IsCurrentPage = false
                },
                new BreadcrumbModel
                {
                    Name = "Sports Mapping",
                    Url = string.Empty,
                    IsCurrentPage = true
                }
            };

            await FetchMappedSports();

            await FetchUnmappedSports();

            _activeTab = MappingTab.Unmapped;

            await PageState.AuthenticateAccessEvent.InvokeAsync();

            _shouldRender = true;
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
                _mappedCount = response.Result.TotalCount;
            }
        }

        private async Task FetchUnmappedSports()
        {
            var response = await _providerMappingService.GetUnmappedItemsByCategory(FetchMappingCategories[MappingCategory.Sport]);

            if (response.Result != null)
            {
                _unmappedCount = response.Result.Unmapped.Count();
            }
        }

        private void HandleTabClick(MappingTab tab)
        {
            _activeTab = tab;
            _shouldRender = true;
        }

        private void SetMappingTabCount(UpdateMappingTabCountModel model)
        {
            var diffCount = 0;
            switch(model.Tab)
            {
                case MappingTab.Unmapped:
                    diffCount = _unmappedCount - model.Count;
                    _unmappedCount = model.Count;
                    _mappedCount = _mappedCount + diffCount;
                    break;
                case MappingTab.Mapped:
                    diffCount = _mappedCount - model.Count;
                    _mappedCount = model.Count;
                    _unmappedCount = _unmappedCount + diffCount;
                    break;
                default:
                    break;
            }

            _shouldRender = true;
        }
    }
}