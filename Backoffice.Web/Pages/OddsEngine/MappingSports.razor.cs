using System.Drawing;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Enums;
using Backoffice.Web.Models.OddsEngine.Mapping;

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

            _activeTab = MappingTab.Unmapped;

            await PageState.AuthenticateAccessEvent.InvokeAsync();
        }

        private void HandleTabClick(MappingTab tab)
        {
            _activeTab = tab;
            _shouldRender = true;
        }

        private void SetMappingTabCount(UpdateMappingTabCountModel model)
        {
            switch(model.Tab)
            {
                case MappingTab.Unmapped:
                    _unmappedCount = model.Count;
                    break;
                case MappingTab.Mapped:
                    _mappedCount = model.Count; 
                    break;
                default:
                    break;
            }

            _shouldRender = true;
        }
    }
}