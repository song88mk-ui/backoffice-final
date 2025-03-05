using Backoffice.Web.Models;
using System.Drawing;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class MappingSports
    {
        private List<BreadcrumbModel> _breadcrumbs;
        private PageHeaderModel _pageHeaderModel;

        private bool _shouldRender;

        private MappingTab _activeTab;

        private enum MappingTab
        {
            Unmapped,
            Mapped
        }

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

        protected override bool ShouldRender() => _shouldRender;

        private void HandleTabClick(MappingTab tab)
        {
            _activeTab = tab;
            _shouldRender = true;
        }
    }
}