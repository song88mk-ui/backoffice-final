using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Shared.Enums;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class MappingLeagues
    {
        private List<BreadcrumbModel> _breadcrumbs;
        private PageHeaderModel _pageHeaderModel;
        private AlertModel _alertModel;

        private bool _shouldRender;

        private MappingTab _activeTab;

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            _pageHeaderModel = new()
            {
                Title = "League Mapping",
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
                    Name = "League Mapping",
                    Url = string.Empty,
                    IsCurrentPage = true
                }
            };

            _activeTab = MappingTab.Unmapped;

            await PageState.AuthenticateAccessEvent.InvokeAsync();
        }

        public async void ShowSuccessSaveMappingAlert()
        {
            _alertModel = new AlertModel()
            {
                Status = AlertStatus.Success,
                Message = "Mapping saved successfully!",
                Opacity = 1.0m,
            };

            _shouldRender = true;
        }

        protected override bool ShouldRender() => _shouldRender;

        private void HandleTabClick(MappingTab tab)
        {
            _activeTab = tab;
            _shouldRender = true;
        }


    }
}